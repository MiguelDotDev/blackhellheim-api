namespace BlackHellHeim.Dominio.Precios;

/// <summary>Redondeo comercial: entero terminado en 9, siempre hacia arriba (decisión de producto, HU-20).</summary>
public static class RedondeoComercial
{
    /// <summary>Menor entero terminado en 9 que sea mayor o igual al precio bruto (0 → 9).</summary>
    public static decimal AEntero9(decimal bruto)
    {
        if (bruto < 0) throw new ArgumentOutOfRangeException(nameof(bruto), "El precio bruto no puede ser negativo.");
        return bruto <= 9 ? 9 : (Math.Ceiling((bruto - 9) / 10m) * 10m) + 9m;
    }
}

public sealed record DesglosePrecio(decimal CostoMxn, decimal PrecioBrutoMxn, decimal PrecioFinalMxn);

/// <summary>Fórmula de la constitución V con <c>decimal</c>; recibe la tasa como dato y nunca consulta el reloj.</summary>
public static class CalculadoraPrecios
{
    public static DesglosePrecio Calcular(decimal costoBaseUsd, decimal impresionExtraUsd, decimal tipoCambio,
        decimal colchonCambiario, decimal margen, decimal comisionPasarela)
    {
        if (costoBaseUsd < 0) throw new ArgumentOutOfRangeException(nameof(costoBaseUsd), "El costo base no puede ser negativo.");
        if (impresionExtraUsd < 0) throw new ArgumentOutOfRangeException(nameof(impresionExtraUsd), "El extra de impresión no puede ser negativo.");
        if (tipoCambio <= 0) throw new ArgumentOutOfRangeException(nameof(tipoCambio), "El tipo de cambio debe ser mayor que cero.");
        ParametrosPrecio.Validar(colchonCambiario, margen, comisionPasarela, impresionExtraUsd);

        var costoMxn = (costoBaseUsd + impresionExtraUsd) * tipoCambio * (1 + colchonCambiario);
        var bruto = costoMxn / (1 - margen - comisionPasarela);
        return new DesglosePrecio(costoMxn, bruto, RedondeoComercial.AEntero9(bruto));
    }
}

/// <summary>Parámetros de precio versionados: el vigente es el de <see cref="VigenteDesdeUtc"/> más reciente.</summary>
public sealed class ParametrosPrecio
{
    private ParametrosPrecio() { }

    public Guid Id { get; private set; }
    public decimal ColchonCambiario { get; private set; }
    public decimal Margen { get; private set; }
    public decimal ComisionPasarela { get; private set; }
    public decimal ImpresionExtraUsd { get; private set; }
    public DateTimeOffset VigenteDesdeUtc { get; private set; }
    public string ActualizadoPor { get; private set; } = "";

    public static void Validar(decimal colchon, decimal margen, decimal comision, decimal extraUsd)
    {
        if (colchon is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(colchon), "El colchón cambiario debe estar entre 0 y 1.");
        if (margen is < 0 or > 0.9m) throw new ArgumentOutOfRangeException(nameof(margen), "El margen debe estar entre 0 y 0.9.");
        if (comision is < 0 or > 0.5m) throw new ArgumentOutOfRangeException(nameof(comision), "La comisión debe estar entre 0 y 0.5.");
        if (margen + comision >= 1) throw new ArgumentOutOfRangeException(nameof(margen), "Margen más comisión debe ser menor que 1.");
        if (extraUsd < 0) throw new ArgumentOutOfRangeException(nameof(extraUsd), "El extra de impresión no puede ser negativo.");
    }

    public static ParametrosPrecio Crear(decimal colchon, decimal margen, decimal comision, decimal extraUsd,
        string actualizadoPor, DateTimeOffset ahora)
    {
        Validar(colchon, margen, comision, extraUsd);
        return new ParametrosPrecio
        {
            Id = Guid.NewGuid(), ColchonCambiario = colchon, Margen = margen, ComisionPasarela = comision,
            ImpresionExtraUsd = extraUsd, VigenteDesdeUtc = ahora, ActualizadoPor = actualizadoPor
        };
    }
}

/// <summary>Tipo de cambio de una fecha (calendario de Ciudad de México); una fila por fecha.</summary>
public sealed class TipoCambioDiario
{
    private TipoCambioDiario() { }

    public Guid Id { get; private set; }
    public DateOnly Fecha { get; private set; }
    public decimal Valor { get; private set; }
    public string Fuente { get; private set; } = "";
    public DateTimeOffset ObtenidoUtc { get; private set; }

    public static TipoCambioDiario Crear(DateOnly fecha, decimal valor, string fuente, DateTimeOffset ahora)
    {
        if (valor <= 0) throw new ArgumentOutOfRangeException(nameof(valor), "El tipo de cambio debe ser mayor que cero.");
        return new TipoCambioDiario { Id = Guid.NewGuid(), Fecha = fecha, Valor = valor, Fuente = fuente, ObtenidoUtc = ahora };
    }
}
