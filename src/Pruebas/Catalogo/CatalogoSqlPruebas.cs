using System.Diagnostics;
using System.Net;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Precios;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Infraestructura.Persistencia;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using BlackHellHeim.Pruebas.Precios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BlackHellHeim.Pruebas.Catalogo;

/// <summary>Historia 019: catálogo público contra SQL Server real.</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class CatalogoSqlPruebas(ContenedorSqlServer sql)
{
    private static readonly DateTimeOffset Ahora = DateTimeOffset.UtcNow;
    private int _variantePrintful = 1000;

    private static ContextoBlackHellHeim Nuevo(string cadena) => new(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);

    private async Task<(IAsyncDisposable Fabrica, HttpClient Cliente, string Cadena, ProveedorTipoCambioFalso Proveedor)> PrepararAsync()
    {
        var cadena = await sql.CrearBaseAsync();
        await using (var c = Nuevo(cadena)) await c.Database.MigrateAsync();
        var proveedor = new ProveedorTipoCambioFalso { Tasa = new TasaProveedor(ServicioTipoCambio.Hoy(TimeProvider.System), 18m) };
        var fabrica = FabricaApiPruebas.Con(cadena).WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IProveedorTipoCambio>(proveedor))));
        return (fabrica, fabrica.CreateClient(), cadena, proveedor);
    }

    private Diseno Sembrar(ContextoBlackHellHeim c, ProductoBase producto, string nombre, EstadoDiseno estado,
        string? descripcion = null, params (string Color, string Talla, decimal Costo)[] variantes)
    {
        var arte = ArchivoArte.Crear($"https://a.test/{Guid.NewGuid():N}.png", $"{Guid.NewGuid():N}.png", "image/png", 10, 4500, 5400, Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), Ahora);
        var d = Diseno.Crear(nombre, descripcion, Ahora);
        d.AsignarArte(arte, Ahora);
        d.ReemplazarVariantes(variantes.Select(v => Variante.Crear(producto, _variantePrintful++, v.Color, "#111111", v.Talla, v.Costo)).ToList(), Ahora);
        if (estado != EstadoDiseno.Borrador) d.CambiarEstado(EstadoDiseno.Publicado, Ahora);
        if (estado == EstadoDiseno.Archivado) d.CambiarEstado(EstadoDiseno.Archivado, Ahora);
        c.Disenos.Add(d);
        return d;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();

    private static string[] Slugs(JsonElement pagina) => pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("slug").GetString()!).ToArray();

    private async Task<(IAsyncDisposable F, HttpClient C, string Cadena)> ConDatosAsync()
    {
        var (f, c, cadena, _) = await PrepararAsync();
        await using var ctx = Nuevo(cadena);
        var producto = ProductoBase.Crear(71, "Playera", "T-SHIRT");
        ctx.ProductosBase.Add(producto);
        Sembrar(ctx, producto, "Calavera épica", EstadoDiseno.Publicado, "Arte con cráneo", ("Black", "M", 10m), ("White", "S", 10m));
        Sembrar(ctx, producto, "Lobo nocturno", EstadoDiseno.Publicado, "100% lobo_azul", ("Black", "L", 20m));
        Sembrar(ctx, producto, "Dragón borrador", EstadoDiseno.Borrador, null, ("Black", "M", 10m));
        Sembrar(ctx, producto, "Fénix archivado", EstadoDiseno.Archivado, null, ("Black", "M", 10m));
        await ctx.SaveChangesAsync();
        return (f, c, cadena);
    }

    [Fact]
    public async Task SinSesionSoloListaLosPublicadosYNuncaExponeCostoNiIdsDePrintful()
    {
        var (f, cliente, _) = await ConDatosAsync();
        await using var _ = f;

        var r = await cliente.GetAsync("/catalogo/playeras");
        var texto = await r.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(["calavera-epica", "lobo-nocturno"], Slugs(JsonDocument.Parse(texto).RootElement).Order());
        Assert.DoesNotContain("costo", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("printful", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("margen", texto, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("public, max-age=60", r.Headers.CacheControl!.ToString());
    }

    [Theory]
    [InlineData("texto=calavera", "calavera-epica")]
    [InlineData("texto=CALAVERA", "calavera-epica")]
    [InlineData("texto=epica", "calavera-epica")] // insensible a acentos
    [InlineData("texto=cr%C3%A1neo", "calavera-epica")] // busca en la descripción
    [InlineData("texto=100%25", "lobo-nocturno")] // % literal, no comodín
    [InlineData("texto=lobo_a", "lobo-nocturno")] // _ literal
    [InlineData("texto=%27%3B%20DROP%20TABLE%20Disenos%3B--", "")] // intento de inyección: parametrizado
    [InlineData("color=white", "calavera-epica")]
    [InlineData("talla=l", "lobo-nocturno")]
    [InlineData("color=black&talla=s", "")] // misma variante: Black solo es M y L en la calavera, y S es White
    [InlineData("color=white&talla=s", "calavera-epica")]
    [InlineData("precioMin=500", "lobo-nocturno")]
    [InlineData("precioMax=400", "calavera-epica")]
    [InlineData("precioMin=330&precioMax=400", "")]
    public async Task LosFiltrosSeAplicanEnLaBaseYSeCombinanConY(string consulta, string esperados)
    {
        var (f, cliente, _) = await ConDatosAsync();
        await using var _ = f;

        var r = await cliente.GetAsync($"/catalogo/playeras?{consulta}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(esperados.Length == 0 ? [] : esperados.Split(','), Slugs(await Json(r)).Order());
    }

    [Fact]
    public async Task UnaVarianteInactivaNoCuentaParaFiltrosNiDetalle()
    {
        var (f, cliente, cadena) = await ConDatosAsync();
        await using var _ = f;
        await sql.ConsultarEscalarAsync<int>(cadena, "UPDATE dbo.Variantes SET Activa = 0 WHERE Color = 'White'; SELECT 1");

        var blanco = await Json(await cliente.GetAsync("/catalogo/playeras?color=white"));
        var detalle = await Json(await cliente.GetAsync("/catalogo/playeras/calavera-epica"));

        Assert.Empty(Slugs(blanco));
        Assert.Equal(1, detalle.GetProperty("variantes").GetArrayLength());
    }

    [Fact]
    public async Task UnDisenoSinVariantesActivasNoSeListaNiTieneDetalle()
    {
        var (f, cliente, cadena) = await ConDatosAsync();
        await using var _ = f;
        await sql.ConsultarEscalarAsync<int>(cadena, "UPDATE dbo.Variantes SET Activa = 0; SELECT 1");

        Assert.Empty(Slugs(await Json(await cliente.GetAsync("/catalogo/playeras"))));
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/catalogo/playeras/calavera-epica")).StatusCode);
    }

    [Fact]
    public async Task ElDetalleTraeImagenesColoresTallasYPrecios()
    {
        var (f, cliente, _) = await ConDatosAsync();
        await using var _ = f;

        var d = await Json(await cliente.GetAsync("/catalogo/playeras/calavera-epica"));

        Assert.Equal("Calavera épica", d.GetProperty("nombre").GetString());
        Assert.Equal(1, d.GetProperty("imagenes").GetArrayLength());
        Assert.Equal(["Black", "White"], d.GetProperty("colores").EnumerateArray().Select(c => c.GetProperty("nombre").GetString()));
        Assert.Equal(["S", "M"], d.GetProperty("tallas").EnumerateArray().Select(t => t.GetString()));
        Assert.All(d.GetProperty("variantes").EnumerateArray(), v => Assert.Equal(329m, v.GetProperty("precioMxn").GetDecimal()));
    }

    [Theory]
    [InlineData("dragon-borrador")]
    [InlineData("fenix-archivado")]
    [InlineData("no-existe")]
    public async Task UnSlugNoPublicadoOInexistenteDa404(string slug)
    {
        var (f, cliente, _) = await ConDatosAsync();
        await using var _ = f;

        var r = await cliente.GetAsync($"/catalogo/playeras/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("tamano=0")]
    [InlineData("tamano=51")]
    [InlineData("pagina=0")]
    [InlineData("orden=popular")]
    [InlineData("precioMin=10&precioMax=5")]
    [InlineData("pagina=abc")]
    public async Task LosParametrosInvalidosDan400(string consulta)
    {
        var (f, cliente, _) = await ConDatosAsync();
        await using var _ = f;

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.GetAsync($"/catalogo/playeras?{consulta}")).StatusCode);
    }

    [Fact]
    public async Task ElEtagPermiteResponder304()
    {
        var (f, cliente, _) = await ConDatosAsync();
        await using var _ = f;
        var primera = await cliente.GetAsync("/catalogo/playeras/calavera-epica");

        var peticion = new HttpRequestMessage(HttpMethod.Get, "/catalogo/playeras/calavera-epica");
        peticion.Headers.TryAddWithoutValidation("If-None-Match", primera.Headers.ETag!.Tag);
        var segunda = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.NotModified, segunda.StatusCode);
    }

    [Fact]
    public async Task SinTipoCambioElPrecioEsNullYNoHayError()
    {
        var (f, cliente, cadena, proveedor) = await PrepararAsync();
        await using var _ = f;
        await using (var ctx = Nuevo(cadena))
        {
            var producto = ProductoBase.Crear(71, "Playera", "T-SHIRT");
            ctx.ProductosBase.Add(producto);
            Sembrar(ctx, producto, "Sin precio", EstadoDiseno.Publicado, null, ("Black", "M", 10m));
            await ctx.SaveChangesAsync();
        }

        proveedor.Falla = true;
        var d = await Json(await cliente.GetAsync("/catalogo/playeras/sin-precio"));

        Assert.Equal(JsonValueKind.Null, d.GetProperty("variantes")[0].GetProperty("precioMxn").ValueKind);
    }

    [Fact]
    public async Task ElListadoDe12ConMilDisenosRespondeEnMenosDe300Ms()
    {
        var (f, cliente, cadena, _) = await PrepararAsync();
        await using var _ = f;
        await using (var ctx = Nuevo(cadena))
        {
            var producto = ProductoBase.Crear(71, "Playera", "T-SHIRT");
            ctx.ProductosBase.Add(producto);
            for (var i = 0; i < 1000; i++)
                Sembrar(ctx, producto, $"Diseño número {i:0000}", EstadoDiseno.Publicado, null, ("Black", "S", 10m + (i % 7)), ("Black", "M", 11m), ("White", "L", 12m));
            await ctx.SaveChangesAsync();
        }

        await cliente.GetAsync("/catalogo/playeras?tamano=12"); // calienta (JIT, plan de consulta, tipo de cambio)
        var reloj = Stopwatch.StartNew();
        var r = await cliente.GetAsync("/catalogo/playeras?tamano=12&orden=precio_asc");
        reloj.Stop();

        var pagina = await Json(r);
        Assert.Equal(1000, pagina.GetProperty("total").GetInt32());
        Assert.Equal(12, pagina.GetProperty("elementos").GetArrayLength());
        Assert.True(reloj.ElapsedMilliseconds < 300, $"tardó {reloj.ElapsedMilliseconds} ms");
    }
}
