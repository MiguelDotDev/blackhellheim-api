using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Garantiza que el repositorio público no contiene secretos (principio VIII).</summary>
public partial class ConfiguracionSinSecretosPruebas
{
    private const string Marcador = "__DEFINIR_CLAVE_LOCAL__";

    // "Password=" (no como parte de otra palabra) seguido de un valor real: se ignoran los
    // marcadores "__...__", comillas, comillas invertidas, variables y plantillas.
    [GeneratedRegex(@"(?<![A-Za-z0-9_])Password=(?!__)[^;'""`\s<{$)\]}]+", RegexOptions.IgnoreCase)]
    private static partial Regex ContrasenaEnCadena();

    private static string Raiz()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "BlackHellHeim.sln")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new InvalidOperationException("No se encontró BlackHellHeim.sln.");
    }

    private static IReadOnlyList<string> ArchivosVersionados()
    {
        var info = new ProcessStartInfo("git", "ls-files") { WorkingDirectory = Raiz(), RedirectStandardOutput = true };
        using var proceso = Process.Start(info)!;
        var salida = proceso.StandardOutput.ReadToEnd();
        proceso.WaitForExit();
        return salida.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void ElArchivoDeEjemploSoloContieneMarcadores()
    {
        var lineas = File.ReadAllLines(Path.Combine(Raiz(), ".env.example"))
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
            .ToArray();

        // Contraseña y cadena de conexión con marcador (HU3), puerto opcional del API (HU5) y
        // clave JWT con marcador y administrador inicial vacío (HU13).
        Assert.Equal(6, lineas.Length);
        Assert.StartsWith("BHH_SQL_SA_PASSWORD=", lineas[0]);
        Assert.Contains(Marcador, lineas[0]);
        Assert.StartsWith("ConnectionStrings__BlackHellHeim=", lineas[1]);
        Assert.Contains($"Password={Marcador}", lineas[1]);
        Assert.Equal("BHH_API_PUERTO='5068'", lineas[2]);
        Assert.Equal("BHH_JWT_CLAVE='__DEFINIR_CLAVE_JWT__'", lineas[3]);
        Assert.Equal("BHH_ADMIN_CORREO=''", lineas[4]);
        Assert.Equal("BHH_ADMIN_CONTRASENA=''", lineas[5]);
    }

    [Fact]
    public void ElArchivoEnvEstaIgnoradoYNoEstaVersionado()
    {
        var gitignore = File.ReadAllLines(Path.Combine(Raiz(), ".gitignore")).Select(l => l.Trim());

        Assert.Contains(".env", gitignore);
        Assert.DoesNotContain(".env", ArchivosVersionados());
    }

    [Fact]
    public void ElArchivoEnvTampocoEntraEnElContextoDeConstruccionDeDocker()
    {
        var dockerignore = File.ReadAllLines(Path.Combine(Raiz(), ".dockerignore")).Select(l => l.Trim());

        Assert.Contains(".env", dockerignore);
    }

    [Fact]
    public void NingunArchivoVersionadoContieneContrasenasReales()
    {
        var raiz = Raiz();
        var infractores = new List<string>();

        foreach (var relativa in ArchivosVersionados())
        {
            var ruta = Path.Combine(raiz, relativa);
            if (!File.Exists(ruta) || relativa.EndsWith("ConfiguracionSinSecretosPruebas.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string texto;
            try
            {
                texto = File.ReadAllText(ruta);
            }
            catch (IOException)
            {
                continue;
            }

            if (ContrasenaEnCadena().IsMatch(texto))
            {
                infractores.Add(relativa);
            }
        }

        Assert.True(infractores.Count == 0, "Posibles contraseñas reales en: " + string.Join(", ", infractores));
    }
}
