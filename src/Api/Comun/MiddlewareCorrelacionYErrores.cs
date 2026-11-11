using System.Text.RegularExpressions;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;

namespace BlackHellHeim.Api.Comun;

/// <summary>
/// Fija el identificador de correlación (cabecera <c>X-Correlation-ID</c> o uno generado),
/// lo devuelve en la respuesta y convierte las excepciones en ProblemDetails uniforme.
/// </summary>
public sealed partial class MiddlewareCorrelacionYErrores(RequestDelegate siguiente)
{
    public const string NombreCabecera = "X-Correlation-ID";

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex PatronValido();

    public async Task InvokeAsync(HttpContext contexto, IContextoEjecucion ejecucion, ILogger<MiddlewareCorrelacionYErrores> registro)
    {
        var entrante = contexto.Request.Headers[NombreCabecera].ToString();
        ejecucion.IdCorrelacion = PatronValido().IsMatch(entrante) ? entrante : Guid.NewGuid().ToString("N");

        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[NombreCabecera] = ejecucion.IdCorrelacion;
            return Task.CompletedTask;
        });

        try
        {
            await siguiente(contexto);
        }
        catch (Exception excepcion) when (!contexto.Response.HasStarted)
        {
            var error = excepcion switch
            {
                ExcepcionCasoDeUso uniforme => uniforme.Error,
                OperationCanceledException => ErrorUniforme.Cancelado(ejecucion.IdCorrelacion),
                _ => ErrorUniforme.Inesperado(ejecucion.IdCorrelacion)
            };

            if (error.Tipo == TipoError.Inesperado)
            {
                // Solo el tipo y la correlación: sin mensaje ni traza en el registro.
                registro.LogError("Error no controlado ({TipoExcepcion}) [{IdCorrelacion}]",
                    (excepcion.InnerException ?? excepcion).GetType().Name, ejecucion.IdCorrelacion);
            }

            contexto.Response.Clear();
            await TraductorProblemas.Traducir(error, contexto.Request.Path.Value ?? "/").ExecuteAsync(contexto);
        }
    }
}
