namespace BlackHellHeim.Dominio.Webhooks;

public enum EstadoEvento { Recibido, Procesado, Fallido }

/// <summary>Evento entrante de un proveedor. La unicidad (Proveedor, IdExterno) garantiza la idempotencia.</summary>
public sealed class EventoWebhook
{
    private EventoWebhook()
    {
    }

    public Guid Id { get; private set; }
    public string Proveedor { get; private set; } = "";
    public string IdExterno { get; private set; } = "";
    public string Tipo { get; private set; } = "";
    public EstadoEvento Estado { get; private set; }
    public string Cuerpo { get; private set; } = "";
    public DateTimeOffset RecibidoUtc { get; private set; }
    public DateTimeOffset? ProcesadoUtc { get; private set; }
    public string? Error { get; private set; }

    public static EventoWebhook Crear(string proveedor, string idExterno, string tipo, string cuerpo, DateTimeOffset ahora) =>
        new()
        {
            Id = Guid.NewGuid(), Proveedor = proveedor, IdExterno = idExterno, Tipo = tipo,
            Estado = EstadoEvento.Recibido, Cuerpo = cuerpo, RecibidoUtc = ahora
        };
}
