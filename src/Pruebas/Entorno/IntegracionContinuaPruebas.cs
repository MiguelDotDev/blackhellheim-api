using Xunit;
using static BlackHellHeim.Pruebas.Entorno.FlujosCi;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Reglas de <c>integracion-continua.yml</c> (historia 006, US1).</summary>
public class IntegracionContinuaPruebas
{
    private const string Archivo = "integracion-continua.yml";

    /// <summary>Comprueba disparadores comunes a los tres flujos.</summary>
    public static void VerificarDisparadores(Dictionary<string, object?> flujo)
    {
        var on = Hijo(flujo, "on");
        Assert.NotNull(on);
        Assert.Equal(["pull_request", "push"], on!.Keys.Order());
        foreach (var evento in on.Keys)
        {
            Assert.Equal(Ramas.Order(), Lista(Hijo(on, evento)!["branches"]).Order());
            Assert.False(Hijo(on, evento)!.ContainsKey("paths") || Hijo(on, evento)!.ContainsKey("paths-ignore"));
        }
    }

    [Fact]
    public void DisparadoresYNombre()
    {
        var flujo = Cargar(Archivo);
        VerificarDisparadores(flujo);
        Assert.Equal("Integración continua", Cadena(flujo, "name"));
        Assert.True(Hijo(flujo, "concurrency")!["cancel-in-progress"]!.ToString() == "true");
    }

    [Fact]
    public void JobYPermisos()
    {
        var flujo = Cargar(Archivo);
        var job = Job(flujo, "compilar-y-probar");
        Assert.Equal("Compilación y pruebas", Cadena(job, "name"));
        Assert.Equal("ubuntu-latest", Cadena(job, "runs-on"));
        Assert.Equal("10", Cadena(job, "timeout-minutes"));
        var permisos = Hijo(flujo, "permissions")!;
        Assert.Equal(["contents"], permisos.Keys);
        Assert.Equal("read", Cadena(permisos, "contents"));
        Assert.False(job.ContainsKey("services"));
    }

    [Fact]
    public void PasosEnOrden()
    {
        var job = Job(Cargar(Archivo), "compilar-y-probar");
        Assert.Equal("false", Cadena(Hijo(PasoUsa(job, "actions/checkout"), "with"), "persist-credentials"));
        var con = Hijo(PasoUsa(job, "actions/setup-dotnet"), "with");
        Assert.Equal("global.json", Cadena(con, "global-json-file"));
        Assert.Equal("true", Cadena(con, "cache"));
        Assert.Equal("**/packages.lock.json", Cadena(con, "cache-dependency-path"));
        Assert.Equal(
        [
            "dotnet restore BlackHellHeim.sln --locked-mode",
            "dotnet build BlackHellHeim.sln --no-restore --configuration Release",
            "dotnet test BlackHellHeim.sln --no-build --configuration Release",
        ], Comandos(job));
    }

    [Fact]
    public void SinSecretosNiDespliegue()
    {
        var texto = Texto(Archivo);
        Assert.DoesNotContain("secrets.", texto);
        Assert.DoesNotContain("deploy", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("workflow_dispatch", texto);
        Assert.DoesNotContain("schedule", texto);
    }
}
