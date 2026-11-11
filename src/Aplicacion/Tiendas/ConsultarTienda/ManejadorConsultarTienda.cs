using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Dominio.Tiendas;
using Mediator;

namespace BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;

public sealed class ManejadorConsultarTienda(IRepositorioTiendas repositorio, IContextoEjecucion contexto)
    : IQueryHandler<ConsultarTiendaQuery, ResultadoCasoDeUso<TiendaDto>>
{
    public async ValueTask<ResultadoCasoDeUso<TiendaDto>> Handle(
        ConsultarTiendaQuery consulta, CancellationToken cancelacion)
    {
        var codigo = CodigoTienda.Crear(consulta.Codigo);
        var tienda = await repositorio.ObtenerPorCodigoAsync(codigo, cancelacion);

        if (tienda is null)
        {
            return ResultadoCasoDeUso<TiendaDto>.Fallo(
                ErrorUniforme.NoEncontrado("No existe una tienda con el código indicado.", contexto.IdCorrelacion));
        }

        return ResultadoCasoDeUso<TiendaDto>.Exito(
            new TiendaDto(tienda.Codigo.Valor, tienda.Nombre, tienda.Moneda));
    }
}
