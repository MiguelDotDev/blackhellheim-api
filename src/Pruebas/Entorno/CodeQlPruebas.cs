using Xunit;
using static BlackHellHeim.Pruebas.Entorno.FlujosCi;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Reglas de <c>codeql.yml</c> (historia 006, US3).</summary>
public class CodeQlPruebas
{
    private const string Archivo = "codeql.yml";

    [Fact]
    public void DisparadoresYPermisos()
    {
        var flujo = Cargar(Archivo);
        IntegracionContinuaPruebas.VerificarDisparadores(flujo);
        Assert.Equal(["contents"], Hijo(flujo, "permissions")!.Keys);
        var job = Job(flujo, "analizar");
        Assert.Equal("CodeQL (C#)", Cadena(job, "name"));
        Assert.Equal("10", Cadena(job, "timeout-minutes"));
        Assert.Equal(["security-events"], Hijo(job, "permissions")!.Keys);
        Assert.Equal("write", Cadena(Hijo(job, "permissions"), "security-events"));
    }

    [Fact]
    public void PasosDeAnalisis()
    {
        var job = Job(Cargar(Archivo), "analizar");
        var init = Hijo(PasoUsa(job, "github/codeql-action/init"), "with");
        Assert.Equal("csharp", Cadena(init, "languages"));
        Assert.Equal("security-extended", Cadena(init, "queries"));
        Assert.Equal("/language:csharp", Cadena(Hijo(PasoUsa(job, "github/codeql-action/analyze"), "with"), "category"));
        var pasos = Pasos(job);
        var i = pasos.FindIndex(p => Cadena(p, "uses")?.StartsWith("github/codeql-action/init@") == true);
        var a = pasos.FindIndex(p => Cadena(p, "uses")?.StartsWith("github/codeql-action/analyze@") == true);
        var restore = pasos.FindIndex(p => Cadena(p, "run") == "dotnet restore BlackHellHeim.sln --locked-mode");
        var build = pasos.FindIndex(p => Cadena(p, "run")?.StartsWith("dotnet build") == true);
        Assert.True(i < restore && restore < build && build < a);
        Assert.DoesNotContain(Comandos(job), c => c.Contains("dotnet test"));
    }
}
