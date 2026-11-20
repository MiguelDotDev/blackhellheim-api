using System.Collections.Concurrent;
using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Dominio.Webhooks;

namespace BlackHellHeim.Pruebas.Dobles;

/// <summary>Repositorio en memoria con las mismas garantías atómicas que el de SQL Server.</summary>
public sealed class RepositorioEventosEnMemoria : IRepositorioEventos
{
    private readonly object _candado = new();
    private readonly List<EventoWebhook> _eventos = [];

    public Task<EventoWebhook?> ObtenerAsync(string proveedor, string idExterno, CancellationToken c)
    {
        lock (_candado) return Task.FromResult(_eventos.FirstOrDefault(e => e.Proveedor == proveedor && e.IdExterno == idExterno));
    }

    public Task<bool> AgregarAsync(EventoWebhook evento, CancellationToken c)
    {
        lock (_candado)
        {
            if (_eventos.Any(e => e.Proveedor == evento.Proveedor && e.IdExterno == evento.IdExterno)) return Task.FromResult(false);
            _eventos.Add(evento);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ReclamarAsync(Guid id, DateTimeOffset ahora, DateTimeOffset umbral, CancellationToken c)
    {
        lock (_candado)
        {
            var e = _eventos.First(x => x.Id == id);
            if (!(e.Estado == EstadoEvento.Fallido || (e.Estado == EstadoEvento.Recibido && e.RecibidoUtc < umbral)))
                return Task.FromResult(false);
            Poner(e, EstadoEvento.Recibido, null, null, ahora);
            return Task.FromResult(true);
        }
    }

    public Task MarcarAsync(Guid id, EstadoEvento estado, DateTimeOffset ahora, string? error, CancellationToken c)
    {
        lock (_candado) Poner(_eventos.First(x => x.Id == id), estado, ahora, error, null);
        return Task.CompletedTask;
    }

    public EstadoEvento Estado(string idExterno)
    {
        lock (_candado) return _eventos.First(e => e.IdExterno == idExterno).Estado;
    }

    public int Total
    {
        get { lock (_candado) return _eventos.Count; }
    }

    // La entidad de dominio no expone mutadores (EF actualiza por ExecuteUpdate): el doble los fuerza por reflexión.
    private static void Poner(EventoWebhook e, EstadoEvento estado, DateTimeOffset? procesado, string? error, DateTimeOffset? recibido)
    {
        typeof(EventoWebhook).GetProperty(nameof(EventoWebhook.Estado))!.SetValue(e, estado);
        if (procesado is not null) typeof(EventoWebhook).GetProperty(nameof(EventoWebhook.ProcesadoUtc))!.SetValue(e, procesado);
        typeof(EventoWebhook).GetProperty(nameof(EventoWebhook.Error))!.SetValue(e, error);
        if (recibido is not null) typeof(EventoWebhook).GetProperty(nameof(EventoWebhook.RecibidoUtc))!.SetValue(e, recibido);
    }
}

public sealed class ConsumidorFalso(string proveedor, params string[] tipos) : IManejadorEventoProveedor
{
    private int _llamadas;

    public string Proveedor => proveedor;
    public IReadOnlyCollection<string> Tipos => tipos;
    public int Llamadas => _llamadas;
    public Func<Exception?> Fallo { get; set; } = () => null;
    public ConcurrentBag<string> Ids { get; } = [];

    public Task ManejarAsync(EventoRecibido evento, CancellationToken cancelacion)
    {
        Interlocked.Increment(ref _llamadas);
        Ids.Add(evento.IdExterno);
        return Fallo() is { } e ? Task.FromException(e) : Task.CompletedTask;
    }
}

public sealed class RelojFalso(DateTimeOffset inicio) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = inicio;

    public override DateTimeOffset GetUtcNow() => Ahora;
}
