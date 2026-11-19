using System.Buffers.Binary;
using System.Security.Cryptography;
using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Dominio.Disenos;
using Mediator;

namespace BlackHellHeim.Aplicacion.Arte;

/// <summary>Límites configurables (<c>Arte__*</c>), validados al arrancar.</summary>
public sealed class OpcionesArte
{
    public int MinAncho { get; set; } = 1800;
    public int MinAlto { get; set; } = 2400;
    public long MaxBytes { get; set; } = 200L * 1024 * 1024;
    public int MaxMegapixeles { get; set; } = 100;
    public double PorcentajeMinimoTransparente { get; set; } = 5;

    public void Validar()
    {
        if (MinAncho < 1 || MinAlto < 1) throw new InvalidOperationException("Arte: MinAncho y MinAlto deben ser positivos.");
        if (MaxBytes < 1) throw new InvalidOperationException("Arte: MaxBytes debe ser positivo.");
        if (MaxMegapixeles < 1 || (long)MinAncho * MinAlto > MaxMegapixeles * 1_000_000L)
            throw new InvalidOperationException("Arte: MaxMegapixeles debe ser positivo y cubrir la resolución mínima.");
        if (PorcentajeMinimoTransparente is < 0 or > 100)
            throw new InvalidOperationException("Arte: PorcentajeMinimoTransparente debe estar entre 0 y 100.");
    }
}

/// <summary>Falla del almacenamiento externo; se traduce a 503.</summary>
public sealed class AlmacenNoDisponibleException(Exception? interna = null)
    : Exception("El almacenamiento de archivos no está disponible.", interna);

/// <summary>Puerto del almacén de archivos (Azure Blob en producción, Azurite en local).</summary>
public interface IAlmacenArchivos
{
    /// <summary>Guarda el flujo con el nombre dado y devuelve su URL pública.</summary>
    Task<Uri> GuardarAsync(string nombre, string tipoContenido, Stream contenido, CancellationToken cancelacion);

    Task<bool> ExisteAsync(string nombre, CancellationToken cancelacion);

    Task EliminarAsync(string nombre, CancellationToken cancelacion);
}

/// <summary>Mide la transparencia decodificando la imagen (la implementa la infraestructura).</summary>
public interface IMuestreadorTransparencia
{
    /// <summary>Devuelve null si la imagen no se puede decodificar.</summary>
    MuestraTransparencia? Muestrear(Stream png);
}

public sealed record MuestraTransparencia(double PorcentajeTransparente, bool EsquinasTransparentes);

public interface IRepositorioArte
{
    Task<ArchivoArte?> ObtenerPorHashAsync(string hash, CancellationToken cancelacion);

    /// <summary>Registra el arte; false si otro proceso ya registró el mismo hash.</summary>
    Task<bool> AgregarAsync(ArchivoArte arte, CancellationToken cancelacion);
}

public sealed record InfoPng(int Ancho, int Alto, bool TieneAlfa);

/// <summary>Valida un PNG: lee a mano la firma, IHDR y los chunks previos a IDAT, y muestrea píxeles solo al final.</summary>
public sealed class ValidadorArte(OpcionesArte opciones, IMuestreadorTransparencia muestreador)
{
    public const string Png = "arte-no-png";
    public const string SinTransparencia = "arte-sin-transparencia";
    public const string ResolucionInsuficiente = "arte-resolucion-insuficiente";
    public const string DemasiadoGrande = "arte-demasiado-grande";
    public const string Ilegible = "arte-ilegible";

    private static readonly byte[] Firma = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Devuelve (null, info) si es válido o (código, mensaje) con el primer rechazo.</summary>
    public (ErrorArte? Error, InfoPng? Info) Validar(Stream flujo)
    {
        flujo.Position = 0;
        if (flujo.Length == 0) return (new(Ilegible, "El archivo está vacío."), null);
        var firma = new byte[8];
        if (flujo.Read(firma) < 8 || !firma.AsSpan().SequenceEqual(Firma))
            return (new(Png, "El archivo no es un PNG."), null);

        var cabecera = LeerChunk(flujo, out var tipo);
        if (tipo != "IHDR" || cabecera.Length != 13) return (new(Ilegible, "El PNG está dañado (sin cabecera)."), null);
        var ancho = BinaryPrimitives.ReadInt32BigEndian(cabecera);
        var alto = BinaryPrimitives.ReadInt32BigEndian(cabecera.AsSpan(4));
        var tipoColor = cabecera[9];
        if (ancho < 1 || alto < 1) return (new(Ilegible, "El PNG está dañado (dimensiones inválidas)."), null);

        var alfa = tipoColor is 4 or 6;
        while (true)
        {
            var datos = LeerChunk(flujo, out tipo);
            if (tipo is null) return (new(Ilegible, "El PNG está dañado o truncado."), null);
            if (tipo == "acTL") return (new(Png, "Los PNG animados (APNG) no se aceptan."), null);
            if (tipo == "tRNS") alfa = true;
            if (tipo is "IDAT" or "IEND") break;
        }

        if ((long)ancho * alto > opciones.MaxMegapixeles * 1_000_000L)
            return (new(DemasiadoGrande, $"La imagen supera {opciones.MaxMegapixeles} megapíxeles."), null);
        if (ancho < opciones.MinAncho || alto < opciones.MinAlto)
            return (new(ResolucionInsuficiente, $"La resolución mínima es {opciones.MinAncho}×{opciones.MinAlto} px."), null);
        if (!alfa) return (new(SinTransparencia, "El PNG no tiene canal alfa."), null);

        flujo.Position = 0;
        var muestra = muestreador.Muestrear(flujo);
        if (muestra is null) return (new(Ilegible, "No se pudo decodificar el PNG."), null);
        if (!muestra.EsquinasTransparentes || muestra.PorcentajeTransparente < opciones.PorcentajeMinimoTransparente)
            return (new(SinTransparencia, $"El fondo debe ser transparente (esquinas y al menos {opciones.PorcentajeMinimoTransparente}% de píxeles)."), null);
        return (null, new InfoPng(ancho, alto, true));
    }

    // Devuelve los datos del chunk; tipo = null si el flujo termina antes de tiempo. Salta los datos sin cargarlos si es IDAT.
    private static byte[] LeerChunk(Stream flujo, out string? tipo)
    {
        Span<byte> enc = stackalloc byte[8];
        tipo = null;
        if (flujo.Read(enc) < 8) return [];
        var largo = BinaryPrimitives.ReadUInt32BigEndian(enc);
        var nombre = System.Text.Encoding.ASCII.GetString(enc[4..]);
        if (nombre is "IDAT" or "IEND") { tipo = nombre; return []; }
        if (largo > 1_000_000 || flujo.Position + largo + 4 > flujo.Length) return [];
        var datos = new byte[largo];
        flujo.ReadExactly(datos);
        flujo.Seek(4, SeekOrigin.Current); // CRC
        tipo = nombre;
        return datos;
    }
}

public sealed record ErrorArte(string Codigo, string Mensaje);

public sealed record ArteDto(Guid Id, string Url, int Ancho, int Alto, long TamanoBytes, bool Reutilizado);

public sealed record SubirArteComando(Stream Contenido, string? NombreArchivo, string? TipoContenido)
    : ICommand<ResultadoCasoDeUso<ArteDto>>;

public sealed class ManejadorSubirArte(
    OpcionesArte opciones, ValidadorArte validador, IAlmacenArchivos almacen, IRepositorioArte repositorio,
    TimeProvider reloj, IContextoEjecucion contexto)
    : ICommandHandler<SubirArteComando, ResultadoCasoDeUso<ArteDto>>
{
    public async ValueTask<ResultadoCasoDeUso<ArteDto>> Handle(SubirArteComando c, CancellationToken cancelacion)
    {
        var id = contexto.IdCorrelacion;
        var nombre = c.NombreArchivo ?? "";
        if (!nombre.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(c.TipoContenido?.Split(';')[0].Trim(), "image/png", StringComparison.OrdinalIgnoreCase))
            return Fallo(TipoError.ArchivoNoSoportado, ValidadorArte.Png, "Solo se aceptan archivos PNG (.png, image/png).");

        var temporal = Path.Combine(Path.GetTempPath(), $"bhh-arte-{Guid.NewGuid():N}.tmp");
        try
        {
            await using var archivo = new FileStream(temporal, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                81920, FileOptions.Asynchronous);
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            long total = 0;
            int leidos;
            while ((leidos = await c.Contenido.ReadAsync(buffer, cancelacion)) > 0)
            {
                total += leidos;
                if (total > opciones.MaxBytes)
                    return Fallo(TipoError.ArchivoDemasiadoGrande, ValidadorArte.DemasiadoGrande,
                        $"El archivo supera el máximo de {opciones.MaxBytes / (1024 * 1024)} MB.");
                sha.AppendData(buffer.AsSpan(0, leidos));
                await archivo.WriteAsync(buffer.AsMemory(0, leidos), cancelacion);
            }
            await archivo.FlushAsync(cancelacion);
            var hash = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();

            var existente = await repositorio.ObtenerPorHashAsync(hash, cancelacion);
            if (existente is not null) return ResultadoCasoDeUso<ArteDto>.Exito(ADto(existente, true));

            var (error, info) = validador.Validar(archivo);
            if (error is not null) return Fallo(TipoDe(error.Codigo), error.Codigo, error.Mensaje);

            var nombreBlob = $"{Guid.NewGuid():N}.png";
            archivo.Position = 0;
            var url = await almacen.GuardarAsync(nombreBlob, "image/png", archivo, cancelacion);
            var arte = ArchivoArte.Crear(url.ToString(), nombreBlob, "image/png", total, info!.Ancho, info.Alto, hash, reloj.GetUtcNow());
            try
            {
                if (await repositorio.AgregarAsync(arte, cancelacion)) return ResultadoCasoDeUso<ArteDto>.Exito(ADto(arte, false));
                await almacen.EliminarAsync(nombreBlob, CancellationToken.None);
                var ganador = await repositorio.ObtenerPorHashAsync(hash, cancelacion);
                return ResultadoCasoDeUso<ArteDto>.Exito(ADto(ganador!, true));
            }
            catch
            {
                await almacen.EliminarAsync(nombreBlob, CancellationToken.None); // sin huérfanos si falla el registro
                throw;
            }
        }
        catch (AlmacenNoDisponibleException)
        {
            return ResultadoCasoDeUso<ArteDto>.Fallo(ErrorUniforme.AlmacenNoDisponible(id));
        }
        finally
        {
            File.Delete(temporal);
        }

        ResultadoCasoDeUso<ArteDto> Fallo(TipoError tipo, string codigo, string mensaje) =>
            ResultadoCasoDeUso<ArteDto>.Fallo(ErrorUniforme.Arte(tipo, codigo, mensaje, id));
    }

    private static TipoError TipoDe(string codigo) => codigo switch
    {
        ValidadorArte.Png => TipoError.ArchivoNoSoportado,
        ValidadorArte.DemasiadoGrande => TipoError.ArchivoDemasiadoGrande,
        _ => TipoError.Validacion
    };

    private static ArteDto ADto(ArchivoArte a, bool reutilizado) => new(a.Id, a.Url, a.Ancho, a.Alto, a.TamanoBytes, reutilizado);
}
