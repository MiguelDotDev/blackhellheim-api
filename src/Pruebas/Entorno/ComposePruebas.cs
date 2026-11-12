using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BlackHellHeim.Pruebas.Contratos;
using Xunit;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Pruebas estáticas de <c>compose.yaml</c> (historia 005).</summary>
public partial class ComposePruebas
{
    private static JsonObject Compose() => LectorContratos.ConvertirYaml(RaizRepositorio.Leer("compose.yaml"));

    private static JsonNode Servicio(string nombre) =>
        Compose()["services"]?[nombre] ?? throw new Xunit.Sdk.XunitException($"compose.yaml no define el servicio '{nombre}'.");

    private static TimeSpan Duracion(string? texto) =>
        texto is not null && texto.EndsWith('s') ? TimeSpan.FromSeconds(double.Parse(texto[..^1])) : TimeSpan.Zero;

    [Fact]
    public void DefineLosTresServiciosYLaApiDependePorSalud()
    {
        var api = Servicio("api");
        Servicio("sqlserver");
        Servicio("migraciones");

        Assert.Equal("service_healthy", api["depends_on"]!["sqlserver"]!["condition"]!.ToString());
        Assert.Equal("service_completed_successfully", api["depends_on"]!["migraciones"]!["condition"]!.ToString());
        Assert.Equal("service_healthy", Servicio("migraciones")["depends_on"]!["sqlserver"]!["condition"]!.ToString());
    }

    [Fact]
    public void LaBaseYLaApiTienenHealthcheckYLasMigracionesNoSeReinician()
    {
        Assert.NotNull(Servicio("sqlserver")["healthcheck"]?["test"]);
        Assert.NotNull(Servicio("api")["healthcheck"]?["test"]);
        Assert.Equal("no", Servicio("migraciones")["restart"]!.ToString());
    }

    [Fact]
    public void ElHealthcheckDeLaApiUsaLaRutaDeListoYAcotaLaEspera()
    {
        var comprobacion = Servicio("api")["healthcheck"]!;

        Assert.Contains("/salud/base", comprobacion["test"]!.ToJsonString());
        var espera = Duracion(comprobacion["start_period"]?.ToString())
                     + (Duracion(comprobacion["interval"]?.ToString()) + Duracion(comprobacion["timeout"]?.ToString()))
                     * int.Parse(comprobacion["retries"]!.ToString());
        Assert.InRange(espera, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void NoHayContrasenasLiteralesYLasVariablesObligatoriasFallanNombrandoLaVariable()
    {
        var texto = RaizRepositorio.Leer("compose.yaml");

        Assert.Contains("${BHH_SQL_SA_PASSWORD:?", texto);
        Assert.DoesNotMatch(ContrasenaLiteral(), texto);
        Assert.Equal("${BHH_SQL_SA_PASSWORD:?Defina BHH_SQL_SA_PASSWORD en .env}",
            Servicio("sqlserver")["environment"]!["MSSQL_SA_PASSWORD"]!.ToString());
    }

    [Fact]
    public void NingunServicioFijaUnEntornoDeDesarrolloOPruebas()
    {
        foreach (var (nombre, servicio) in Compose()["services"]!.AsObject())
        {
            var entorno = servicio?["environment"]?.AsObject();
            foreach (var variable in new[] { "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT" })
            {
                var valor = entorno?[variable]?.ToString();
                Assert.True(valor is null or "Production",
                    $"El servicio '{nombre}' fija {variable}={valor}; en el contenedor debe ser Production.");
            }
        }
    }

    [Fact]
    public void LaApiSeConstruyeEnLaEtapaFinalYLasMigracionesEnLaSuya()
    {
        Assert.Equal("final", Servicio("api")["build"]!["target"]!.ToString());
        Assert.Equal("migraciones", Servicio("migraciones")["build"]!["target"]!.ToString());
    }

    // "Password=" seguido de un valor que no sea una variable ${...}.
    [GeneratedRegex(@"(?<![A-Za-z0-9_])Passwor[d]=(?!\$\{)[^;'""\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex ContrasenaLiteral();
}
