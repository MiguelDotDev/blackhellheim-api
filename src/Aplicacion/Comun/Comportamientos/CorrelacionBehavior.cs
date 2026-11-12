using BlackHellHeim.Aplicacion.Comun.Correlacion;
using Mediator;

namespace BlackHellHeim.Aplicacion.Comun.Comportamientos;

/// <summary>
/// Garantiza un identificador de correlación no vacío, también cuando el caso de uso se
/// despacha sin pasar por HTTP.
/// </summary>
public sealed class CorrelacionBehavior<TMessage, TResponse>(IContextoEjecucion contexto)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public ValueTask<TResponse> Handle(
        TMessage mensaje, MessageHandlerDelegate<TMessage, TResponse> siguiente, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(contexto.IdCorrelacion))
        {
            contexto.IdCorrelacion = Guid.NewGuid().ToString("N");
        }

        return siguiente(mensaje, cancelacion);
    }
}
