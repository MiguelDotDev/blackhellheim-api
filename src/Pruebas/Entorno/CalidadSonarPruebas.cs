using Xunit;
using static BlackHellHeim.Pruebas.Entorno.FlujosCi;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Reglas de <c>calidad-sonar.yml</c> (historia 006, US4).</summary>
public class CalidadSonarPruebas
{
    private const string Archivo = "calidad-sonar.yml";
    private const string Token = "${{ secrets.SONAR_TOKEN }}";

    [Fact]
    public void DisparadoresYPermisos()
    {
        var flujo = Cargar(Archivo);
        IntegracionContinuaPruebas.VerificarDisparadores(flujo);
        Assert.Equal(["contents"], Hijo(flujo, "permissions")!.Keys);
    }

    [Fact]
    public void DeteccionDeToken()
    {
        var job = Job(Cargar(Archivo), "detectar-token");
        Assert.Equal("Detección de token de Sonar", Cadena(job, "name"));
        var paso = Pasos(job).Single(p => Cadena(p, "run") is not null);
        Assert.Equal(Token, Cadena(Hijo(paso, "env"), "SONAR_TOKEN"));
        var run = Cadena(paso, "run")!;
        Assert.Contains("GITHUB_OUTPUT", run);
        Assert.Contains("disponible=", run);
        Assert.DoesNotContain("echo \"$SONAR_TOKEN", run);
    }

    [Fact]
    public void AnalisisCondicionadoYOrdenado()
    {
        var job = Job(Cargar(Archivo), "analizar");
        Assert.Equal("SonarQube Cloud", Cadena(job, "name"));
        Assert.Equal("detectar-token", Cadena(job, "needs"));
        Assert.Equal("needs.detectar-token.outputs.disponible == 'true'", Cadena(job, "if"));
        Assert.Equal("10", Cadena(job, "timeout-minutes"));
        var co = Hijo(PasoUsa(job, "actions/checkout"), "with");
        Assert.Equal("0", Cadena(co, "fetch-depth"));
        Assert.Equal("false", Cadena(co, "persist-credentials"));
        var java = Hijo(PasoUsa(job, "actions/setup-java"), "with");
        Assert.Equal("temurin", Cadena(java, "distribution"));
        Assert.Equal("21", Cadena(java, "java-version"));

        var c = Comandos(job);
        var idx = new[] { "dotnet tool restore", "dotnet sonarscanner begin", "dotnet restore", "dotnet build", "dotnet test", "dotnet sonarscanner end" }
            .Select(p => c.FindIndex(x => x.StartsWith(p))).ToList();
        Assert.DoesNotContain(-1, idx);
        Assert.Equal(idx.Order(), idx);
        Assert.Contains("--locked-mode", c[idx[2]]);
        Assert.Contains("--configuration Release", c[idx[3]]);
        Assert.Contains("--no-build --configuration Release --collect:\"XPlat Code Coverage\"", c[idx[4]]);
    }

    [Fact]
    public void ScannerConPropiedadesYTokenPorEntorno()
    {
        var job = Job(Cargar(Archivo), "analizar");
        var begin = Pasos(job).Single(p => Cadena(p, "run")?.StartsWith("dotnet sonarscanner begin") == true);
        var fin = Pasos(job).Single(p => Cadena(p, "run")?.StartsWith("dotnet sonarscanner end") == true);
        Assert.Equal(Token, Cadena(Hijo(begin, "env"), "SONAR_TOKEN"));
        Assert.Equal(Token, Cadena(Hijo(fin, "env"), "SONAR_TOKEN"));
        var run = Cadena(begin, "run")!;
        foreach (var p in new[]
        {
            "sonar.host.url=https://sonarcloud.io", "/o:\"${{ vars.SONAR_ORGANIZATION }}\"", "/k:\"${{ vars.SONAR_PROJECT_KEY }}\"",
            "sonar.cs.cobertura.reportsPaths=**/TestResults/**/coverage.cobertura.xml", "sonar.qualitygate.wait=true",
            "sonar.qualitygate.timeout=300", "sonar.sourceEncoding=UTF-8",
        })
        {
            Assert.Contains(p, run);
        }

        Assert.DoesNotContain("secrets.", run);
        Assert.DoesNotContain("sonar.token", Texto(Archivo));
    }
}
