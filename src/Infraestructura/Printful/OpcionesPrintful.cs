namespace BlackHellHeim.Infraestructura.Printful;

/// <summary>Sección <c>Printful</c> (variables <c>Printful__*</c>). El token nunca se versiona.</summary>
public sealed class OpcionesPrintful
{
    public string Token { get; set; } = "";
    public string UrlBase { get; set; } = "https://api.printful.com";
    public int TiempoEsperaSegundos { get; set; } = 10;
    public int MinutosCache { get; set; } = 10;

    /// <summary>Regiones de venta de Printful que cubren cada país; "worldwide" siempre aplica. A verificar con token real.</summary>
    public Dictionary<string, string[]> RegionesPorPais { get; set; } = new()
    {
        ["MX"] = ["mexico", "north_america", "worldwide"],
        ["US"] = ["usa", "north_america", "worldwide"],
        ["CA"] = ["canada", "north_america", "worldwide"]
    };

    public void Validar()
    {
        if (!Uri.TryCreate(UrlBase, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Printful__UrlBase debe ser una URL https absoluta.");
        }

        if (TiempoEsperaSegundos is < 1 or > 120)
        {
            throw new InvalidOperationException("Printful__TiempoEsperaSegundos debe estar entre 1 y 120.");
        }
    }
}
