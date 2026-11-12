using BlackHellHeim.Pruebas.Comun;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Persistencia;

/// <summary>RF-003: el esquema solo cambia con migraciones; la API nunca crea ni migra la base al arrancar.</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class ArranqueSinMigrarPruebas(ContenedorSqlServer sql)
{
    private static readonly string[] Prohibidos = ["EnsureCreated", "EnsureCreatedAsync", "Migrate(", "MigrateAsync("];

    private static string Raiz()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "BlackHellHeim.sln")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new InvalidOperationException("No se encontró BlackHellHeim.sln.");
    }

    [Fact]
    public void ElCodigoDeLaApiNiDeInfraestructuraCreaOMigraLaBase()
    {
        var raiz = Raiz();
        var s = Path.DirectorySeparatorChar;
        var infractores = new[] { "Api", "Infraestructura" }
            .SelectMany(p => Directory.EnumerateFiles(Path.Combine(raiz, "src", p), "*.cs", SearchOption.AllDirectories))
            .Where(r => !r.Contains($"{s}obj{s}") && !r.Contains($"{s}bin{s}") && !r.Contains($"Persistencia{s}Migraciones{s}"))
            .Where(r => Prohibidos.Any(t => File.ReadAllText(r).Contains(t)))
            .Select(r => Path.GetRelativePath(raiz, r))
            .ToArray();

        Assert.True(infractores.Length == 0, "Creación o migración automática en: " + string.Join(", ", infractores));
    }

    [Fact]
    public async Task ArrancarLaApiNoCreaNiMigraUnaBaseVacia()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var fabrica = FabricaApiPruebas.Con(cadena);
        var cliente = fabrica.CreateClient();

        var salud = await cliente.GetAsync("/salud");
        await cliente.GetAsync("/salud/base");

        Assert.True(salud.IsSuccessStatusCode);
        Assert.Null(await sql.ConsultarEscalarAsync<string>(cadena, "SELECT OBJECT_ID('dbo.Tiendas')"));
        Assert.Null(await sql.ConsultarEscalarAsync<string>(cadena, "SELECT OBJECT_ID('dbo.__EFMigrationsHistory')"));
    }
}
