using System.Diagnostics;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Archivos versionados de texto (según <c>git ls-files</c>), para las pruebas de secretos.</summary>
public static class ArchivosVersionados
{
    public static IReadOnlyList<string> Rutas()
    {
        var info = new ProcessStartInfo("git", "ls-files") { WorkingDirectory = RaizRepositorio.Ruta, RedirectStandardOutput = true };
        using var proceso = Process.Start(info)!;
        var salida = proceso.StandardOutput.ReadToEnd();
        proceso.WaitForExit();
        return salida.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public static IEnumerable<(string Ruta, string Texto)> Textos()
    {
        foreach (var ruta in Rutas())
        {
            var completa = RaizRepositorio.Combinar(ruta);
            if (!File.Exists(completa))
            {
                continue;
            }

            string texto;
            try
            {
                texto = File.ReadAllText(completa);
            }
            catch (IOException)
            {
                continue;
            }

            yield return (ruta, texto);
        }
    }
}
