using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace BlackHellHeim.Infraestructura.Almacen;

public sealed class OpcionesAlmacen
{
    public string CadenaConexion { get; set; } = "";
    public string Contenedor { get; set; } = "arte";
}

/// <summary>Adaptador de Azure Blob (o Azurite en local). El cliente se crea al primer uso: sin cadena responde 503.</summary>
public sealed class AlmacenBlobAzure(OpcionesAlmacen opciones) : IAlmacenArchivos
{
    private BlobContainerClient? _contenedor;
    private readonly SemaphoreSlim _cerrojo = new(1, 1);

    public async Task<Uri> GuardarAsync(string nombre, string tipoContenido, Stream contenido, CancellationToken cancelacion) =>
        await Ejecutar(async () =>
        {
            var blob = (await ContenedorAsync(cancelacion)).GetBlobClient(nombre);
            await blob.UploadAsync(contenido, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = tipoContenido } }, cancelacion);
            return blob.Uri;
        });

    public async Task<bool> ExisteAsync(string nombre, CancellationToken cancelacion) =>
        await Ejecutar(async () => (await (await ContenedorAsync(cancelacion)).GetBlobClient(nombre).ExistsAsync(cancelacion)).Value);

    public async Task EliminarAsync(string nombre, CancellationToken cancelacion) =>
        await Ejecutar(async () =>
        {
            await (await ContenedorAsync(cancelacion)).GetBlobClient(nombre).DeleteIfExistsAsync(cancellationToken: cancelacion);
            return true;
        });

    private async Task<BlobContainerClient> ContenedorAsync(CancellationToken cancelacion)
    {
        if (_contenedor is not null) return _contenedor;
        await _cerrojo.WaitAsync(cancelacion);
        try
        {
            if (_contenedor is not null) return _contenedor;
            var cliente = new BlobContainerClient(opciones.CadenaConexion, opciones.Contenedor);
            try { await cliente.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancelacion); }
            catch (RequestFailedException) { /* en producción el contenedor lo crea una persona ([MANUAL]); si falta, la subida falla */ }
            return _contenedor = cliente;
        }
        finally { _cerrojo.Release(); }
    }

    private static async Task<T> Ejecutar<T>(Func<Task<T>> accion)
    {
        try { return await accion(); }
        catch (Exception e) when (e is RequestFailedException or ArgumentException or FormatException or HttpRequestException or AggregateException)
        {
            throw new AlmacenNoDisponibleException(e);
        }
    }
}

/// <summary>Decodifica con SkiaSharp (MIT) solo cuando la cabecera ya pasó los límites de tamaño.</summary>
public sealed class MuestreadorTransparenciaSkia : IMuestreadorTransparencia
{
    public MuestraTransparencia? Muestrear(Stream png)
    {
        using var origen = new SKManagedStream(png, false); // el flujo sigue siendo del llamador
        using var codec = SKCodec.Create(origen);
        if (codec is null) return null;
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var mapa = new SKBitmap(info);
        if (codec.GetPixels(info, mapa.GetPixels()) != SKCodecResult.Success) return null;

        var pixeles = mapa.GetPixelSpan(); // RGBA: el alfa es el cuarto byte
        long transparentes = 0;
        for (var i = 3; i < pixeles.Length; i += 4) if (pixeles[i] == 0) transparentes++;
        var w = info.Width;
        var h = info.Height;
        var esquinas = pixeles[3] == 0 && pixeles[((w - 1) * 4) + 3] == 0
            && pixeles[(((h - 1) * w) * 4) + 3] == 0 && pixeles[((((h - 1) * w) + w - 1) * 4) + 3] == 0;
        return new MuestraTransparencia(transparentes * 100.0 / ((long)info.Width * info.Height), esquinas);
    }
}

public sealed class RepositorioArte(ContextoBlackHellHeim contexto) : IRepositorioArte
{
    public Task<ArchivoArte?> ObtenerPorHashAsync(string hash, CancellationToken cancelacion) =>
        contexto.ArchivosArte.AsNoTracking().FirstOrDefaultAsync(a => a.HashSha256 == hash, cancelacion);

    public async Task<bool> AgregarAsync(ArchivoArte arte, CancellationToken cancelacion)
    {
        contexto.ArchivosArte.Add(arte);
        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            contexto.Entry(arte).State = EntityState.Detached;
            return false;
        }
    }
}
