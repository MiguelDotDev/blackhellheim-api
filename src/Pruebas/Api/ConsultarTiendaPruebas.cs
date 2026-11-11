using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Pruebas en proceso de <c>GET /tiendas/{codigo}</c> (contrato de la historia 002b).</summary>
public class ConsultarTiendaPruebas : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _fabrica;
    private readonly HttpClient _cliente;

    public ConsultarTiendaPruebas(WebApplicationFactory<Program> fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task ConsultaUnaTiendaExistente()
    {
        var respuesta = await _cliente.GetAsync("/tiendas/BHH");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("BHH", cuerpo!["codigo"]);
        Assert.Equal("BlackHellHeim", cuerpo["nombre"]);
        Assert.Equal("MXN", cuerpo["moneda"]);
    }

    [Fact]
    public async Task UnaTiendaInexistenteDevuelve404ComoProblema()
    {
        var respuesta = await _cliente.GetAsync("/tiendas/ZZZ");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("recurso_no_encontrado", doc.RootElement.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task LaComprobacionDeSaludSigueIgual()
    {
        var respuesta = await _cliente.GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", cuerpo!["estado"]);
    }

    [Fact]
    public async Task UnCodigoInvalidoDevuelve400ConErroresPorCampo()
    {
        var respuesta = await _cliente.GetAsync("/tiendas/B");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = doc.RootElement;
        Assert.Equal("solicitud_invalida", raiz.GetProperty("codigo").GetString());
        Assert.Equal("El código debe tener exactamente tres letras mayúsculas.",
            raiz.GetProperty("errores").GetProperty("codigo")[0].GetString());
        Assert.Equal(respuesta.Headers.GetValues("X-Correlation-ID").Single(),
            raiz.GetProperty("idCorrelacion").GetString());
    }

    [Fact]
    public async Task ElRegistroDeEjecucionNoContieneLaCargaUtilNiDatosSensibles()
    {
        var captura = new CapturaDeRegistros();
        var cliente = _fabrica
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<ILoggerProvider>(captura)))
            .CreateClient();

        var respuesta = await cliente.GetAsync("/tiendas/SECRETO-XYZ");
        var id = respuesta.Headers.GetValues("X-Correlation-ID").Single();

        var mensaje = Assert.Single(captura.Mensajes, m => m.Contains("ConsultarTiendaQuery"));
        Assert.Contains(id, mensaje);
        Assert.DoesNotContain("SECRETO-XYZ", string.Concat(captura.Mensajes));
    }

    [Fact]
    public async Task LaCabeceraDeCorrelacionValidaSeConservaEnLaRespuesta()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/tiendas/BHH");
        peticion.Headers.Add("X-Correlation-ID", "abc-123");

        var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal("abc-123", respuesta.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Theory]
    [InlineData("con espacios <>")]
    public async Task UnaCabeceraInvalidaSeReemplazaPorUnaGenerada(string invalida)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/tiendas/BHH");
        peticion.Headers.TryAddWithoutValidation("X-Correlation-ID", invalida);

        var respuesta = await _cliente.SendAsync(peticion);

        var id = respuesta.Headers.GetValues("X-Correlation-ID").Single();
        Assert.NotEqual(invalida, id);
        Assert.InRange(id.Length, 1, 64);
    }

    [Fact]
    public async Task ElCuerpoDeErrorNoIncluyeTrazasNiRutasInternas()
    {
        var respuesta = await _cliente.GetAsync("/tiendas/B");

        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("StackTrace", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".cs", texto);
        Assert.DoesNotContain("traceId", texto);
        Assert.True(respuesta.Headers.Contains("X-Correlation-ID"));
    }

    private sealed class CapturaDeRegistros : ILoggerProvider
    {
        public List<string> Mensajes { get; } = [];

        public ILogger CreateLogger(string categoria) => new Registrador(this);

        public void Dispose()
        {
        }

        private sealed class Registrador(CapturaDeRegistros padre) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState estado) where TState : notnull => null;

            public bool IsEnabled(LogLevel nivel) => true;

            public void Log<TState>(LogLevel nivel, EventId evento, TState estado, Exception? excepcion,
                Func<TState, Exception?, string> formateador)
            {
                lock (padre.Mensajes)
                {
                    padre.Mensajes.Add(formateador(estado, excepcion));
                }
            }
        }
    }
}
