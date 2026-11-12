using System.Text.Json.Nodes;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Contratos;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>
/// Contrato primero (principio II): el documento publicado coincide con los contratos de
/// <c>specs/*/contratos/openapi.yaml</c>, en ambos sentidos.
/// </summary>
public class ContratoOpenApiPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;

    public ContratoOpenApiPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private static IReadOnlyDictionary<string, JsonObject> LeerContratos() =>
        LectorContratos.LeerTodos(LectorContratos.BuscarRaiz(AppContext.BaseDirectory));

    private async Task<JsonObject> ObtenerPublicadoAsync()
    {
        var texto = await _fabrica.CreateClient().GetStringAsync("/openapi/v1.json");
        return (JsonObject)JsonNode.Parse(texto)!;
    }

    private static IReadOnlyList<string> Diferencias(
        IEnumerable<KeyValuePair<string, JsonObject>> contratos, JsonObject publicado)
    {
        var composicion = ComparadorContratos.Componer(contratos);
        var lista = composicion.Errores.ToList();
        lista.AddRange(ComparadorContratos.Comparar(
            ComparadorContratos.Proyectar(composicion), ComparadorContratos.Proyectar(publicado)));
        return lista;
    }

    [Fact]
    public async Task ElDocumentoPublicadoCoincideConLosContratosEnAmbosSentidos()
    {
        var diferencias = Diferencias(LeerContratos(), await ObtenerPublicadoAsync());

        Assert.True(diferencias.Count == 0,
            "El documento publicado no coincide con los contratos:\n" + string.Join("\n", diferencias));
    }

    [Fact]
    public void LasCopiasDeProblemDetailsSonIdenticasEnTodosLosContratos()
    {
        var composicion = ComparadorContratos.Componer(LeerContratos());

        Assert.DoesNotContain(composicion.Errores, e => e.Contains("ProblemDetails"));
        Assert.NotNull(composicion.Esquemas["ProblemDetails"]);
    }

    [Fact]
    public async Task TodaOperacionContratadaTieneRespuestaDeErrorUniforme()
    {
        var composicion = ComparadorContratos.Componer(LeerContratos());

        foreach (var (clave, operacion) in composicion.Paths)
        {
            var respuestas = (JsonObject)operacion!["responses"]!;
            Assert.True(respuestas["500"] is not null, $"{clave} no declara la respuesta 500 con ProblemDetails.");
        }

        _ = await ObtenerPublicadoAsync();
    }

    [Fact]
    public async Task UnContratoAlteradoEnMemoriaHaceFallarLaComparacionNombrandoLaDiferencia()
    {
        var contratos = LeerContratos().ToDictionary(c => c.Key, c => (JsonObject)c.Value.DeepClone());
        var tiendas = contratos["002b-casos-de-uso-mediator"];
        ((JsonObject)tiendas["paths"]!["/tiendas/{codigo}"]!["get"]!["responses"]!).Remove("404");
        ((JsonObject)contratos["003-sql-server-efcore"]["components"]!["schemas"]!["EstadoBaseDatos"]!["properties"]!["estado"]!)["type"] = "integer";

        var diferencias = Diferencias(contratos, await ObtenerPublicadoAsync());

        Assert.Contains(diferencias, d => d.Contains("GET /tiendas/{codigo} respuesta 404"));
        Assert.Contains(diferencias, d => d.Contains("EstadoBaseDatos.properties.estado.type"));
    }
}
