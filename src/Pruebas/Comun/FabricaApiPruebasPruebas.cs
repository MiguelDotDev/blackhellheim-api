using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BlackHellHeim.Pruebas.Comun;

public class FabricaApiPruebasPruebas
{
    private const string Variable = "ConnectionStrings__BlackHellHeim";

    [Fact]
    public async Task LaApiArrancaSinLaVariableDeEntornoYSaludRespondeOk()
    {
        // La fábrica aporta su propia cadena: no depende de que la variable exista en el entorno.
        var original = Environment.GetEnvironmentVariable(Variable);
        Environment.SetEnvironmentVariable(Variable, null);
        try
        {
            await using var fabrica = new FabricaApiPruebas();
            var respuesta = await fabrica.CreateClient().GetAsync("/salud");

            Assert.True(respuesta.IsSuccessStatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, original);
        }
    }

    [Fact]
    public async Task ElEntornoEsPruebas()
    {
        await using var fabrica = new FabricaApiPruebas();
        _ = fabrica.CreateClient();

        var entorno = fabrica.Services.GetRequiredService<IHostEnvironment>();

        Assert.Equal("Pruebas", entorno.EnvironmentName);
    }

    [Fact]
    public async Task UnaCadenaPasadaAlConstructorSeUsaEnLugarDeLaPredeterminada()
    {
        const string cadena = "Server=127.0.0.1,2;Database=Otra;Integrated Security=true;Encrypt=False";
        await using var fabrica = FabricaApiPruebas.Con(cadena);
        _ = fabrica.CreateClient();

        var configuracion = fabrica.Services.GetRequiredService<IConfiguration>();

        Assert.Equal(cadena, configuracion["ConnectionStrings:BlackHellHeim"]);
    }

    [Fact]
    public void LaCadenaPredeterminadaNoContieneCredenciales()
    {
        var cadena = FabricaApiPruebas.CadenaPredeterminada;

        Assert.DoesNotContain("Password=", cadena, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User Id=", cadena, StringComparison.OrdinalIgnoreCase);
    }
}
