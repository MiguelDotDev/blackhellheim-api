using Xunit;

namespace BlackHellHeim.Pruebas.Comun;

/// <summary>
/// Obliga a que toda prueba con servidor de pruebas use <c>FabricaApiPruebas</c>
/// (nunca <c>WebApplicationFactory&lt;Program&gt;</c> directo).
/// </summary>
public class UsoDeFabricaPruebas
{
    [Fact]
    public void NingunaPruebaUsaWebApplicationFactoryDirectamente()
    {
        var raiz = BuscarRaiz();
        var directorio = Path.Combine(raiz, "src", "Pruebas");

        var infractores = Directory.EnumerateFiles(directorio, "*.cs", SearchOption.AllDirectories)
            .Where(ruta => !ruta.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !ruta.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(ruta => Path.GetFileName(ruta) is not ("FabricaApiPruebas.cs" or "UsoDeFabricaPruebas.cs"))
            .Where(ruta => File.ReadAllText(ruta).Contains("WebApplicationFactory<Program>"))
            .Select(ruta => Path.GetRelativePath(raiz, ruta))
            .ToArray();

        Assert.True(infractores.Length == 0,
            "Usa FabricaApiPruebas en lugar de WebApplicationFactory<Program>: " + string.Join(", ", infractores));
    }

    private static string BuscarRaiz()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "BlackHellHeim.sln")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new InvalidOperationException("No se encontró BlackHellHeim.sln.");
    }
}
