using System.Net;
using System.Text;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Infraestructura.Printful;
using BlackHellHeim.Pruebas.Aplicacion.Autenticacion;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Printful;

/// <summary>Historia 021: el adaptador con JSON supuesto de Printful v2 (sin verificar); nunca llama a Printful.</summary>
public class MockupsPrintfulPruebas
{
    private sealed class Manejador(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<(HttpMethod Metodo, Uri Url, string? Cuerpo, bool ConAutorizacion)> Llamadas { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
        {
            Llamadas.Add((r.Method, r.RequestUri!, r.Content is null ? null : await r.Content.ReadAsStringAsync(c), r.Headers.Authorization is not null));
            return responder(r);
        }
    }

    private static HttpResponseMessage Json(string texto, HttpStatusCode c = HttpStatusCode.OK) =>
        new(c) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };

    private static ProveedorImpresionPrintful Crear(Manejador m, OpcionesMockups? o = null, string idTienda = "", Microsoft.Extensions.Logging.ILogger<ProveedorImpresionPrintful>? log = null) =>
        new(new HttpClient(m), Options.Create(new OpcionesPrintful { Token = "token-de-prueba-no-real", IdTienda = idTienda }),
            new MemoryCache(new MemoryCacheOptions()), TimeProvider.System, new ContextoFalso(), o, log);

    private sealed class RegistroFalso : Microsoft.Extensions.Logging.ILogger<ProveedorImpresionPrintful>
    {
        public List<(Microsoft.Extensions.Logging.LogLevel Nivel, string Mensaje)> Entradas { get; } = [];
        public IDisposable? BeginScope<TState>(TState estado) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel nivel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel nivel, Microsoft.Extensions.Logging.EventId id, TState estado, Exception? ex, Func<TState, Exception?, string> f) =>
            Entradas.Add((nivel, f(estado, ex)));
    }

    private static readonly SolicitudMockup Pedido = new([new ProductoMockup(71, [1])], "https://x.test/a.png", "front", "dtg");

    [Fact]
    public async Task LaCabeceraDeTiendaSeEnviaAlCrearYConsultar()
    {
        var cabeceras = new List<string?>();
        var m = new Manejador(r =>
        {
            cabeceras.Add(r.Headers.TryGetValues("X-PF-Store-Id", out var v) ? v.Single() : null);
            return Json("""{"data":[{"id":7,"status":"pending"}]}""");
        });
        var p = Crear(m, idTienda: "123");
        await p.CrearTareaMockupAsync(Pedido, default);
        await p.ConsultarTareaMockupAsync("7", default);
        Assert.Equal(["123", "123"], cabeceras);
    }

    [Fact]
    public async Task SinIdDeTiendaNoSeEnviaLaCabeceraNiFalla()
    {
        var m = new Manejador(r =>
            Json(r.Headers.Contains("X-PF-Store-Id") ? "no" : """{"data":[{"id":7,"status":"pending"}]}"""));
        Assert.Equal("7", await Crear(m).CrearTareaMockupAsync(Pedido, default));
    }

    [Fact]
    public async Task UnRechazoDeTiendaSeRegistraEnWarningSinElToken()
    {
        var log = new RegistroFalso();
        var cuerpo = "This endpoint requires `store_id`!" + new string('x', 600);
        var m = new Manejador(_ => Json(cuerpo, HttpStatusCode.BadRequest));
        await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => Crear(m, log: log).CrearTareaMockupAsync(Pedido, default));
        var (nivel, mensaje) = Assert.Single(log.Entradas);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, nivel);
        Assert.Contains("400", mensaje);
        Assert.Contains("requires `store_id`", mensaje);
        Assert.DoesNotContain(new string('x', 500), mensaje); // truncado a 500 en total
        Assert.DoesNotContain("token-de-prueba-no-real", mensaje);
    }

    [Fact]
    public async Task CrearEnviaElProductoLasVariantesLaColocacionYLaCapaConElArte()
    {
        var m = new Manejador(_ => Json("""{"data":[{"id":4242,"status":"pending"}]}"""));

        var id = await Crear(m).CrearTareaMockupAsync(
            new SolicitudMockup([new ProductoMockup(71, [1, 2])], "https://almacen.test/arte/a.png", "front", "dtg"), default);

        Assert.Equal("4242", id);
        var (metodo, url, cuerpo, auth) = Assert.Single(m.Llamadas);
        Assert.Equal(HttpMethod.Post, metodo);
        Assert.Equal("/v2/mockup-tasks", url.AbsolutePath);
        Assert.True(auth);
        var p = JsonDocument.Parse(cuerpo!).RootElement.GetProperty("products")[0];
        Assert.Equal(71, p.GetProperty("catalog_product_id").GetInt32());
        Assert.Equal(2, p.GetProperty("catalog_variant_ids").GetArrayLength());
        var colocacion = p.GetProperty("placements")[0];
        Assert.Equal(("front", "dtg"), (colocacion.GetProperty("placement").GetString(), colocacion.GetProperty("technique").GetString()));
        Assert.Equal("https://almacen.test/arte/a.png", colocacion.GetProperty("layers")[0].GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("""{"data":[]}""")]
    [InlineData("""{"data":[{"status":"pending"}]}""")]
    [InlineData("no es json")]
    public async Task CrearConUnaRespuestaSinIdEsFalloDelProveedor(string cuerpo)
    {
        var proveedor = Crear(new Manejador(_ => Json(cuerpo)));

        var e = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() =>
            proveedor.CrearTareaMockupAsync(new SolicitudMockup([], "https://x.test/a.png", "front", "dtg"), default));
        Assert.Equal(TipoError.ProveedorFallo, e.Error.Tipo);
    }

    [Fact]
    public async Task ConsultarMapeaEstadosYMockups()
    {
        var m = new Manejador(_ => Json("""
            {"data":[{"id":4242,"status":"completed","catalog_variant_mockups":[
              {"catalog_variant_id":1,"mockups":[{"placement":"front","mockup_url":"https://files.cdn.printful.com/m1.png"}]},
              {"catalog_variant_id":2,"mockups":[{"placement":"front","mockup_url":"no-es-url"}]}]}]}
            """));

        var r = await Crear(m).ConsultarTareaMockupAsync("4242", default);

        Assert.Equal(EstadoMockupProveedor.Completada, r!.Estado);
        var mockup = Assert.Single(r.Mockups);
        Assert.Equal((1, "front"), (mockup.VarianteId, mockup.Posicion));
        Assert.Equal("id=4242", m.Llamadas[0].Url.Query.TrimStart('?'));
    }

    [Theory]
    [InlineData("pending", EstadoMockupProveedor.Pendiente)]
    [InlineData("otro", EstadoMockupProveedor.Pendiente)]
    [InlineData("failed", EstadoMockupProveedor.Fallida)]
    public async Task ConsultarMapeaLosEstadosDelProveedor(string estado, EstadoMockupProveedor esperado)
    {
        var proveedor = Crear(new Manejador(_ => Json($$"""{"data":[{"id":1,"status":"{{estado}}","failure_reasons":[{"message":"x"}]}]}""")));

        var r = await proveedor.ConsultarTareaMockupAsync("1", default);

        Assert.Equal(esperado, r!.Estado);
        Assert.Contains("message", r.Motivo);
    }

    [Fact]
    public async Task ConsultarUnaTareaDesconocidaDevuelveNull()
    {
        Assert.Null(await Crear(new Manejador(_ => new(HttpStatusCode.NotFound))).ConsultarTareaMockupAsync("1", default));
        Assert.Null(await Crear(new Manejador(_ => Json("""{"data":[]}"""))).ConsultarTareaMockupAsync("1", default));
    }

    // --- Descarga (anti-SSRF) ---
    [Theory]
    [InlineData("http://files.cdn.printful.com/a.png")]
    [InlineData("https://evil.example.com/a.png")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    public async Task UnaUrlQueNoEsHttpsDeUnHostPermitidoNoSeDescargaNiSeLlama(string url)
    {
        var m = new Manejador(_ => new(HttpStatusCode.OK));

        Assert.Null(await Crear(m).DescargarImagenAsync(new Uri(url), 1000, default));
        Assert.Empty(m.Llamadas);
    }

    [Fact]
    public async Task LaDescargaDeUnHostPermitidoDevuelveLosBytesSinEnviarElToken()
    {
        var m = new Manejador(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });

        var bytes = await Crear(m).DescargarImagenAsync(new Uri("https://FILES.cdn.printful.com/a.png"), 1000, default);

        Assert.Equal([1, 2, 3], bytes);
        Assert.False(m.Llamadas[0].ConAutorizacion);
    }

    [Fact]
    public async Task UnaImagenMayorAlMaximoSeDescarta()
    {
        var m = new Manejador(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[2000]) });

        Assert.Null(await Crear(m).DescargarImagenAsync(new Uri("https://files.cdn.printful.com/a.png"), 1000, default));
    }

    [Fact]
    public async Task UnaDescargaConErrorHttpEsFalloTransitorio()
    {
        var proveedor = Crear(new Manejador(_ => new(HttpStatusCode.Forbidden)));

        var e = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() =>
            proveedor.DescargarImagenAsync(new Uri("https://files.cdn.printful.com/a.png"), 1000, default));
        Assert.Equal(TipoError.ProveedorFallo, e.Error.Tipo);
    }

    [Fact]
    public async Task LosHostsPermitidosSonConfigurables()
    {
        var m = new Manejador(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent([9]) });
        var proveedor = Crear(m, new OpcionesMockups { HostsDescargaPermitidos = ["cdn.otro.test"] });

        Assert.Null(await proveedor.DescargarImagenAsync(new Uri("https://files.cdn.printful.com/a.png"), 10, default));
        Assert.Equal([9], await proveedor.DescargarImagenAsync(new Uri("https://cdn.otro.test/a.png"), 10, default));
    }
}
