using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Precios;
using BlackHellHeim.Dominio.Precios;
using BlackHellHeim.Infraestructura.Precios;
using BlackHellHeim.Pruebas.Arte;
using Xunit;

namespace BlackHellHeim.Pruebas.Precios;

internal sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => Ahora;
}

internal sealed class ProveedorTipoCambioFalso : IProveedorTipoCambio
{
    public string Fuente => "falso";
    public TasaProveedor? Tasa { get; set; }
    public bool Falla { get; set; }
    public int Llamadas { get; private set; }

    public Task<TasaProveedor?> ObtenerAsync(CancellationToken cancelacion)
    {
        Llamadas++;
        return Falla ? throw new HttpRequestException("caído") : Task.FromResult(Tasa);
    }
}

internal sealed class AdvertenciasFalsas : IAdvertencias
{
    public List<string> Mensajes { get; } = [];
    public void Advertir(string mensaje) => Mensajes.Add(mensaje);
}

internal sealed class RepositorioPreciosEnMemoria : IRepositorioPrecios
{
    public List<TipoCambioDiario> Tasas { get; } = [];
    public List<ParametrosPrecio> Parametros { get; } = [ParametrosPrecio.Crear(0.03m, 0.40m, 0.036m, 0m, "semilla", DateTimeOffset.UnixEpoch)];
    public Dictionary<Guid, decimal> Costos { get; } = [];
    public bool PierdeLaCarrera { get; set; }

    public Task<TipoCambioDiario?> ObtenerTipoCambioAsync(DateOnly fecha, CancellationToken c) => Task.FromResult(Tasas.FirstOrDefault(t => t.Fecha == fecha));

    public Task<TipoCambioDiario?> ObtenerUltimoTipoCambioAsync(CancellationToken c) => Task.FromResult(Tasas.OrderByDescending(t => t.Fecha).FirstOrDefault());

    public Task<bool> GuardarTipoCambioAsync(TipoCambioDiario tasa, CancellationToken c)
    {
        if (PierdeLaCarrera)
        {
            Tasas.Add(TipoCambioDiario.Crear(tasa.Fecha, tasa.Valor + 0.5m, "otra petición", tasa.ObtenidoUtc));
            return Task.FromResult(false);
        }

        Tasas.Add(tasa);
        return Task.FromResult(true);
    }

    public Task<ParametrosPrecio> ObtenerParametrosVigentesAsync(CancellationToken c) =>
        Task.FromResult(Parametros.OrderByDescending(p => p.VigenteDesdeUtc).First());

    public Task AgregarParametrosAsync(ParametrosPrecio p, CancellationToken c)
    {
        Parametros.Add(p);
        return Task.CompletedTask;
    }

    public Task<decimal?> ObtenerCostoBaseVarianteAsync(Guid id, CancellationToken c) =>
        Task.FromResult(Costos.TryGetValue(id, out var costo) ? costo : (decimal?)null);
}

public class ServicioTipoCambioPruebas
{
    // 2026-11-11 15:00 UTC = 09:00 en Ciudad de México.
    private static readonly DateTimeOffset Manana = new(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = new(2026, 11, 11);

    private readonly ProveedorTipoCambioFalso _proveedor = new() { Tasa = new TasaProveedor(Hoy, 18.5m) };
    private readonly RepositorioPreciosEnMemoria _repo = new();
    private readonly AdvertenciasFalsas _avisos = new();
    private readonly RelojFijo _reloj = new(Manana);

    private ServicioTipoCambio Crear() => new(_proveedor, _repo, _reloj, _avisos);

    [Fact]
    public async Task ObtieneDelProveedorYLoGuarda()
    {
        var r = await Crear().ObtenerDelDiaAsync(default);

        Assert.Equal(new TipoCambioResultado(18.5m, Hoy, false), r);
        Assert.Single(_repo.Tasas);
    }

    [Fact]
    public async Task LasConsultasPosterioresDelDiaUsanLoGuardadoSinLlamarAlProveedor()
    {
        await Crear().ObtenerDelDiaAsync(default);
        var r = await Crear().ObtenerDelDiaAsync(default);

        Assert.Equal(18.5m, r!.Valor);
        Assert.Equal(1, _proveedor.Llamadas);
    }

    [Fact]
    public async Task SiElProveedorFallaUsaElUltimoGuardadoComoReservaYAdvierte()
    {
        _repo.Tasas.Add(TipoCambioDiario.Crear(Hoy.AddDays(-3), 18.0m, "falso", Manana));
        _proveedor.Falla = true;

        var r = await Crear().ObtenerDelDiaAsync(default);

        Assert.Equal(new TipoCambioResultado(18.0m, Hoy.AddDays(-3), true), r);
        Assert.Single(_avisos.Mensajes);
        Assert.Single(_repo.Tasas);
    }

    [Fact]
    public async Task SinDatoDelProveedorUsaLaReserva()
    {
        _repo.Tasas.Add(TipoCambioDiario.Crear(Hoy.AddDays(-1), 18.0m, "falso", Manana));
        _proveedor.Tasa = null;

        var r = await Crear().ObtenerDelDiaAsync(default);

        Assert.True(r!.EsReserva);
        Assert.Single(_avisos.Mensajes);
    }

    [Fact]
    public async Task SinDatoNiGuardadoDevuelveNull()
    {
        _proveedor.Falla = true;
        Assert.Null(await Crear().ObtenerDelDiaAsync(default));
    }

    [Theory]
    [InlineData("21.60", false)] // +20 % exacto: se acepta
    [InlineData("21.61", true)]
    [InlineData("14.40", false)] // −20 % exacto
    [InlineData("14.39", true)]
    public async Task UnValorFueraDeMasMenos20PorCientoSeRechazaYConservaElAnterior(string valor, bool rechazado)
    {
        _repo.Tasas.Add(TipoCambioDiario.Crear(Hoy.AddDays(-1), 18.0m, "falso", Manana));
        _proveedor.Tasa = new TasaProveedor(Hoy, decimal.Parse(valor));

        var r = await Crear().ObtenerDelDiaAsync(default);

        Assert.Equal(rechazado, r!.EsReserva);
        Assert.Equal(rechazado ? 18.0m : decimal.Parse(valor), r.Valor);
        Assert.Equal(rechazado ? 1 : 2, _repo.Tasas.Count);
        Assert.Equal(rechazado, _avisos.Mensajes.Count == 1);
    }

    [Fact]
    public async Task SiOtraPeticionGanaLaCarreraSeUsaSuFilaYQuedaUnaSola()
    {
        _repo.PierdeLaCarrera = true;

        var r = await Crear().ObtenerDelDiaAsync(default);

        Assert.Equal(19.0m, r!.Valor);
        Assert.Single(_repo.Tasas);
    }

    [Fact]
    public async Task UnDatoDeUnaFechaAnteriorSeGuardaPeroSeMarcaComoReserva()
    {
        _proveedor.Tasa = new TasaProveedor(Hoy.AddDays(-1), 18.2m); // fin de semana: Banxico repite el último día hábil

        var r = await Crear().ObtenerDelDiaAsync(default);

        Assert.True(r!.EsReserva);
        Assert.Equal(Hoy.AddDays(-1), r.Fecha);
    }

    [Fact]
    public void ElDiaCambiaALaMedianocheDeCiudadDeMexicoNoDeUtc()
    {
        // 05:59 UTC del 12 = 23:59 del 11 en CDMX; 06:00 UTC = 00:00 del 12.
        Assert.Equal(new DateOnly(2026, 11, 11), ServicioTipoCambio.Hoy(new RelojFijo(new(2026, 11, 12, 5, 59, 0, TimeSpan.Zero))));
        Assert.Equal(new DateOnly(2026, 11, 12), ServicioTipoCambio.Hoy(new RelojFijo(new(2026, 11, 12, 6, 0, 0, TimeSpan.Zero))));
    }
}

public class ManejadoresPreciosPruebas
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);
    private readonly RepositorioPreciosEnMemoria _repo = new();
    private readonly ProveedorTipoCambioFalso _proveedor = new() { Tasa = new TasaProveedor(new DateOnly(2026, 11, 11), 18m) };

    private ICalculadoraPrecios Calculadora() =>
        new ServicioPrecios(new ServicioTipoCambio(_proveedor, _repo, new RelojFijo(Ahora), new AdvertenciasFalsas()), _repo);

    [Fact]
    public async Task SimularDevuelveElDesgloseConLosParametrosVigentes()
    {
        var id = Guid.NewGuid();
        _repo.Costos[id] = 10m;

        var r = await new ManejadorSimularPrecio(Calculadora(), _repo, new ContextoArteFalso()).Handle(new SimularPrecioQuery(id), default);

        Assert.True(r.EsExito);
        Assert.Equal(329m, r.Valor!.PrecioFinalMxn);
        Assert.Equal(185.4m, r.Valor.CostoMxn);
        Assert.False(r.Valor.TipoCambioObsoleto);
    }

    [Fact]
    public async Task SimularUnaVarianteInexistenteDa404()
    {
        var r = await new ManejadorSimularPrecio(Calculadora(), _repo, new ContextoArteFalso()).Handle(new SimularPrecioQuery(Guid.NewGuid()), default);
        Assert.Equal(TipoError.NoEncontrado, r.Error!.Tipo);
    }

    [Fact]
    public async Task SimularSinTipoCambioDa503()
    {
        var id = Guid.NewGuid();
        _repo.Costos[id] = 10m;
        _proveedor.Falla = true;

        var r = await new ManejadorSimularPrecio(Calculadora(), _repo, new ContextoArteFalso()).Handle(new SimularPrecioQuery(id), default);

        Assert.Equal(TipoError.NoDisponible, r.Error!.Tipo);
        Assert.Equal("tipo_cambio_no_disponible", r.Error.Codigo);
    }

    [Fact]
    public async Task ActualizarAgregaUnaVersionNuevaYLaVuelveVigente()
    {
        var manejador = new ManejadorActualizarParametrosPrecio(_repo, new RelojFijo(Ahora));
        var s = new ParametrosPrecioSolicitud { ColchonCambiario = 0.05m, Margen = 0.5m, ComisionPasarela = 0.04m, ImpresionExtraUsd = 1m };

        var r = await manejador.Handle(new ActualizarParametrosPrecioComando(s, "admin-1"), default);

        Assert.Equal(new ParametrosPrecioDto(0.05m, 0.5m, 0.04m, 1m), r.Valor);
        Assert.Equal(2, _repo.Parametros.Count);
        var vigentes = await new ManejadorObtenerParametrosPrecio(_repo).Handle(new ObtenerParametrosPrecioQuery(), default);
        Assert.Equal(0.5m, vigentes.Valor!.Margen);
    }

    [Theory]
    [InlineData(null, 0.4, 0.036, 0.0, "colchonCambiario")]
    [InlineData(0.03, null, 0.036, 0.0, "margen")]
    [InlineData(0.03, 0.4, null, 0.0, "comisionPasarela")]
    [InlineData(0.03, 0.4, 0.036, null, "impresionExtraUsd")]
    [InlineData(0.03, 0.4, 0.036, -1.0, "impresionExtraUsd")]
    [InlineData(1.5, 0.4, 0.036, 0.0, "colchonCambiario")]
    [InlineData(0.03, 0.9, 0.2, 0.0, "margen")]
    public void ElValidadorRechazaParametrosIncompletosOFueraDeRango(double? colchon, double? margen, double? comision, double? extra, string campo)
    {
        decimal? D(double? v) => v is null ? null : (decimal)v;
        var s = new ParametrosPrecioSolicitud { ColchonCambiario = D(colchon), Margen = D(margen), ComisionPasarela = D(comision), ImpresionExtraUsd = D(extra) };

        var errores = new ValidadorActualizarParametrosPrecio().Validar(new ActualizarParametrosPrecioComando(s, "a"));

        Assert.Contains(errores, e => e.Campo == campo);
    }
}

public class ProveedorBanxicoPruebas
{
    [Fact]
    public void MapeaLaRespuestaDeLaApiSie()
    {
        var json = """{"bmx":{"series":[{"idSerie":"SF43718","titulo":"Tipo de cambio FIX","datos":[{"fecha":"11/11/2026","dato":"18.4321"}]}]}}""";
        Assert.Equal(new TasaProveedor(new DateOnly(2026, 11, 11), 18.4321m), ProveedorTipoCambioBanxico.MapearJson(json));
    }

    [Theory]
    [InlineData("""{"bmx":{"series":[{"datos":[{"fecha":"11/11/2026","dato":"N/E"}]}]}}""")]
    [InlineData("""{"bmx":{"series":[{"datos":[]}]}}""")]
    [InlineData("""{"bmx":{"series":[]}}""")]
    [InlineData("""{}""")]
    [InlineData("""{"bmx":{"series":[{"datos":[{"fecha":"mañana","dato":"18.1"}]}]}}""")]
    [InlineData("""{"bmx":{"series":[{"datos":[{"fecha":"11/11/2026","dato":"-1"}]}]}}""")]
    public void SinDatoUtilDevuelveNull(string json) => Assert.Null(ProveedorTipoCambioBanxico.MapearJson(json));

    [Fact]
    public async Task SinTokenFallaSinLlamarALaRed()
    {
        var proveedor = new ProveedorTipoCambioBanxico(new HttpClient(), new OpcionesBanxico());
        await Assert.ThrowsAsync<InvalidOperationException>(() => proveedor.ObtenerAsync(default));
    }
}
