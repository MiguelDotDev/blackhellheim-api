using System.Security.Cryptography;
using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Dominio.Webhooks;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BlackHellHeim.Infraestructura.Webhooks;

public static class VerificadorFirmaHmac
{
    /// <summary>HMAC-SHA256 del cuerpo crudo con el secreto ya decodificado, contra la firma hexadecimal, en tiempo constante.</summary>
    public static bool EsValida(byte[] secreto, ReadOnlySpan<byte> cuerpo, string? firma)
    {
        if (secreto.Length == 0 || string.IsNullOrWhiteSpace(firma)) return false;
        if (firma.Length != 64) return false;
        byte[] recibida;
        try
        {
            recibida = Convert.FromHexString(firma);
        }
        catch (FormatException)
        {
            return false;
        }

        var esperada = HMACSHA256.HashData(secreto, cuerpo);
        return CryptographicOperations.FixedTimeEquals(esperada, recibida);
    }
}

public sealed class RepositorioEventos(ContextoBlackHellHeim contexto) : IRepositorioEventos
{
    public Task<EventoWebhook?> ObtenerAsync(string proveedor, string idExterno, CancellationToken cancelacion) =>
        contexto.EventosWebhook.AsNoTracking().FirstOrDefaultAsync(e => e.Proveedor == proveedor && e.IdExterno == idExterno, cancelacion);

    public async Task<bool> AgregarAsync(EventoWebhook evento, CancellationToken cancelacion)
    {
        contexto.EventosWebhook.Add(evento);
        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            contexto.Entry(evento).State = EntityState.Detached;
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            contexto.Entry(evento).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> ReclamarAsync(Guid id, DateTimeOffset ahora, DateTimeOffset umbral, CancellationToken cancelacion) =>
        await contexto.EventosWebhook
            .Where(e => e.Id == id && (e.Estado == EstadoEvento.Fallido || (e.Estado == EstadoEvento.Recibido && e.RecibidoUtc < umbral)))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, EstadoEvento.Recibido).SetProperty(e => e.RecibidoUtc, ahora), cancelacion) == 1;

    public Task MarcarAsync(Guid id, EstadoEvento estado, DateTimeOffset ahora, string? error, CancellationToken cancelacion) =>
        contexto.EventosWebhook.Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, estado).SetProperty(e => e.ProcesadoUtc, ahora)
                .SetProperty(e => e.Error, error), cancelacion);
}
