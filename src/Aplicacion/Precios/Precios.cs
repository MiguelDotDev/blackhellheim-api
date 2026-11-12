using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using BlackHellHeim.Dominio.Precios;
using Mediator;

namespace BlackHellHeim.Aplicacion.Precios;

/// <summary>Dato publicado por el proveedor (Banxico FIX).</summary>
public sealed record TasaProveedor(DateOnly Fecha, decimal Valor);

/// <summary>Puerto del proveedor de tipo de cambio. Devuelve null si no hay dato; lanza si falla.</summary>
public interface IProveedorTipoCambio
{
    string Fuente { get; }

    Task<TasaProveedor?> ObtenerAsync(CancellationToken cancelacion);
}

public interface IAdvertencias
{
    void Advertir(string mensaje);
}

public interface IRepositorioPrecios
{
    Task<TipoCambioDiario?> ObtenerTipoCambioAsync(DateOnly fecha, CancellationToken cancelacion);

    Task<TipoCambioDiario?> ObtenerUltimoTipoCambioAsync(CancellationToken cancelacion);

    /// <summary>false si ya existía una fila de esa fecha (otra petición ganó).</summary>
    Task<bool> GuardarTipoCambioAsync(TipoCambioDiario tasa, CancellationToken cancelacion);

    Task<ParametrosPrecio> ObtenerParametrosVigentesAsync(CancellationToken cancelacion);

    Task AgregarParametrosAsync(ParametrosPrecio parametros, CancellationToken cancelacion);

    Task<decimal?> ObtenerCostoBaseVarianteAsync(Guid varianteId, CancellationToken cancelacion);
}

public sealed record TipoCambioResultado(decimal Valor, DateOnly Fecha, bool EsReserva);

/// <summary>Tipo de cambio de hoy (CDMX): una consulta al proveedor por día; si falla, usa el último guardado como reserva.</summary>
public sealed class ServicioTipoCambio(IProveedorTipoCambio proveedor, IRepositorioPrecios repositorio, TimeProvider reloj, IAdvertencias advertencias)
{
    public const decimal RangoMaximo = 0.20m;

    private static readonly TimeZoneInfo ZonaMexico = BuscarZona();

    public static DateOnly Hoy(TimeProvider reloj) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), ZonaMexico).DateTime);

    private static TimeZoneInfo BuscarZona()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"); }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // shortcut: sin tzdata se usa UTC-6 fijo (México no aplica horario de verano desde 2022); revisar si cambia la ley.
            return TimeZoneInfo.CreateCustomTimeZone("mexico-fijo", TimeSpan.FromHours(-6), "México", "México");
        }
    }

    public async Task<TipoCambioResultado?> ObtenerDelDiaAsync(CancellationToken cancelacion)
    {
        var hoy = Hoy(reloj);
        var deHoy = await repositorio.ObtenerTipoCambioAsync(hoy, cancelacion);
        if (deHoy is not null) return new(deHoy.Valor, deHoy.Fecha, false);

        var ultimo = await repositorio.ObtenerUltimoTipoCambioAsync(cancelacion);
        TasaProveedor? tasa = null;
        var fallo = false;
        try
        {
            tasa = await proveedor.ObtenerAsync(cancelacion);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            fallo = true;
            advertencias.Advertir($"El proveedor de tipo de cambio falló ({e.GetType().Name}); se usa el último valor guardado.");
        }

        if (tasa is not null && tasa.Valor > 0)
        {
            if (ultimo is not null && Math.Abs(tasa.Valor - ultimo.Valor) / ultimo.Valor > RangoMaximo)
            {
                advertencias.Advertir($"Tipo de cambio {tasa.Valor} fuera de ±{RangoMaximo:P0} del último guardado ({ultimo.Valor}); se conserva el anterior.");
            }
            else
            {
                var nuevo = TipoCambioDiario.Crear(tasa.Fecha, tasa.Valor, proveedor.Fuente, reloj.GetUtcNow());
                if (!await repositorio.GuardarTipoCambioAsync(nuevo, cancelacion))
                    nuevo = await repositorio.ObtenerTipoCambioAsync(tasa.Fecha, cancelacion) ?? nuevo; // ganó otra petición
                return new(nuevo.Valor, nuevo.Fecha, nuevo.Fecha != hoy);
            }
        }
        else if (!fallo)
        {
            advertencias.Advertir("El proveedor no publicó tipo de cambio hoy; se usa el último valor guardado.");
        }

        return ultimo is null ? null : new(ultimo.Valor, ultimo.Fecha, true);
    }
}

public sealed record ParametrosPrecioDto(decimal ColchonCambiario, decimal Margen, decimal ComisionPasarela, decimal ImpresionExtraUsd);

public sealed class ParametrosPrecioSolicitud
{
    public decimal? ColchonCambiario { get; init; }
    public decimal? Margen { get; init; }
    public decimal? ComisionPasarela { get; init; }
    public decimal? ImpresionExtraUsd { get; init; }
}

public sealed record DesglosePrecioDto(
    decimal CostoBaseUsd, decimal ImpresionExtraUsd, decimal TipoCambio, DateOnly TipoCambioFecha, bool TipoCambioObsoleto,
    decimal ColchonCambiario, decimal Margen, decimal ComisionPasarela, decimal CostoMxn, decimal PrecioBrutoMxn, decimal PrecioFinalMxn);

/// <summary>Cálculo ya preparado con el tipo de cambio y los parámetros de una petición (sin más consultas).</summary>
public interface ICalculoDePrecios
{
    DesglosePrecioDto Calcular(decimal costoBaseUsd);
}

/// <summary>Puerto de consumo para catálogo (HU-19) y pedidos (HU-26/28): el precio siempre lo calcula el backend.</summary>
public interface ICalculadoraPrecios
{
    /// <summary>Null si no hay tipo de cambio disponible (el llamador responde 503 o deja el precio en null; no inventa uno).</summary>
    Task<ICalculoDePrecios?> PrepararAsync(CancellationToken cancelacion);

    async Task<DesglosePrecioDto?> CalcularAsync(decimal costoBaseUsd, CancellationToken cancelacion) =>
        (await PrepararAsync(cancelacion))?.Calcular(costoBaseUsd);
}

public sealed class ServicioPrecios(ServicioTipoCambio tipoCambio, IRepositorioPrecios repositorio) : ICalculadoraPrecios
{
    public async Task<ICalculoDePrecios?> PrepararAsync(CancellationToken cancelacion)
    {
        var tasa = await tipoCambio.ObtenerDelDiaAsync(cancelacion);
        if (tasa is null) return null;
        return new Calculo(tasa, await repositorio.ObtenerParametrosVigentesAsync(cancelacion));
    }

    private sealed class Calculo(TipoCambioResultado tasa, ParametrosPrecio p) : ICalculoDePrecios
    {
        public DesglosePrecioDto Calcular(decimal costoBaseUsd)
        {
            var d = CalculadoraPrecios.Calcular(costoBaseUsd, p.ImpresionExtraUsd, tasa.Valor, p.ColchonCambiario, p.Margen, p.ComisionPasarela);
            return new(costoBaseUsd, p.ImpresionExtraUsd, tasa.Valor, tasa.Fecha, tasa.EsReserva, p.ColchonCambiario, p.Margen,
                p.ComisionPasarela, d.CostoMxn, d.PrecioBrutoMxn, d.PrecioFinalMxn);
        }
    }
}

public sealed record ObtenerParametrosPrecioQuery : IQuery<ResultadoCasoDeUso<ParametrosPrecioDto>>;

public sealed record ActualizarParametrosPrecioComando(ParametrosPrecioSolicitud Solicitud, string ActualizadoPor)
    : ICommand<ResultadoCasoDeUso<ParametrosPrecioDto>>;

public sealed record SimularPrecioQuery(Guid VarianteId) : IQuery<ResultadoCasoDeUso<DesglosePrecioDto>>;

public sealed class ValidadorActualizarParametrosPrecio : IValidadorSolicitud<ActualizarParametrosPrecioComando>
{
    public IReadOnlyList<ErrorCampo> Validar(ActualizarParametrosPrecioComando c)
    {
        var s = c.Solicitud;
        var errores = new List<ErrorCampo>();
        void Requerido(decimal? valor, string campo, decimal min, decimal max)
        {
            if (valor is null) errores.Add(new ErrorCampo(campo, "El campo es obligatorio."));
            else if (valor < min || valor > max) errores.Add(new ErrorCampo(campo, $"Debe estar entre {min} y {max}."));
        }

        Requerido(s.ColchonCambiario, "colchonCambiario", 0, 1);
        Requerido(s.Margen, "margen", 0, 0.9m);
        Requerido(s.ComisionPasarela, "comisionPasarela", 0, 0.5m);
        if (s.ImpresionExtraUsd is null) errores.Add(new ErrorCampo("impresionExtraUsd", "El campo es obligatorio."));
        else if (s.ImpresionExtraUsd < 0) errores.Add(new ErrorCampo("impresionExtraUsd", "No puede ser negativo."));
        if (errores.Count == 0 && s.Margen + s.ComisionPasarela >= 1)
            errores.Add(new ErrorCampo("margen", "Margen más comisión debe ser menor que 1."));
        return errores;
    }
}

public sealed class ManejadorObtenerParametrosPrecio(IRepositorioPrecios repositorio)
    : IQueryHandler<ObtenerParametrosPrecioQuery, ResultadoCasoDeUso<ParametrosPrecioDto>>
{
    public async ValueTask<ResultadoCasoDeUso<ParametrosPrecioDto>> Handle(ObtenerParametrosPrecioQuery q, CancellationToken cancelacion) =>
        ResultadoCasoDeUso<ParametrosPrecioDto>.Exito(ADto(await repositorio.ObtenerParametrosVigentesAsync(cancelacion)));

    internal static ParametrosPrecioDto ADto(ParametrosPrecio p) => new(p.ColchonCambiario, p.Margen, p.ComisionPasarela, p.ImpresionExtraUsd);
}

public sealed class ManejadorActualizarParametrosPrecio(IRepositorioPrecios repositorio, TimeProvider reloj)
    : ICommandHandler<ActualizarParametrosPrecioComando, ResultadoCasoDeUso<ParametrosPrecioDto>>
{
    public async ValueTask<ResultadoCasoDeUso<ParametrosPrecioDto>> Handle(ActualizarParametrosPrecioComando c, CancellationToken cancelacion)
    {
        var s = c.Solicitud;
        var nuevo = ParametrosPrecio.Crear(s.ColchonCambiario!.Value, s.Margen!.Value, s.ComisionPasarela!.Value,
            s.ImpresionExtraUsd!.Value, c.ActualizadoPor, reloj.GetUtcNow());
        await repositorio.AgregarParametrosAsync(nuevo, cancelacion);
        return ResultadoCasoDeUso<ParametrosPrecioDto>.Exito(ManejadorObtenerParametrosPrecio.ADto(nuevo));
    }
}

public sealed class ManejadorSimularPrecio(ICalculadoraPrecios calculadora, IRepositorioPrecios repositorio, IContextoEjecucion contexto)
    : IQueryHandler<SimularPrecioQuery, ResultadoCasoDeUso<DesglosePrecioDto>>
{
    public async ValueTask<ResultadoCasoDeUso<DesglosePrecioDto>> Handle(SimularPrecioQuery q, CancellationToken cancelacion)
    {
        var costo = await repositorio.ObtenerCostoBaseVarianteAsync(q.VarianteId, cancelacion);
        if (costo is null)
            return ResultadoCasoDeUso<DesglosePrecioDto>.Fallo(ErrorUniforme.NoEncontrado("No existe una variante con ese id.", contexto.IdCorrelacion));
        var desglose = await calculadora.CalcularAsync(costo.Value, cancelacion);
        return desglose is null
            ? ResultadoCasoDeUso<DesglosePrecioDto>.Fallo(ErrorUniforme.TipoCambioNoDisponible(contexto.IdCorrelacion))
            : ResultadoCasoDeUso<DesglosePrecioDto>.Exito(desglose);
    }
}
