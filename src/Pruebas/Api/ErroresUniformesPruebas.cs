using System.Net;
using System.Text.Json;
using BlackHellHeim.Pruebas.Comun;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Errores de validación (400) e inesperados (500) con el mismo formato ProblemDetails.</summary>
public class ErroresUniformesPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly FabricaApiPruebas _fabrica;

    public ErroresUniformesPruebas(FabricaApiPruebas fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task UnErrorDeValidacionDevuelveProblemaUniformeConCorrelacion()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/tiendas/B");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var texto = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(texto);
        var raiz = doc.RootElement;
        Assert.Equal("solicitud_invalida", raiz.GetProperty("codigo").GetString());
        Assert.Equal(respuesta.Headers.GetValues("X-Correlation-ID").Single(), raiz.GetProperty("idCorrelacion").GetString());
        Assert.NotEmpty(raiz.GetProperty("errores").GetProperty("codigo").EnumerateArray());
        Assert.DoesNotContain("traceId", texto);
    }

    [Fact]
    public async Task UnErrorInesperadoDevuelveElMismoFormatoSinDetallesInternos()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/pruebas/error");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var texto = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(texto);
        var raiz = doc.RootElement;
        Assert.Equal("error_inesperado", raiz.GetProperty("codigo").GetString());
        Assert.Equal(500, raiz.GetProperty("status").GetInt32());
        Assert.Equal(respuesta.Headers.GetValues("X-Correlation-ID").Single(), raiz.GetProperty("idCorrelacion").GetString());
        Assert.DoesNotContain("secreto-interno", texto);
        Assert.DoesNotContain("InvalidOperationException", texto);
        Assert.DoesNotContain("StackTrace", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".cs", texto);
        Assert.DoesNotContain("traceId", texto);
    }

    [Fact]
    public async Task SiNoHayCorrelacionElManejadorLaGeneraYElEndpointDePruebaNoExisteEnProduccion()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/pruebas/error");
        Assert.False(string.IsNullOrWhiteSpace(respuesta.Headers.GetValues("X-Correlation-ID").Single()));

        await using var produccion = _fabrica.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var cliente = produccion.CreateClient();
        var enProduccion = await cliente.GetAsync("/pruebas/error");
        var documento = await cliente.GetStringAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, enProduccion.StatusCode);
        Assert.DoesNotContain("/pruebas", documento);
        Assert.DoesNotContain("/pruebas", await _fabrica.CreateClient().GetStringAsync("/openapi/v1.json"));
    }
}
