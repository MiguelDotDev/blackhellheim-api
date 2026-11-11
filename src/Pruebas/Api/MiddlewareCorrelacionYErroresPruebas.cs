using System.Net;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Api.Comun;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using BlackHellHeim.Pruebas.Comun;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>
/// Pruebas del middleware de correlación y errores con endpoints temporales registrados
/// desde la prueba (sin tocar <c>Program.cs</c>).
/// </summary>
public class MiddlewareCorrelacionYErroresPruebas : IClassFixture<FabricaApiPruebas>
{
    private const string Cabecera = "X-Correlation-ID";
    private readonly HttpClient _cliente;

    public MiddlewareCorrelacionYErroresPruebas(FabricaApiPruebas fabrica)
    {
        _cliente = fabrica
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddTransient<IStartupFilter, FiltroEndpointsTemporales>()))
            .CreateClient();
    }

    [Fact]
    public async Task ConservaLaCabeceraDeCorrelacionValida()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/prueba/ok");
        peticion.Headers.Add(Cabecera, "mi-correlacion-123");

        var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("mi-correlacion-123", respuesta.Headers.GetValues(Cabecera).Single());
        Assert.Equal("mi-correlacion-123", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GeneraLaCorrelacionSiFalta()
    {
        var respuesta = await _cliente.GetAsync("/prueba/ok");

        var id = respuesta.Headers.GetValues(Cabecera).Single();
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.InRange(id.Length, 1, 64);
    }

    [Theory]
    [InlineData("")]
    [InlineData("con espacios y símbolos <>")]
    public async Task GeneraLaCorrelacionSiLaCabeceraEsInvalida(string invalida)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/prueba/ok");
        peticion.Headers.TryAddWithoutValidation(Cabecera, invalida);

        var respuesta = await _cliente.SendAsync(peticion);

        var id = respuesta.Headers.GetValues(Cabecera).Single();
        Assert.NotEqual(invalida, id);
        Assert.InRange(id.Length, 1, 64);
    }

    [Fact]
    public async Task GeneraLaCorrelacionSiLaCabeceraSuperaLos64Caracteres()
    {
        var larga = new string('a', 65);
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/prueba/ok");
        peticion.Headers.Add(Cabecera, larga);

        var respuesta = await _cliente.SendAsync(peticion);

        Assert.NotEqual(larga, respuesta.Headers.GetValues(Cabecera).Single());
    }

    [Fact]
    public async Task LaExcepcionDeValidacionDevuelve400ConErroresPorCampo()
    {
        var respuesta = await _cliente.GetAsync("/prueba/validacion");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = doc.RootElement;
        Assert.Equal("solicitud_invalida", raiz.GetProperty("codigo").GetString());
        Assert.Equal(400, raiz.GetProperty("status").GetInt32());
        Assert.Equal(respuesta.Headers.GetValues(Cabecera).Single(), raiz.GetProperty("idCorrelacion").GetString());
        Assert.Equal("El campo es obligatorio.", raiz.GetProperty("errores").GetProperty("campo")[0].GetString());
        Assert.False(raiz.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task UnErrorNoControladoDevuelve500GenericoSinDetallesInternos()
    {
        var respuesta = await _cliente.GetAsync("/prueba/inesperado");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secreto-interno", texto);
        Assert.DoesNotContain("StackTrace", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".cs", texto);
        using var doc = JsonDocument.Parse(texto);
        Assert.Equal("error_inesperado", doc.RootElement.GetProperty("codigo").GetString());
        Assert.Equal(respuesta.Headers.GetValues(Cabecera).Single(),
            doc.RootElement.GetProperty("idCorrelacion").GetString());
    }

    [Fact]
    public async Task UnaExcepcionDeCasoDeUsoConservaSuClasificacion()
    {
        var respuesta = await _cliente.GetAsync("/prueba/conflicto");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("conflicto", doc.RootElement.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task ElTraductorConvierteUnNoEncontradoEn404()
    {
        var respuesta = await _cliente.GetAsync("/prueba/no-encontrado");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("recurso_no_encontrado", doc.RootElement.GetProperty("codigo").GetString());
        Assert.Equal("/prueba/no-encontrado", doc.RootElement.GetProperty("instance").GetString());
    }

    /// <summary>Registra endpoints temporales después del pipeline de la API.</summary>
    private sealed class FiltroEndpointsTemporales : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(
            Action<IApplicationBuilder> siguiente) =>
            app =>
            {
                siguiente(app);
                app.Map("/prueba/ok", b => b.Run(async c =>
                    await c.Response.WriteAsync(c.RequestServices.GetRequiredService<IContextoEjecucion>().IdCorrelacion)));
                app.Map("/prueba/validacion", b => b.Run(c =>
                    throw new ExcepcionValidacion([new ErrorCampo("campo", "El campo es obligatorio.")],
                        c.RequestServices.GetRequiredService<IContextoEjecucion>().IdCorrelacion)));
                app.Map("/prueba/inesperado", b => b.Run(_ =>
                    throw new InvalidOperationException("secreto-interno en C:\\ruta\\Clase.cs")));
                app.Map("/prueba/conflicto", b => b.Run(c =>
                    throw new ExcepcionCasoDeUso(new ErrorUniforme(TipoError.Conflicto, "conflicto",
                        "Hay un conflicto.", null,
                        c.RequestServices.GetRequiredService<IContextoEjecucion>().IdCorrelacion))));
                app.Map("/prueba/no-encontrado", b => b.Run(c =>
                    TraductorProblemas.Traducir(
                        ErrorUniforme.NoEncontrado("No existe.",
                            c.RequestServices.GetRequiredService<IContextoEjecucion>().IdCorrelacion),
                        "/prueba/no-encontrado").ExecuteAsync(c)));
            };
    }
}
