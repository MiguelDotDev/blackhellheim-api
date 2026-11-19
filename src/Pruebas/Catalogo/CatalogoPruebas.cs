using BlackHellHeim.Aplicacion.Catalogo;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Precios;
using BlackHellHeim.Pruebas.Arte;
using BlackHellHeim.Pruebas.Precios;
using Xunit;

namespace BlackHellHeim.Pruebas.Catalogo;

internal sealed class ConsultasCatalogoFalsas : IConsultasCatalogo
{
    public List<FilaDiseno> Filas { get; } = [];

    public Task<IReadOnlyList<FilaDiseno>> ListarPublicadosAsync(string? texto, string? color, string? talla, CancellationToken c) =>
        Task.FromResult<IReadOnlyList<FilaDiseno>>(Filas);

    public Task<FilaDiseno?> ObtenerPublicadoAsync(string slug, CancellationToken c) =>
        Task.FromResult(Filas.FirstOrDefault(f => f.Slug == slug));
}

public class CatalogoPruebas
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);
    private readonly ConsultasCatalogoFalsas _consultas = new();
    private readonly RepositorioPreciosEnMemoria _repo = new();
    private readonly ProveedorTipoCambioFalso _proveedor = new() { Tasa = new TasaProveedor(new DateOnly(2026, 11, 11), 18m) };

    private ICalculadoraPrecios Precios() =>
        new ServicioPrecios(new ServicioTipoCambio(_proveedor, _repo, new RelojFijo(Ahora), new AdvertenciasFalsas()), _repo);

    private static FilaDiseno Fila(string nombre, int dia, params (string Color, string Talla, decimal Costo)[] variantes) =>
        new(new Guid(dia, 0, 0, [0, 0, 0, 0, 0, 0, 0, 1]), nombre.ToLowerInvariant().Replace(' ', '-'), nombre, "desc", $"https://a.test/{dia}.png",
            Ahora.AddDays(dia), variantes.Select(v => new FilaVariante(v.Color, "#000", v.Talla, v.Costo)).ToList());

    private async Task<PaginaPlayerasDto> Listar(ListarCatalogoQuery q) =>
        (await new ManejadorListarCatalogo(_consultas, Precios()).Handle(q, default)).Valor!;

    private static ListarCatalogoQuery Q(int? pagina = null, int? tamano = null, decimal? min = null, decimal? max = null, string? orden = null, string? color = null, string? talla = null) =>
        new(pagina, tamano, null, color, talla, min, max, orden);

    public CatalogoPruebas()
    {
        _consultas.Filas.Add(Fila("Beta", 1, ("Black", "M", 10m), ("White", "S", 12m)));   // precios 329 y 399
        _consultas.Filas.Add(Fila("Alfa", 2, ("Black", "M", 20m)));                          // 659
        _consultas.Filas.Add(Fila("Gama", 3, ("Red", "L", 10m)));                            // 329
    }

    [Fact]
    public async Task LaOrdenacionPorNovedadEsLaPorOmisionYMuestraElPrecioDesde()
    {
        var p = await Listar(Q());

        Assert.Equal(["Gama", "Alfa", "Beta"], p.Elementos.Select(e => e.Nombre));
        Assert.Equal(329m, p.Elementos[0].PrecioDesdeMxn);
        Assert.Equal(3, p.Total);
        Assert.Equal((1, 12), (p.Pagina, p.Tamano));
    }

    [Theory]
    [InlineData("precio_asc", "Beta,Gama,Alfa")] // 329 (Beta, Id 1) y 329 (Gama, Id 3): desempate por id
    [InlineData("precio_desc", "Alfa,Beta,Gama")]
    [InlineData("nombre", "Alfa,Beta,Gama")]
    public async Task OrdenaPorPrecioYNombreConDesempatePorId(string orden, string esperado) =>
        Assert.Equal(esperado.Split(','), (await Listar(Q(orden: orden))).Elementos.Select(e => e.Nombre));

    [Fact]
    public async Task ElRangoDePrecioConsideraLasVariantesYSusLimitesSonInclusivos()
    {
        Assert.Equal(["Gama", "Beta"], (await Listar(Q(min: 329, max: 329))).Elementos.Select(e => e.Nombre));
        Assert.Equal(["Alfa"], (await Listar(Q(min: 400, max: 659))).Elementos.Select(e => e.Nombre));
        Assert.Empty((await Listar(Q(min: 660))).Elementos);
    }

    [Fact]
    public async Task ElRangoDePrecioSoloMiraLasVariantesQueCumplenElColor()
    {
        // Beta tiene una variante White de 399; con color=Black esa ya no cuenta.
        Assert.Equal(["Beta"], (await Listar(Q(min: 390, max: 400))).Elementos.Select(e => e.Nombre));
        Assert.Empty((await Listar(Q(min: 390, max: 400, color: "Black"))).Elementos);
    }

    [Fact]
    public async Task LaPaginacionFueraDeRangoDaListaVaciaConElTotalCorrecto()
    {
        var p = await Listar(Q(pagina: 5, tamano: 2));
        Assert.Empty(p.Elementos);
        Assert.Equal(3, p.Total);
        Assert.Equal(["Beta"], (await Listar(Q(pagina: 2, tamano: 2))).Elementos.Select(e => e.Nombre));
    }

    [Fact]
    public async Task SinTipoCambioElPrecioEsNullYNoSeInventa()
    {
        _proveedor.Falla = true;
        var p = await Listar(Q(orden: "precio_asc"));
        Assert.All(p.Elementos, e => Assert.Null(e.PrecioDesdeMxn));
        Assert.Empty((await Listar(Q(min: 0))).Elementos); // con rango de precio nada califica sin precio
    }

    [Fact]
    public async Task ElDetalleTraeImagenesColoresTallasYPrecioPorVariante()
    {
        var r = await new ManejadorObtenerPlayera(_consultas, Precios(), new ContextoArteFalso()).Handle(new ObtenerPlayeraQuery("BETA"), default);

        var d = r.Valor!;
        Assert.Equal(["https://a.test/1.png"], d.Imagenes);
        Assert.Equal(["Black", "White"], d.Colores.Select(c => c.Nombre));
        Assert.Equal(["S", "M"], d.Tallas);
        Assert.Equal([329m, 399m], d.Variantes.Select(v => v.PrecioMxn));
        Assert.All(d.Variantes, v => Assert.True(v.Disponible));
    }

    [Fact]
    public async Task UnSlugInexistenteDa404()
    {
        var r = await new ManejadorObtenerPlayera(_consultas, Precios(), new ContextoArteFalso()).Handle(new ObtenerPlayeraQuery("no-existe"), default);
        Assert.Equal(TipoError.NoEncontrado, r.Error!.Tipo);
    }

    [Theory]
    [InlineData(0, null, null, null, null, "pagina")]
    [InlineData(null, 0, null, null, null, "tamano")]
    [InlineData(null, 51, null, null, null, "tamano")]
    [InlineData(null, null, 10.0, 5.0, null, "precioMin")]
    [InlineData(null, null, -1.0, null, null, "precioMin")]
    [InlineData(null, null, null, -1.0, null, "precioMax")]
    [InlineData(null, null, null, null, "popular", "orden")]
    public void ElValidadorRechazaParametrosInvalidos(int? pagina, int? tamano, double? min, double? max, string? orden, string campo)
    {
        var q = new ListarCatalogoQuery(pagina, tamano, null, null, null, (decimal?)min, (decimal?)max, orden);
        Assert.Contains(new ValidadorListarCatalogo().Validar(q), e => e.Campo == campo);
    }

    [Fact]
    public void ElValidadorRechazaTextoLargoYAceptaLosLimites()
    {
        var v = new ValidadorListarCatalogo();
        Assert.NotEmpty(v.Validar(new ListarCatalogoQuery(null, null, new string('a', 101), null, null, null, null, null)));
        Assert.Empty(v.Validar(new ListarCatalogoQuery(1, 50, new string('a', 100), null, null, 0m, 0m, "precio_desc")));
    }
}
