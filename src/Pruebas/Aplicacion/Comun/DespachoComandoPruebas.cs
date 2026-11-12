using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Registro;
using BlackHellHeim.Aplicacion.Ejemplos;
using BlackHellHeim.Aplicacion.Tiendas;
using BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;
using BlackHellHeim.Dominio.Tiendas;
using BlackHellHeim.Pruebas.Dobles;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Comun;

/// <summary>
/// RF-001: comandos y consultas se despachan por el mismo mecanismo (<see cref="ISender"/>).
/// </summary>
public class DespachoComandoPruebas
{
    private static ServiceProvider ConstruirProveedor(RegistroEjecucionEnMemoria? registro = null)
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarCasosDeUso();
        servicios.AddScoped<IRegistroEjecucion>(_ => registro ?? new RegistroEjecucionEnMemoria());
        servicios.AddScoped<IRepositorioTiendas>(_ =>
            new RepositorioTiendasFalso(new Tienda(CodigoTienda.Crear("BHH"), "BlackHellHeim", "MXN")));
        return servicios.BuildServiceProvider();
    }

    [Fact]
    public async Task UnComandoSeDespachaPorElMediador()
    {
        await using var proveedor = ConstruirProveedor();
        using var ambito = proveedor.CreateScope();
        var emisor = ambito.ServiceProvider.GetRequiredService<ISender>();

        var resultado = await emisor.Send(new EcoComando("hola"));

        Assert.True(resultado.EsExito);
        Assert.Equal("hola", resultado.Valor);
    }

    [Fact]
    public async Task UnaConsultaSeDespachaPorElMismoMediador()
    {
        await using var proveedor = ConstruirProveedor();
        using var ambito = proveedor.CreateScope();
        ambito.ServiceProvider.GetRequiredService<IContextoEjecucion>().IdCorrelacion = "c";
        var emisor = ambito.ServiceProvider.GetRequiredService<ISender>();

        var resultado = await emisor.Send(new ConsultarTiendaQuery("BHH"));

        Assert.True(resultado.EsExito);
        Assert.Equal("BlackHellHeim", resultado.Valor!.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UnComandoConTextoVacioEsRechazadoPorLaValidacion(string? texto)
    {
        await using var proveedor = ConstruirProveedor();
        using var ambito = proveedor.CreateScope();
        var emisor = ambito.ServiceProvider.GetRequiredService<ISender>();

        var excepcion = await Assert.ThrowsAsync<ExcepcionValidacion>(async () =>
            await emisor.Send(new EcoComando(texto!)));

        Assert.Equal("texto", Assert.Single(excepcion.Errores).Campo);
    }
}
