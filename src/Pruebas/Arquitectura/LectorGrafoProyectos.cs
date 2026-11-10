using System.Xml.Linq;

namespace BlackHellHeim.Pruebas.Arquitectura;

/// <summary>
/// Lee el grafo real de referencias entre proyectos enumerando los <c>.csproj</c>
/// bajo <c>src/</c> y sus <c>ProjectReference</c>.
/// </summary>
public static class LectorGrafoProyectos
{
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> LeerGrafo()
    {
        var raizRepositorio = BuscarRaizRepositorio();
        var directorioFuente = Path.Combine(raizRepositorio, "src");
        var grafo = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);

        foreach (var rutaProyecto in Directory.EnumerateFiles(directorioFuente, "*.csproj", SearchOption.AllDirectories))
        {
            var documento = XDocument.Load(rutaProyecto);
            var nombreProyecto = Path.GetFileNameWithoutExtension(rutaProyecto);

            var referencias = documento
                .Descendants("ProjectReference")
                .Select(referencia => (string?)referencia.Attribute("Include"))
                .Where(incluida => !string.IsNullOrWhiteSpace(incluida))
                .Select(incluida => Path.GetFileNameWithoutExtension(
                    incluida!.Replace('\\', Path.DirectorySeparatorChar)))
                .ToArray();

            grafo[nombreProyecto] = referencias;
        }

        return grafo;
    }

    private static string BuscarRaizRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null &&
               !File.Exists(Path.Combine(directorio.FullName, "BlackHellHeim.sln")))
        {
            directorio = directorio.Parent;
        }

        if (directorio is null)
        {
            throw new InvalidOperationException(
                "No se encontró BlackHellHeim.sln hacia arriba desde " + AppContext.BaseDirectory);
        }

        return directorio.FullName;
    }
}
