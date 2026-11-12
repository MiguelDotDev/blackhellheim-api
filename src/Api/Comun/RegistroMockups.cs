using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Infraestructura.Mockups;
using Mediator;

namespace BlackHellHeim.Api.Comun;

public static class RegistroMockups
{
    /// <summary>Registra los mockups asíncronos: opciones <c>Mockups__*</c> validadas al arrancar, repositorio, consumidor del evento y polling.</summary>
    public static IServiceCollection AgregarMockups(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = configuracion.GetSection("Mockups").Get<OpcionesMockups>() ?? new OpcionesMockups();
        opciones.Validar();
        servicios.AddSingleton(opciones);
        servicios.AddScoped<IRepositorioMockups, RepositorioMockups>();
        servicios.AddScoped<IManejadorEventoProveedor, ManejadorMockupTaskFinished>();
        servicios.AddHostedService<ServicioPollingMockups>();
        return servicios;
    }
}

/// <summary>Respaldo del evento: consulta periódicamente las tareas pendientes y las finaliza (o las agota).</summary>
public sealed class ServicioPollingMockups(
    IServiceScopeFactory ambitos, OpcionesMockups opciones, TimeProvider reloj, ILogger<ServicioPollingMockups> registro)
    : BackgroundService
{
    private const int MaximoPorCiclo = 50;

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(opciones.IntervaloPollingSegundos), reloj);
        try
        {
            while (await temporizador.WaitForNextTickAsync(parada))
            {
                await CicloAsync(parada);
            }
        }
        catch (OperationCanceledException)
        {
            // apagado normal
        }
    }

    /// <summary>Un ciclo de polling; separado del temporizador para probarlo con reloj falso.</summary>
    public async Task CicloAsync(CancellationToken cancelacion)
    {
        try
        {
            await using var ambito = ambitos.CreateAsyncScope();
            var repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioMockups>();
            var emisor = ambito.ServiceProvider.GetRequiredService<ISender>();
            var limite = reloj.GetUtcNow() - TimeSpan.FromSeconds(opciones.EsperaMinimaSegundos);
            foreach (var id in await repositorio.ListarPendientesAntesDeAsync(limite, MaximoPorCiclo, cancelacion))
            {
                try
                {
                    await emisor.Send(new FinalizarTareaMockupComando(id), cancelacion);
                }
                catch (ExcepcionCasoDeUso e)
                {
                    // Transitorio (Printful, almacén): la tarea sigue pendiente y se reintenta en el siguiente ciclo.
                    registro.LogWarning("Mockup {Tarea} no finalizó: {Codigo}", id, e.Error.Codigo);
                }
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            registro.LogWarning(e, "Ciclo de polling de mockups fallido; se reintenta en el siguiente.");
        }
    }
}
