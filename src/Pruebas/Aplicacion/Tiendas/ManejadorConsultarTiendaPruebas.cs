using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;
using BlackHellHeim.Dominio.Tiendas;
using BlackHellHeim.Pruebas.Dobles;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Tiendas;

public class ManejadorConsultarTiendaPruebas
{
    private static readonly Tienda Bhh = new(CodigoTienda.Crear("BHH"), "BlackHellHeim", "MXN");

    private static (ManejadorConsultarTienda Manejador, RepositorioTiendasFalso Repositorio) Crear(string correlacion = "corr-1")
    {
        var repositorio = new RepositorioTiendasFalso(Bhh);
        var contexto = new ContextoEjecucionFalso { IdCorrelacion = correlacion };
        return (new ManejadorConsultarTienda(repositorio, contexto), repositorio);
    }

    [Fact]
    public async Task DevuelveElDtoDeLaTiendaExistente()
    {
        var (manejador, _) = Crear();

        var resultado = await manejador.Handle(new ConsultarTiendaQuery("BHH"), CancellationToken.None);

        Assert.True(resultado.EsExito);
        Assert.Equal(new TiendaDto("BHH", "BlackHellHeim", "MXN"), resultado.Valor);
        Assert.Null(resultado.Error);
    }

    [Fact]
    public async Task DevuelveFalloNoEncontradoSinLanzarExcepcion()
    {
        var (manejador, _) = Crear("corr-xyz");

        var resultado = await manejador.Handle(new ConsultarTiendaQuery("ZZZ"), CancellationToken.None);

        Assert.False(resultado.EsExito);
        Assert.Null(resultado.Valor);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
        Assert.Equal("recurso_no_encontrado", resultado.Error.Codigo);
        Assert.Equal("corr-xyz", resultado.Error.IdCorrelacion);
    }

    [Fact]
    public async Task ElRepositorioRecibeElCodigoNormalizado()
    {
        var (manejador, repositorio) = Crear();

        await manejador.Handle(new ConsultarTiendaQuery(" bhh "), CancellationToken.None);

        Assert.Equal(CodigoTienda.Crear("BHH"), repositorio.UltimoCodigo);
    }
}
