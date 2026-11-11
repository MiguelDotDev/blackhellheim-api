namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Localiza la raíz del repositorio y lee archivos relativos a ella.</summary>
public static class RaizRepositorio
{
    public static string Ruta { get; } = Buscar();

    public static string Combinar(params string[] partes) => Path.Combine([Ruta, .. partes]);

    public static string Leer(params string[] partes) => File.ReadAllText(Combinar(partes));

    private static string Buscar()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "BlackHellHeim.sln")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new InvalidOperationException("No se encontró BlackHellHeim.sln.");
    }
}
