using System.Net;
using System.Text.Json;
using BlackHellHeim.Pruebas.Comun;
using Microsoft.OpenApi;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Documento OpenAPI publicado en <c>/openapi/v1.json</c> (historia 004).</summary>
public class OpenApiPublicadoPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;

    public OpenApiPublicadoPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private async Task<(HttpResponseMessage Respuesta, string Texto)> ObtenerAsync()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/openapi/v1.json");
        return (respuesta, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PublicaUnDocumentoOpenApi31ConElTituloYLasOperaciones()
    {
        var (respuesta, texto) = await ObtenerAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(texto);
        var raiz = doc.RootElement;
        Assert.StartsWith("3.1", raiz.GetProperty("openapi").GetString());
        Assert.Equal("BlackHellHeim API", raiz.GetProperty("info").GetProperty("title").GetString());

        var rutas = raiz.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Contains("/salud", rutas);
        Assert.Contains("/tiendas/{codigo}", rutas);
        Assert.Contains("/salud/base", rutas);
        Assert.DoesNotContain(rutas, r => r.StartsWith("/openapi", StringComparison.Ordinal));
        Assert.DoesNotContain(rutas, r => r.StartsWith("/scalar", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CadaOperacionDeclaraElErrorUniformeDeLos500()
    {
        var (_, texto) = await ObtenerAsync();

        using var doc = JsonDocument.Parse(texto);
        foreach (var ruta in doc.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operacion in ruta.Value.EnumerateObject())
            {
                var respuesta500 = operacion.Value.GetProperty("responses").GetProperty("500");
                var esquema = respuesta500.GetProperty("content").GetProperty("application/problem+json").GetProperty("schema");
                Assert.Equal("#/components/schemas/ProblemDetails", esquema.GetProperty("$ref").GetString());
            }
        }
    }

    [Fact]
    public async Task ElDocumentoEsValidoYNoTieneReferenciasRotas()
    {
        var (_, texto) = await ObtenerAsync();

        var lectura = OpenApiDocument.Parse(texto, "json");
        Assert.NotNull(lectura.Document);
        Assert.Empty(lectura.Diagnostic?.Errors ?? []);

        using var doc = JsonDocument.Parse(texto);
        var esquemas = doc.RootElement.GetProperty("components").GetProperty("schemas")
            .EnumerateObject().Select(p => p.Name).ToHashSet();
        var rotas = new List<string>();
        foreach (var referencia in BuscarReferencias(doc.RootElement))
        {
            const string prefijo = "#/components/schemas/";
            if (!referencia.StartsWith(prefijo, StringComparison.Ordinal) || !esquemas.Contains(referencia[prefijo.Length..]))
            {
                rotas.Add(referencia);
            }
        }

        Assert.True(rotas.Count == 0, "Referencias rotas: " + string.Join(", ", rotas));
    }

    [Fact]
    public async Task ElDocumentoNoExponeSecretosTrazasNiRutasInternas()
    {
        var (_, texto) = await ObtenerAsync();

        foreach (var prohibido in new[] { "Password", "Server=", "ConnectionString", "secreto", "StackTrace", ".cs", "C:\\", "traceId", "/pruebas" })
        {
            Assert.DoesNotContain(prohibido, texto, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task LaSaludSigueIgualYApareceDocumentada()
    {
        var cliente = _fabrica.CreateClient();
        var salud = await cliente.GetAsync("/salud");
        var (_, texto) = await ObtenerAsync();

        Assert.Equal(HttpStatusCode.OK, salud.StatusCode);
        Assert.Equal("""{"estado":"ok"}""", await salud.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(texto);
        var esquema = doc.RootElement.GetProperty("paths").GetProperty("/salud").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.Equal("#/components/schemas/EstadoSalud", esquema.GetProperty("$ref").GetString());
    }

    private static IEnumerable<string> BuscarReferencias(JsonElement elemento)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var propiedad in elemento.EnumerateObject())
                {
                    if (propiedad.Name == "$ref" && propiedad.Value.ValueKind == JsonValueKind.String)
                    {
                        yield return propiedad.Value.GetString()!;
                    }
                    else
                    {
                        foreach (var r in BuscarReferencias(propiedad.Value))
                        {
                            yield return r;
                        }
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var hijo in elemento.EnumerateArray())
                {
                    foreach (var r in BuscarReferencias(hijo))
                    {
                        yield return r;
                    }
                }

                break;
        }
    }
}
