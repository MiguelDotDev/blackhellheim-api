using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Infraestructura.Almacen;
using SkiaSharp;

namespace BlackHellHeim.Pruebas.Arte;

internal static class GeneradorPng
{
    /// <summary>PNG RGBA de <paramref name="ancho"/>×<paramref name="alto"/>; transparente salvo un rectángulo opaco central.</summary>
    public static byte[] Crear(int ancho, int alto, bool transparente = true, double fraccionOpaca = 0.5)
    {
        using var mapa = new SKBitmap(new SKImageInfo(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using var lienzo = new SKCanvas(mapa);
        lienzo.Clear(transparente ? SKColors.Transparent : SKColors.Red);
        if (transparente)
        {
            using var pintura = new SKPaint { Color = SKColors.Red };
            var w = (float)(ancho * Math.Sqrt(fraccionOpaca));
            var h = (float)(alto * Math.Sqrt(fraccionOpaca));
            lienzo.DrawRect((ancho - w) / 2, (alto - h) / 2, w, h, pintura);
        }
        using var imagen = SKImage.FromBitmap(mapa);
        return imagen.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    /// <summary>Inserta un chunk acTL (APNG) justo después de IHDR; el CRC no importa porque se rechaza antes de decodificar.</summary>
    public static byte[] ConAnimacion(byte[] png)
    {
        byte[] acTl = [0, 0, 0, 8, (byte)'a', (byte)'c', (byte)'T', (byte)'L', 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0];
        var corte = 8 + 12 + 13;
        return [.. png[..corte], .. acTl, .. png[corte..]];
    }
}

internal static class OpcionesPequenas
{
    public static OpcionesArte Crear() => new() { MinAncho = 40, MinAlto = 50, MaxBytes = 1_000_000, MaxMegapixeles = 1 };
}

internal sealed class AlmacenEnMemoria : IAlmacenArchivos
{
    public Dictionary<string, byte[]> Blobs { get; } = [];
    public bool Falla { get; set; }

    public async Task<Uri> GuardarAsync(string nombre, string tipoContenido, Stream contenido, CancellationToken cancelacion)
    {
        if (Falla) throw new AlmacenNoDisponibleException();
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, cancelacion);
        Blobs[nombre] = memoria.ToArray();
        return new Uri($"https://almacen.test/arte/{nombre}");
    }

    public Task<bool> ExisteAsync(string nombre, CancellationToken cancelacion) => Task.FromResult(Blobs.ContainsKey(nombre));

    public Task EliminarAsync(string nombre, CancellationToken cancelacion)
    {
        Blobs.Remove(nombre);
        return Task.CompletedTask;
    }
}

internal sealed class RepositorioArteEnMemoria : IRepositorioArte
{
    public List<ArchivoArte> Registros { get; } = [];
    public bool FallaAlRegistrar { get; set; }

    public Task<ArchivoArte?> ObtenerPorHashAsync(string hash, CancellationToken cancelacion) =>
        Task.FromResult(Registros.FirstOrDefault(a => a.HashSha256 == hash));

    public Task<bool> AgregarAsync(ArchivoArte arte, CancellationToken cancelacion)
    {
        if (FallaAlRegistrar) throw new InvalidOperationException("falla simulada");
        Registros.Add(arte);
        return Task.FromResult(true);
    }
}

internal sealed class ContextoArteFalso : IContextoEjecucion
{
    public string IdCorrelacion { get; set; } = "corr";
}

/// <summary>Flujo de N bytes (ceros) que no ocupa memoria; cuenta lo leído.</summary>
internal sealed class FlujoGenerado(long largo) : Stream
{
    public long Leidos { get; private set; }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => largo;
    public override long Position { get => Leidos; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = (int)Math.Min(count, largo - Leidos);
        Array.Clear(buffer, offset, n);
        Leidos += n;
        return n;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> destino, CancellationToken cancelacion = default)
    {
        var n = (int)Math.Min(destino.Length, largo - Leidos);
        destino.Span[..n].Clear();
        Leidos += n;
        return ValueTask.FromResult(n);
    }
}
