using System.Text.RegularExpressions;
using BlackHellHeim.Aplicacion.Comun.Correlacion;

namespace BlackHellHeim.Api.Comun;

/// <summary>
/// Fija el identificador de correlación (cabecera <c>X-Correlation-ID</c> o uno generado) y lo
/// devuelve en la respuesta. Las excepciones las convierte <c>ManejadorExcepcionesGlobal</c>.
/// </summary>
public sealed partial class MiddlewareCorrelacionYErrores(RequestDelegate siguiente)
{
    public const string NombreCabecera = "X-Correlation-ID";

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex PatronValido();

    public async Task InvokeAsync(HttpContext contexto, IContextoEjecucion ejecucion)
    {
        var entrante = contexto.Request.Headers[NombreCabecera].ToString();
        ejecucion.IdCorrelacion = PatronValido().IsMatch(entrante) ? entrante : Guid.NewGuid().ToString("N");

        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[NombreCabecera] = ejecucion.IdCorrelacion;
            return Task.CompletedTask;
        });

        await siguiente(contexto);
    }
}
