using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Infraestructura.Persistencia;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>
/// <c>GET /salud/base</c> según <c>specs/003-sql-server-efcore/contratos/openapi.yaml</c>.
/// <c>/salud</c> sigue siendo la comprobación de vida y no toca la base.
/// </summary>
public static class SaludBaseDatosEndpoints
{
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(3);

    public static IEndpointRouteBuilder MapearSaludBaseDatos(this IEndpointRouteBuilder rutas)
    {
        rutas.MapGet("/salud/base", async (
            IServiceScopeFactory fabricaAmbitos, IContextoEjecucion ejecucion, HttpContext http, CancellationToken cancelacion) =>
        {
            var disponible = await ComprobarAsync(fabricaAmbitos, cancelacion);

            return disponible
                ? Results.Ok(new { estado = "disponible" })
                : TraductorProblemas.Traducir(
                    ErrorUniforme.BaseNoDisponible(ejecucion.IdCorrelacion), http.Request.Path.Value ?? "/salud/base");
        });

        return rutas;
    }

    /// <summary>
    /// Comprueba la base con un tiempo máximo estricto. Con el servidor congelado, la cancelación de
    /// SqlClient puede tardar más que el límite, así que la espera se acota con un temporizador
    /// y la comprobación corre en un ámbito propio que no depende de la petición.
    /// </summary>
    private static async Task<bool> ComprobarAsync(IServiceScopeFactory fabricaAmbitos, CancellationToken cancelacion)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        limite.CancelAfter(TiempoMaximo);

        var comprobacion = Task.Run(async () =>
        {
            await using var ambito = fabricaAmbitos.CreateAsyncScope();
            var contexto = ambito.ServiceProvider.GetRequiredService<ContextoBlackHellHeim>();
            return await contexto.Database.CanConnectAsync(limite.Token);
        }, CancellationToken.None);

        try
        {
            var terminada = await Task.WhenAny(comprobacion, Task.Delay(TiempoMaximo, cancelacion));
            if (terminada != comprobacion)
            {
                _ = comprobacion.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return false;
            }

            return await comprobacion;
        }
        catch (Exception) when (!cancelacion.IsCancellationRequested)
        {
            // Fallo de conexión: nunca se exponen detalles internos.
            return false;
        }
    }
}
