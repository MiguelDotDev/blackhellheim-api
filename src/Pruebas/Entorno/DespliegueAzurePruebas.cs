using System.Text.RegularExpressions;
using Xunit;
using static BlackHellHeim.Pruebas.Entorno.FlujosCi;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Reglas de los scripts <c>scripts/azure</c> y los flujos de despliegue (HU-25).</summary>
public class DespliegueAzurePruebas
{
    private static readonly string[] Scripts =
        ["variables.sh", "crear-recursos.sh", "destruir-recursos.sh", "configurar-oidc.sh", "configurar-secretos.sh", "revertir.sh"];

    private static string Script(string nombre) => File.ReadAllText(RaizRepositorio.Combinar("scripts", "azure", nombre));

    [Theory]
    [MemberData(nameof(NombresScripts))]
    public void ScriptsEstrictosYSinContrasenas(string nombre)
    {
        var texto = Script(nombre);
        Assert.Contains("set -euo pipefail", texto);
        Assert.DoesNotMatch(@"(?i)(password|clave|secret)\s*=\s*[""']?[A-Za-z0-9]{8,}[""']?\s*$", texto);
        Assert.DoesNotMatch(@"--admin-password\s+[""']?[A-Za-z]", texto);
    }

    public static IEnumerable<object[]> NombresScripts() => Scripts.Select(s => new object[] { s });

    [Fact]
    public void DestruirExigeConfirmacionAntesDeBorrar()
    {
        var t = Script("destruir-recursos.sh");
        Assert.True(t.IndexOf("read -r", StringComparison.Ordinal) < t.IndexOf("az group delete", StringComparison.Ordinal));
    }

    [Fact]
    public void CrearEsIdempotenteYUsaTamanosMinimos()
    {
        var t = Script("crear-recursos.sh");
        Assert.Contains("--revisions-mode multiple", t);
        Assert.Contains("--min-replicas 1", t);
        Assert.Contains("--use-free-limit true", t);
        Assert.Contains("ASPNETCORE_FORWARDEDHEADERS_ENABLED=true", t);
        Assert.True(Regex.Matches(t, @"\bshow -n").Count >= 4, "Cada recurso se consulta antes de crearse.");
        Assert.Contains("BHH_CPU:=0.25", Script("variables.sh"));
    }

    [Fact]
    public void OidcLimitadoAlEntornoYRolMinimo()
    {
        var t = Script("configurar-oidc.sh");
        Assert.Contains("environment:${BHH_ENTORNO_GITHUB}", t);
        Assert.Contains("--role Contributor", t);
        Assert.Contains("resourceGroups/$BHH_RG", t);
    }

    [Fact]
    public void SecretosPorSecretrefYFallaSiFaltan()
    {
        var t = Script("configurar-secretos.sh");
        Assert.Contains("fallar \"Falta la variable", t);
        foreach (var s in new[] { "ConnectionStrings__BlackHellHeim=secretref:", "Jwt__Clave=secretref:", "Administrador__Contrasena=secretref:" })
        {
            Assert.Contains(s, t);
        }
    }

    [Fact]
    public void DespliegueDisparadoresPermisosYOrden()
    {
        var flujo = Cargar("despliegue.yml");
        var on = Hijo(flujo, "on")!;
        Assert.Equal(["workflow_dispatch", "workflow_run"], on.Keys.Order());
        Assert.Equal(["main"], Lista(Hijo(on, "workflow_run")!["branches"]));
        Assert.Equal("false", Cadena(Hijo(flujo, "concurrency"), "cancel-in-progress"));
        Assert.Equal("read", Cadena(Hijo(flujo, "permissions"), "contents"));

        var publicar = Job(flujo, "publicar");
        var desplegar = Job(flujo, "desplegar");
        Assert.Equal("write", Cadena(Hijo(publicar, "permissions"), "packages"));
        Assert.Null(Hijo(publicar, "permissions")!.GetValueOrDefault("id-token"));
        Assert.Equal("write", Cadena(Hijo(desplegar, "permissions"), "id-token"));
        Assert.Equal("produccion", Cadena(desplegar, "environment"));
        Assert.Equal("publicar", Cadena(desplegar, "needs"));
        Assert.Contains("refs/heads/main", Cadena(publicar, "if"));

        var login = PasoUsa(desplegar, "azure/login");
        Assert.Equal(["client-id", "subscription-id", "tenant-id"], Hijo(login, "with")!.Keys.Order());

        var texto = Pasos(desplegar).Select(p => Cadena(p, "run") ?? "").ToList();
        var iMigrar = texto.FindIndex(r => r.Contains("job start"));
        var iRevision = texto.FindIndex(r => r.Contains("containerapp update"));
        var iSalud = texto.FindIndex(r => r.Contains("/salud") && r.Contains("revision label add"));
        var iTrafico = texto.FindIndex(r => r.Contains("--revision-weight \"$NUEVA=100\""));
        Assert.True(0 <= iMigrar && iMigrar < iRevision && iRevision < iSalud && iSalud < iTrafico);
    }

    [Fact]
    public void FlujosDeDespliegueSinSecretosDeAzure()
    {
        foreach (var f in new[] { "despliegue.yml", "revertir.yml" })
        {
            var t = FlujosCi.Texto(f);
            Assert.DoesNotContain("secrets.", t);
            Assert.DoesNotContain("AZURE_CREDENTIALS", t);
            Assert.DoesNotContain("pull_request", t);
        }

        var revertir = Job(Cargar("revertir.yml"), "revertir");
        Assert.Equal("write", Cadena(Hijo(revertir, "permissions"), "id-token"));
        Assert.Equal("produccion", Cadena(revertir, "environment"));
    }
}
