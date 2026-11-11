using System.Diagnostics;
using System.Net;
using System.Text.Json;
using BlackHellHeim.Pruebas.Comun;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Página interactiva de Scalar en <c>/scalar</c> (historia 004).</summary>
public class DocumentacionScalarPruebas : IClassFixture<FabricaApiPruebas>
{
    private const string Aviso =
        "No se pudo cargar el contrato OpenAPI de BlackHellHeim API. Verifica que /openapi/v1.json responda e inténtalo de nuevo.";

    private readonly FabricaApiPruebas _fabrica;

    public DocumentacionScalarPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    private HttpClient ClienteSinRedireccion() =>
        _fabrica.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task ScalarRedirigeAScalarConDiagonal()
    {
        var respuesta = await ClienteSinRedireccion().GetAsync("/scalar");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.EndsWith("scalar/", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task LaPaginaSirveHtmlConElTituloElDocumentoYSinRecursosExternos()
    {
        var cronometro = Stopwatch.StartNew();
        var respuesta = await _fabrica.CreateClient().GetAsync("/scalar/");
        var html = await respuesta.Content.ReadAsStringAsync();
        cronometro.Stop();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/html", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("BlackHellHeim API", html);
        Assert.Contains("openapi/v1.json", html);
        Assert.DoesNotContain("http://", html);
        Assert.DoesNotContain("https://", html);
        Assert.True(cronometro.Elapsed < TimeSpan.FromSeconds(3), $"Tardó {cronometro.Elapsed}.");
    }

    [Fact]
    public async Task LaPaginaIncluyeElAvisoOcultoParaCuandoElDocumentoNoSeCarga()
    {
        var html = await _fabrica.CreateClient().GetStringAsync("/scalar/");

        Assert.Contains($"id=\"aviso-documento\"", html);
        Assert.Matches("id=\"aviso-documento\"[^>]*\\bhidden\\b|\\bhidden\\b[^>]*id=\"aviso-documento\"", html);
        Assert.Contains(Aviso, html);
        Assert.Contains("fetch('/openapi/v1.json')", html);
        Assert.Contains("removeAttribute('hidden')", html);
    }

    [Fact]
    public async Task SiLaGeneracionDelDocumentoFallaElDocumentoDaUn500UniformeYLaPaginaSigueEn200()
    {
        await using var fabrica = _fabrica.WithWebHostBuilder(b => b.ConfigureServices(servicios =>
            servicios.Configure<OpenApiOptions>("v1", opciones =>
                opciones.AddDocumentTransformer((_, _, _) => throw new InvalidOperationException("secreto-interno")))));
        var cliente = fabrica.CreateClient();

        var documento = await cliente.GetAsync("/openapi/v1.json");
        var pagina = await cliente.GetAsync("/scalar/");
        var cuerpo = await documento.Content.ReadAsStringAsync();
        var html = await pagina.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, documento.StatusCode);
        Assert.Equal("application/problem+json", documento.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(cuerpo);
        Assert.Equal("error_inesperado", doc.RootElement.GetProperty("codigo").GetString());
        Assert.DoesNotContain("secreto-interno", cuerpo);
        Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        Assert.Contains(Aviso, html);
    }

    [Fact]
    public async Task EnProduccionLaPaginaYElDocumentoRespondenYElEndpointDePruebaNoExiste()
    {
        await using var fabrica = _fabrica.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var cliente = fabrica.CreateClient();

        var pagina = await cliente.GetAsync("/scalar");
        var documento = await cliente.GetAsync("/openapi/v1.json");
        var prueba = await cliente.GetAsync("/pruebas/error");

        Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        Assert.Equal(HttpStatusCode.OK, documento.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, prueba.StatusCode);
    }
}
