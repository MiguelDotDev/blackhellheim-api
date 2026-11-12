using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Dominio.Webhooks;
using Mediator;

namespace BlackHellHeim.Aplicacion.Webhooks;

public sealed record EventoRecibido(Guid Id, string Proveedor, string IdExterno, string Tipo, string Cuerpo);

/// <summary>
/// Consumidor de eventos de un proveedor (HU-21, HU-32...). Puede reejecutarse tras un fallo,
/// así que sus efectos deben ser idempotentes.
/// </summary>
public interface IManejadorEventoProveedor
{
    string Proveedor { get; }

    IReadOnlyCollection<string> Tipos { get; }

    Task ManejarAsync(EventoRecibido evento, CancellationToken cancelacion);
}

public interface IRepositorioEventos
{
    Task<EventoWebhook?> ObtenerAsync(string proveedor, string idExterno, CancellationToken cancelacion);

    /// <summary>Inserta; devuelve false si (proveedor, id) ya existía.</summary>
    Task<bool> AgregarAsync(EventoWebhook evento, CancellationToken cancelacion);

    /// <summary>
    /// Toma el evento para procesarlo si está Fallido o si quedó Recibido desde antes de <paramref name="umbral"/>
    /// (proceso caído). Atómico: solo un llamador obtiene true.
    /// </summary>
    Task<bool> ReclamarAsync(Guid id, DateTimeOffset ahora, DateTimeOffset umbral, CancellationToken cancelacion);

    Task MarcarAsync(Guid id, EstadoEvento estado, DateTimeOffset ahora, string? error, CancellationToken cancelacion);
}

public sealed record RecibirWebhookComando(string Proveedor, string IdExterno, string Tipo, string Cuerpo)
    : ICommand<ResultadoCasoDeUso<ResultadoWebhook>>;

/// <param name="Repetido">El evento ya existía; no se reprocesó (o lo tomó otra petición).</param>
public sealed record ResultadoWebhook(EstadoEvento Estado, bool Repetido, bool CuerpoDistinto = false);

public sealed class ManejadorRecibirWebhook(
    IRepositorioEventos repositorio, IEnumerable<IManejadorEventoProveedor> consumidores, TimeProvider reloj,
    IContextoEjecucion contexto)
    : ICommandHandler<RecibirWebhookComando, ResultadoCasoDeUso<ResultadoWebhook>>
{
    /// <summary>Un evento Recibido más viejo que esto se considera huérfano y puede reintentarse.</summary>
    public static readonly TimeSpan EsperaHuerfano = TimeSpan.FromMinutes(5);

    public async ValueTask<ResultadoCasoDeUso<ResultadoWebhook>> Handle(RecibirWebhookComando c, CancellationToken cancelacion)
    {
        var ahora = reloj.GetUtcNow();
        var evento = EventoWebhook.Crear(c.Proveedor, c.IdExterno, c.Tipo, c.Cuerpo, ahora);
        if (!await repositorio.AgregarAsync(evento, cancelacion))
        {
            var previo = await repositorio.ObtenerAsync(c.Proveedor, c.IdExterno, cancelacion);
            if (previo is null) return Fallo(); // borrado entre ambas consultas: que el proveedor reintente
            var distinto = previo.Cuerpo != c.Cuerpo; // se conserva el primero; el endpoint lo advierte en el registro
            if (previo.Estado == EstadoEvento.Procesado
                || !await repositorio.ReclamarAsync(previo.Id, ahora, ahora - EsperaHuerfano, cancelacion))
                return ResultadoCasoDeUso<ResultadoWebhook>.Exito(new(previo.Estado, true, distinto));
            evento = previo;
        }

        var recibido = new EventoRecibido(evento.Id, c.Proveedor, c.IdExterno, c.Tipo, evento.Cuerpo);
        try
        {
            foreach (var m in consumidores.Where(m => m.Proveedor == c.Proveedor
                         && m.Tipos.Contains(c.Tipo, StringComparer.OrdinalIgnoreCase)))
                await m.ManejarAsync(recibido, cancelacion);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            await repositorio.MarcarAsync(evento.Id, EstadoEvento.Fallido, reloj.GetUtcNow(), e.GetType().Name, CancellationToken.None);
            return Fallo();
        }

        await repositorio.MarcarAsync(evento.Id, EstadoEvento.Procesado, reloj.GetUtcNow(), null, cancelacion);
        return ResultadoCasoDeUso<ResultadoWebhook>.Exito(new(EstadoEvento.Procesado, false));
    }

    private ResultadoCasoDeUso<ResultadoWebhook> Fallo() =>
        ResultadoCasoDeUso<ResultadoWebhook>.Fallo(ErrorUniforme.Inesperado(contexto.IdCorrelacion));
}
