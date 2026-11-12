using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using Microsoft.AspNetCore.Diagnostics;

namespace BlackHellHeim.Api.Errores;

/// <summary>
/// Convierte cualquier excepción en el ProblemDetails uniforme: las excepciones de caso de uso
/// conservan su clasificación; las cancelaciones se vuelven 408; el resto, un 500 genérico
/// (<c>error_inesperado</c>) sin trazas, rutas internas ni mensajes de la excepción.
/// </summary>
public sealed class ManejadorExcepcionesGlobal(ILogger<ManejadorExcepcionesGlobal> registro) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        var ejecucion = contexto.RequestServices.GetRequiredService<IContextoEjecucion>();
        var error = excepcion switch
        {
            ExcepcionCasoDeUso uniforme => uniforme.Error,
            OperationCanceledException => ErrorUniforme.Cancelado(ejecucion.IdCorrelacion),
            _ => ErrorUniforme.Inesperado(ejecucion.IdCorrelacion)
        };

        if (error.Tipo == TipoError.Inesperado)
        {
            // Solo el tipo y la correlación: sin mensaje ni traza en el registro de la respuesta.
            registro.LogError("Error no controlado ({TipoExcepcion}) [{IdCorrelacion}]",
                (excepcion.InnerException ?? excepcion).GetType().Name, ejecucion.IdCorrelacion);
        }

        await TraductorProblemas.Traducir(error, contexto.Request.Path.Value ?? "/").ExecuteAsync(contexto);
        return true;
    }
}
