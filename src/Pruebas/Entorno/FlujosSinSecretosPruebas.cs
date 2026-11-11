using System.Text.RegularExpressions;
using Xunit;
using static BlackHellHeim.Pruebas.Entorno.FlujosCi;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Los flujos no filtran secretos y fijan sus acciones (historia 006, US2).</summary>
public class FlujosSinSecretosPruebas
{
    [Fact]
    public void ExistenLosTresFlujos()
    {
        var nombres = Archivos().Select(Path.GetFileName).ToList();
        foreach (var f in new[] { "integracion-continua.yml", "codeql.yml", "calidad-sonar.yml" })
        {
            Assert.Contains(f, nombres);
        }
    }

    [Fact]
    public void AccionesFijadasASha()
    {
        var usos = Archivos().SelectMany(a => Regex.Matches(File.ReadAllText(a), @"^\s*-?\s*uses:\s*(\S+)", RegexOptions.Multiline).Select(m => m.Groups[1].Value));
        foreach (var uso in usos)
        {
            Assert.Matches(@"@[0-9a-f]{40}$", uso);
        }
    }

    [Fact]
    public void PermisosYSecretos()
    {
        foreach (var archivo in Archivos())
        {
            var nombre = Path.GetFileName(archivo);
            var texto = File.ReadAllText(archivo);
            var flujo = Cargar(nombre);
            Assert.DoesNotContain("pull_request_target", texto);
            Assert.DoesNotContain("write-all", texto);
            Assert.True(flujo.ContainsKey("permissions"), $"{nombre} sin permissions de flujo.");
            foreach (Match m in Regex.Matches(texto, @"secrets\.(\w+)"))
            {
                Assert.Equal("SONAR_TOKEN", m.Groups[1].Value);
                Assert.Equal("calidad-sonar.yml", nombre);
            }

            Assert.DoesNotMatch(@"SONAR_TOKEN:\s*['""]?[0-9a-f]{40}", texto);
            foreach (var job in Hijo(flujo, "jobs")!.Values)
            {
                foreach (var cmd in Comandos(Mapa(job)!))
                {
                    Assert.DoesNotMatch(@"sonar\.token", cmd);
                    Assert.DoesNotMatch(@"\b(echo|printenv|set -x)\b[^\n]*(SONAR_TOKEN|secrets\.)|^\s*env\s*$", cmd);
                }
            }
        }
    }
}
