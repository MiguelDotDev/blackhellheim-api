using Xunit;

namespace BlackHellHeim.Pruebas.Arquitectura;

/// <summary>
/// Casos sintéticos que demuestran que el validador señala la dependencia infractora
/// sin tocar los archivos de proyecto reales.
/// </summary>
public class ViolacionesSinteticasPruebas
{
    [Theory]
    [MemberData(nameof(CasosDeViolacion))]
    public void ElValidadorReportaLaDependenciaInfractora(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> grafo,
        string violacionEsperada)
    {
        var violaciones = MatrizReferencias.Validar(grafo);

        Assert.Single(violaciones);
        Assert.Contains(violacionEsperada, violaciones);
    }

    public static TheoryData<IReadOnlyDictionary<string, IReadOnlyCollection<string>>, string> CasosDeViolacion =>
        new()
        {
            {
                Grafo(
                    ("BlackHellHeim.Dominio", ["BlackHellHeim.Aplicacion"]),
                    ("BlackHellHeim.Aplicacion", ["BlackHellHeim.Dominio"]),
                    ("BlackHellHeim.Infraestructura", ["BlackHellHeim.Aplicacion", "BlackHellHeim.Dominio"]),
                    ("BlackHellHeim.Api", ["BlackHellHeim.Aplicacion"]),
                    ("BlackHellHeim.Pruebas", ["BlackHellHeim.Dominio", "BlackHellHeim.Aplicacion", "BlackHellHeim.Infraestructura", "BlackHellHeim.Api"])),
                "referencia no permitida: BlackHellHeim.Dominio → BlackHellHeim.Aplicacion"
            },
            {
                Grafo(
                    ("BlackHellHeim.Dominio", []),
                    ("BlackHellHeim.Aplicacion", ["BlackHellHeim.Dominio", "BlackHellHeim.Infraestructura"]),
                    ("BlackHellHeim.Infraestructura", ["BlackHellHeim.Aplicacion", "BlackHellHeim.Dominio"]),
                    ("BlackHellHeim.Api", ["BlackHellHeim.Aplicacion"]),
                    ("BlackHellHeim.Pruebas", ["BlackHellHeim.Dominio", "BlackHellHeim.Aplicacion", "BlackHellHeim.Infraestructura", "BlackHellHeim.Api"])),
                "referencia no permitida: BlackHellHeim.Aplicacion → BlackHellHeim.Infraestructura"
            },
            {
                Grafo(
                    ("BlackHellHeim.Dominio", ["BlackHellHeim.Aplicacion"]),
                    ("BlackHellHeim.Aplicacion", ["BlackHellHeim.Dominio"]),
                    ("BlackHellHeim.Infraestructura", ["BlackHellHeim.Aplicacion", "BlackHellHeim.Dominio"]),
                    ("BlackHellHeim.Api", ["BlackHellHeim.Aplicacion"]),
                    ("BlackHellHeim.Pruebas", ["BlackHellHeim.Dominio", "BlackHellHeim.Aplicacion", "BlackHellHeim.Infraestructura", "BlackHellHeim.Api"])),
                "referencia no permitida: BlackHellHeim.Dominio → BlackHellHeim.Aplicacion"
            },
            {
                Grafo(
                    ("BlackHellHeim.Dominio", []),
                    ("BlackHellHeim.Aplicacion", ["BlackHellHeim.Dominio"]),
                    ("BlackHellHeim.Infraestructura", ["BlackHellHeim.Aplicacion", "BlackHellHeim.Dominio"]),
                    ("BlackHellHeim.Api", ["BlackHellHeim.Aplicacion"]),
                    ("BlackHellHeim.Pruebas", ["BlackHellHeim.Dominio", "BlackHellHeim.Aplicacion", "BlackHellHeim.Infraestructura", "BlackHellHeim.Api"]),
                    ("BlackHellHeim.Servicios", [])),
                "proyecto desconocido: BlackHellHeim.Servicios"
            }
        };

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Grafo(
        params (string Proyecto, string[] Referencias)[] proyectos)
    {
        return proyectos.ToDictionary(
            proyecto => proyecto.Proyecto,
            proyecto => (IReadOnlyCollection<string>)proyecto.Referencias,
            StringComparer.Ordinal);
    }
}
