using System.Diagnostics;
using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Registro;
using Mediator;

namespace BlackHellHeim.Aplicacion.Comun.Comportamientos;

/// <summary>
/// Mide y registra cada ejecución (tipo de solicitud, resultado, duración y correlación).
/// No altera la respuesta y un fallo del registro no se propaga. Nunca registra la carga
/// útil de la solicitud ni mensajes o trazas de excepción.
/// </summary>
public sealed class RegistroBehavior<TMessage, TResponse>(
    IRegistroEjecucion registro,
    IContextoEjecucion contexto) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage mensaje, MessageHandlerDelegate<TMessage, TResponse> siguiente, CancellationToken cancelacion)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            var respuesta = await siguiente(mensaje, cancelacion);
            var error = (respuesta as IResultadoCasoDeUso)?.Error;
            Anotar(cronometro, error is null, error?.Codigo, null);
            return respuesta;
        }
        catch (Exception excepcion)
        {
            var codigo = excepcion switch
            {
                ExcepcionCasoDeUso uniforme => uniforme.Error.Codigo,
                OperationCanceledException => "operacion_cancelada",
                _ => "error_inesperado"
            };
            Anotar(cronometro, false, codigo, excepcion.GetType().Name);
            throw;
        }
    }

    private void Anotar(Stopwatch cronometro, bool exito, string? codigoError, string? tipoExcepcion)
    {
        try
        {
            registro.Registrar(new RegistroCasoDeUso(
                typeof(TMessage).Name, exito, cronometro.ElapsedMilliseconds, contexto.IdCorrelacion,
                DateTimeOffset.UtcNow, codigoError, tipoExcepcion));
        }
        catch
        {
            // El registro nunca debe alterar la respuesta ni ocultar la excepción original.
        }
    }
}
