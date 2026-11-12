using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using Mediator;

namespace BlackHellHeim.Aplicacion.Comun.Comportamientos;

/// <summary>
/// Ejecuta todos los validadores de la solicitud y acumula sus errores. Si hay alguno,
/// lanza <see cref="ExcepcionValidacion"/> y el manejador no se invoca.
/// </summary>
public sealed class ValidacionBehavior<TMessage, TResponse>(
    IEnumerable<IValidadorSolicitud<TMessage>> validadores,
    IContextoEjecucion contexto) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public ValueTask<TResponse> Handle(
        TMessage mensaje, MessageHandlerDelegate<TMessage, TResponse> siguiente, CancellationToken cancelacion)
    {
        var errores = validadores.SelectMany(v => v.Validar(mensaje)).ToList();
        if (errores.Count > 0)
        {
            throw new ExcepcionValidacion(errores, contexto.IdCorrelacion);
        }

        return siguiente(mensaje, cancelacion);
    }
}
