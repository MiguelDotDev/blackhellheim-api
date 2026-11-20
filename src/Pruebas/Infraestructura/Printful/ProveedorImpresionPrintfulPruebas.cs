using System.Net;
using System.Text;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Infraestructura.Printful;
using BlackHellHeim.Pruebas.Aplicacion.Autenticacion;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Printful;

/// <summary>El adaptador con JSON de ejemplo de la API v2; nunca llama a Printful.</summary>
public class ProveedorImpresionPrintfulPruebas
{
    private const string Token = "token-de-prueba-no-real";

    private const string Variantes = """
        {"data":[
          {"id":1,"catalog_product_id":71,"size":"S","color":"Black","color_code":"#000000"},
          {"id":2,"catalog_product_id":71,"size":"M","color":"Black","color_code":"#000000"},
          {"id":3,"catalog_product_id":71,"size":"L","color":"White","color_code":"#ffffff"}]}
        """;
    private const string Disponibilidad = """
        {"data":[
          {"catalog_variant_id":1,"techniques":[{"technique":"dtg","selling_regions":[{"name":"mexico","availability":"in_stock"}]}]},
          {"catalog_variant_id":2,"techniques":[{"technique":"dtg","selling_regions":[{"name":"europe","availability":"in_stock"}]}]},
          {"catalog_variant_id":3,"techniques":[{"technique":"embroidery","selling_regions":[{"name":"worldwide","availability":"in_stock"}]}]}]}
        """;
    private const string Precios = """
        {"data":{"currency":"USD","variants":[
          {"id":1,"techniques":[{"technique_key":"dtg","price":"12.50"}]},
          {"id":2,"techniques":[{"technique_key":"dtg","price":"12.50"}]},
          {"id":3,"techniques":[{"technique_key":"embroidery","price":"15.00"}]}]}}
        """;

    private sealed class Manejador(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> Rutas { get; } = [];
        public List<HttpRequestMessage> Solicitudes { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
        {
            Rutas.Add(r.RequestUri!.PathAndQuery);
            Solicitudes.Add(r);
            return Task.FromResult(responder(r));
        }
    }

    private static HttpResponseMessage Json(string texto, HttpStatusCode c = HttpStatusCode.OK) =>
        new(c) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };

    private static ProveedorImpresionPrintful Crear(Manejador m, string token = Token) =>
        new(new HttpClient(m), Options.Create(new OpcionesPrintful { Token = token }),
            new MemoryCache(new MemoryCacheOptions()), TimeProvider.System, new ContextoFalso());

    private static HttpResponseMessage Catalogo(HttpRequestMessage r) => r.RequestUri!.AbsolutePath switch
    {
        var p when p.EndsWith("/catalog-variants") => Json(Variantes),
        var p when p.EndsWith("/availability") => Json(Disponibilidad),
        var p when p.EndsWith("/prices") => Json(Precios),
        _ => new HttpResponseMessage(HttpStatusCode.NotFound)
    };

    private const string UnaPlayera = """{"data":[{"id":71,"type":"T-SHIRT","name":"Unisex Staple T-Shirt"}]}""";

    [Fact]
    public async Task Playeras_FiltraEnPrintfulPorCategoriasYPaginaConOffset()
    {
        var m = new Manejador(_ => Json(UnaPlayera));

        var lista = await Crear(m).ListarProductosAsync("playeras", 2, 20, default);

        Assert.Single(lista);
        Assert.Equal("/v2/catalog-products?limit=20&offset=20&category_ids=24,32,38", m.Rutas.Single());
    }

    [Fact]
    public async Task CategoriaNumerica_SePasaTalCual()
    {
        var m = new Manejador(_ => Json(UnaPlayera));

        await Crear(m).ListarProductosAsync("24", 1, 20, default);

        Assert.EndsWith("&category_ids=24", m.Rutas.Single());
    }

    [Fact]
    public async Task ListaRealmenteVacia_DevuelveVacio()
    {
        var lista = await Crear(new Manejador(_ => Json("""{"data":[]}"""))).ListarProductosAsync("playeras", 1, 20, default);

        Assert.Empty(lista);
    }

    [Fact]
    public async Task FiltraPorTecnicaYPaisYMapeaLosCampos()
    {
        var proveedor = Crear(new Manejador(Catalogo));

        var variantes = await proveedor.ListarVariantesAsync(71, "dtg", "MX", default);

        var unica = Assert.Single(variantes!);
        Assert.Equal(new(1, 71, "Black", "#000000", "S", 12.50m, "dtg", true), unica);
    }

    [Fact]
    public async Task UnPaisSinRegionMapeadaSoloVeLoDisponibleEnTodoElMundo()
    {
        var proveedor = Crear(new Manejador(Catalogo));

        Assert.Empty((await proveedor.ListarVariantesAsync(71, "dtg", "FR", default))!);
        Assert.Single((await proveedor.ListarVariantesAsync(71, "embroidery", "FR", default))!);
    }

    [Fact]
    public async Task SinCoincidenciasDevuelveListaVacia()
    {
        var proveedor = Crear(new Manejador(Catalogo));

        Assert.Empty((await proveedor.ListarVariantesAsync(71, "sublimation", "MX", default))!);
    }

    [Fact]
    public async Task UnProductoInexistenteDaNull()
    {
        var proveedor = Crear(new Manejador(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.Null(await proveedor.ObtenerProductoAsync(9, default));
        Assert.Null(await proveedor.ListarVariantesAsync(9, "dtg", "MX", default));
    }

    [Fact]
    public async Task ObtenerProductoMapeaTecnicasYUsaCache()
    {
        var m = new Manejador(_ => Json("""{"data":{"id":71,"name":"Unisex T-Shirt","type":"T-SHIRT","techniques":[{"key":"dtg"},{"key":"embroidery"}]}}"""));
        var proveedor = Crear(m);

        var uno = await proveedor.ObtenerProductoAsync(71, default);
        await proveedor.ObtenerProductoAsync(71, default);

        Assert.Equal([71], [uno!.Id]);
        Assert.Equal(["dtg", "embroidery"], uno.Tecnicas);
        Assert.Single(m.Rutas);
    }

    [Fact]
    public async Task ListarProductosTextoLibreFiltraPorTipoYEnviaLaPaginacion()
    {
        var m = new Manejador(_ => Json("""{"data":[{"id":71,"name":"Unisex T-Shirt","type":"T-SHIRT","techniques":[]},{"id":9,"name":"Mug","type":"MUG","techniques":[]}]}"""));
        var proveedor = Crear(m);

        var productos = await proveedor.ListarProductosAsync("mug", 3, 10, default);

        Assert.Equal([9], productos.Select(p => p.Id));
        Assert.Contains("limit=10&offset=20", m.Rutas.Single());
    }

    [Fact]
    public async Task ElTokenSeEnviaComoBearerYNuncaApareceEnLosErrores()
    {
        var m = new Manejador(_ => Json("{}", HttpStatusCode.Unauthorized));
        var proveedor = Crear(m);

        var ex = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => proveedor.ObtenerProductoAsync(1, default));

        Assert.Equal("Bearer " + Token, m.Solicitudes.Single().Headers.Authorization!.ToString());
        Assert.Equal(TipoError.ProveedorFallo, ex.Error.Tipo);
        Assert.DoesNotContain(Token, ex.Error.Mensaje + ex.Message);
    }

    [Fact]
    public async Task SinTokenConfiguradoDaErrorSinLlamarAlProveedor()
    {
        var m = new Manejador(_ => Json("{}"));
        var proveedor = Crear(m, token: "");

        var ex = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => proveedor.ObtenerProductoAsync(1, default));

        Assert.Equal(TipoError.ProveedorFallo, ex.Error.Tipo);
        Assert.Empty(m.Rutas);
    }

    [Fact]
    public async Task UnJsonInesperadoDa502()
    {
        var proveedor = Crear(new Manejador(_ => Json("esto no es json")));

        var ex = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => proveedor.ObtenerProductoAsync(1, default));
        Assert.Equal(TipoError.ProveedorFallo, ex.Error.Tipo);

        var sinId = Crear(new Manejador(_ => Json("""{"data":{"name":"x"}}""")));
        Assert.Equal(TipoError.ProveedorFallo,
            (await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => sinId.ObtenerProductoAsync(1, default))).Error.Tipo);
    }

    [Fact]
    public async Task UnaMonedaDistintaDeUsdSeRechaza()
    {
        var proveedor = Crear(new Manejador(r => r.RequestUri!.AbsolutePath.EndsWith("/prices")
            ? Json("""{"data":{"currency":"EUR","variants":[]}}""") : Catalogo(r)));

        var ex = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => proveedor.ListarVariantesAsync(71, "dtg", "MX", default));
        Assert.Equal(TipoError.ProveedorFallo, ex.Error.Tipo);
    }

    [Fact]
    public async Task UnLimiteAgotadoDa503ConElTiempoSugerido()
    {
        var proveedor = Crear(new Manejador(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            return r;
        }));

        var ex = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => proveedor.ObtenerProductoAsync(1, default));

        Assert.Equal(TipoError.ProveedorSaturado, ex.Error.Tipo);
        Assert.Equal(7, ex.Error.ReintentarEnSegundos);
    }

    [Fact]
    public async Task UnTiempoAgotadoDa504()
    {
        var proveedor = new ProveedorImpresionPrintful(
            new HttpClient(new Lanza(new TimeoutException())), Options.Create(new OpcionesPrintful { Token = Token }),
            new MemoryCache(new MemoryCacheOptions()), TimeProvider.System, new ContextoFalso());

        var ex = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => proveedor.ObtenerProductoAsync(1, default));
        Assert.Equal(TipoError.ProveedorTiempoAgotado, ex.Error.Tipo);
    }

    [Theory]
    [InlineData("http://insegura.example")]
    [InlineData("no-es-url")]
    public void LasOpcionesRechazanUnaUrlQueNoSeaHttps(string url) =>
        Assert.Throws<InvalidOperationException>(() => new OpcionesPrintful { UrlBase = url }.Validar());

    private sealed class Lanza(Exception e) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c) => throw e;
    }
}
