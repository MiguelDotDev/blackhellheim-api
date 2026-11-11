using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using Mediator;

namespace BlackHellHeim.Aplicacion.Comun.Comportamientos;

/// <summary>
/// Convierte los errores no controlados y las cancelaciones en <see cref="ExcepcionCasoDeUso"/>
/// con un <see cref="ErrorUniforme"/> genérico. Las excepciones ya uniformes pasan sin cambios.
/// </summary>
public sealed class ErroresBehavior<TMessage, TResponse>(IContextoEjecucion contexto)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage mensaje, MessageHandlerDelegate<TMessage, TResponse> siguiente, CancellationToken cancelacion)
    {
        try
        {
            return await siguiente(mensaje, cancelacion);
        }
        catch (ExcepcionCasoDeUso)
        {
            throw;
        }
        catch (OperationCanceledException excepcion)
        {
            throw new ExcepcionCasoDeUso(ErrorUniforme.Cancelado(contexto.IdCorrelacion), excepcion);
        }
        catch (Exception excepcion)
        {
            throw new ExcepcionCasoDeUso(ErrorUniforme.Inesperado(contexto.IdCorrelacion), excepcion);
        }
    }
}
