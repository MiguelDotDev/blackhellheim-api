using System.Text.RegularExpressions;

namespace BlackHellHeim.Dominio.Tiendas;

/// <summary>Entidad de ejemplo de solo lectura: código, nombre y moneda.</summary>
public sealed partial class Tienda
{
    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex PatronMoneda();

    // Permite el mapeo posterior de EF Core (historia 003) sin exponer un constructor público.
    private Tienda()
    {
        Codigo = null!;
        Nombre = string.Empty;
        Moneda = string.Empty;
    }

    public Tienda(CodigoTienda codigo, string nombre, string moneda)
    {
        Codigo = codigo ?? throw new ArgumentNullException(nameof(codigo));
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));
        }

        if (moneda is null || !PatronMoneda().IsMatch(moneda))
        {
            throw new ArgumentException("La moneda debe tener tres letras mayúsculas.", nameof(moneda));
        }

        Nombre = nombre;
        Moneda = moneda;
    }

    public CodigoTienda Codigo { get; private set; }

    public string Nombre { get; private set; }

    public string Moneda { get; private set; }
}
