using System.Text.Json.Nodes;
using Xunit;

namespace BlackHellHeim.Pruebas.Contratos;

public class ComparadorContratosPruebas
{
    private static JsonObject Contrato(string yaml) => LectorContratos.ConvertirYaml(yaml);

    private const string Base = """
        openapi: 3.1.0
        info: { title: A, version: 1 }
        paths:
          /a:
            get:
              summary: texto que se ignora
              parameters:
                - name: id
                  in: path
                  required: true
                  schema: { type: string, pattern: '^[A-Z]{3}$' }
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/X' }
        components:
          schemas:
            X:
              type: object
              required: [n]
              properties:
                n: { type: string, examples: [uno] }
        """;

    [Fact]
    public void IgnoraInfoDescripcionesEjemplosYVersionMenorDeOpenApi()
    {
        var a = Contrato(Base);
        var b = Contrato(Base.Replace("3.1.0", "3.1.1").Replace("texto que se ignora", "otro texto").Replace("[uno]", "[dos]"));

        var diferencias = ComparadorContratos.Comparar(
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("a", a)])),
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("b", b)])));

        Assert.NotEmpty(diferencias);
    }

    [Fact]
    public void IgnoraLaCabeceraDeCorrelacion()
    {
        var conCabecera = Base.Replace("        - name: id",
            "        - { name: X-Correlation-ID, in: header, required: false, schema: { type: string } }\n        - name: id");

        var diferencias = ComparadorContratos.Comparar(
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("a", Contrato(conCabecera))])),
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("b", Contrato(Base))])));

        Assert.Empty(diferencias);
    }

    [Fact]
    public void DetectaUnParametroDistinto()
    {
        var distinto = Base.Replace("^[A-Z]{3}$", "^[a-z]{3}$");

        var diferencias = ComparadorContratos.Comparar(
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("a", Contrato(Base))])),
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("b", Contrato(distinto))])));

        Assert.Contains(diferencias, d => d.Contains("parámetro path:id") && d.Contains("pattern"));
    }

    [Fact]
    public void DetectaUnaRespuestaFaltanteYUnEsquemaDistintoConMensajeLegible()
    {
        var a = Contrato(Base);
        var b = Contrato(Base.Replace("'200':", "'201':").Replace("type: object", "type: array"));

        var diferencias = ComparadorContratos.Comparar(
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("a", a)])),
            ComparadorContratos.Proyectar(ComparadorContratos.Componer([new("b", b)])));

        Assert.Contains(diferencias, d => d.Contains("GET /a respuesta 200"));
        Assert.Contains(diferencias, d => d.Contains("GET /a respuesta 201"));
        Assert.Contains(diferencias, d => d.Contains("esquema X.type"));
    }

    [Fact]
    public void UnEsquemaDuplicadoConContenidoDistintoEsUnError()
    {
        var a = Contrato(Base);
        var b = Contrato(Base.Replace("type: object", "type: array").Replace("/a:", "/b:"));

        var composicion = ComparadorContratos.Componer([new("a", a), new("b", b)]);

        Assert.Contains(composicion.Errores, e => e.Contains("Esquema duplicado") && e.Contains('X'));
    }

    [Fact]
    public void UnEsquemaDuplicadoIgualNoEsUnError()
    {
        var a = Contrato(Base);
        var b = Contrato(Base.Replace("/a:", "/b:"));

        var composicion = ComparadorContratos.Componer([new("a", a), new("b", b)]);

        Assert.Empty(composicion.Errores);
    }

    [Fact]
    public void UnaOperacionDuplicadaConContenidoDistintoEsUnError()
    {
        var a = Contrato(Base);
        var b = Contrato(Base.Replace("'200':", "'404':"));

        var composicion = ComparadorContratos.Componer([new("a", a), new("b", b)]);

        Assert.Contains(composicion.Errores, e => e.Contains("Operación duplicada") && e.Contains("GET /a"));
    }
}
