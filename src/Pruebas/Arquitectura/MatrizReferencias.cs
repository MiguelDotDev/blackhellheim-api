namespace BlackHellHeim.Pruebas.Arquitectura;

/// <summary>
/// Matriz de referencias permitidas según el plan (dependencias hacia adentro) y su
/// validador. Cada violación señala la dependencia infractora.
/// </summary>
public static class MatrizReferencias
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> ReferenciasPermitidas =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["BlackHellHeim.Dominio"] = [],
            ["BlackHellHeim.Aplicacion"] = ["BlackHellHeim.Dominio"],
            ["BlackHellHeim.Infraestructura"] = ["BlackHellHeim.Aplicacion", "BlackHellHeim.Dominio"],
            ["BlackHellHeim.Api"] = ["BlackHellHeim.Aplicacion", "BlackHellHeim.Dominio", "BlackHellHeim.Infraestructura"],
            ["BlackHellHeim.Pruebas"] =
            [
                "BlackHellHeim.Dominio",
                "BlackHellHeim.Aplicacion",
                "BlackHellHeim.Infraestructura",
                "BlackHellHeim.Api"
            ]
        };

    public static IReadOnlyList<string> Validar(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> grafo)
    {
        var violaciones = new List<string>();

        foreach (var (proyecto, referencias) in grafo)
        {
            if (!ReferenciasPermitidas.TryGetValue(proyecto, out var permitidas))
            {
                violaciones.Add($"proyecto desconocido: {proyecto}");
                continue;
            }

            foreach (var referencia in referencias)
            {
                if (!permitidas.Contains(referencia))
                {
                    violaciones.Add($"referencia no permitida: {proyecto} → {referencia}");
                }
            }
        }

        return violaciones;
    }
}
