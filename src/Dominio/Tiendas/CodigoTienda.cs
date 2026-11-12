using System.Text.RegularExpressions;

namespace BlackHellHeim.Dominio.Tiendas;

/// <summary>Código de tienda: exactamente tres letras ASCII de la A a la Z.</summary>
public sealed partial record CodigoTienda
{
    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Patron();

    private CodigoTienda(string valor)
    {
        Valor = valor;
    }

    public string Valor { get; }

    /// <summary>Recorta espacios, convierte a mayúsculas y valida la regla.</summary>
    /// <exception cref="ArgumentException">El valor no cumple la regla.</exception>
    public static CodigoTienda Crear(string valor)
    {
        var normalizado = (valor ?? string.Empty).Trim().ToUpperInvariant();
        if (!Patron().IsMatch(normalizado))
        {
            throw new ArgumentException(
                "El código debe tener exactamente tres letras mayúsculas de la A a la Z.", nameof(valor));
        }

        return new CodigoTienda(normalizado);
    }

    public override string ToString() => Valor;
}
