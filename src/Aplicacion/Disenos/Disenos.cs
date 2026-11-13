using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using BlackHellHeim.Dominio.Disenos;
using Mediator;

namespace BlackHellHeim.Aplicacion.Disenos;

public sealed record VarianteDisenoDto(
    int PrintfulVarianteId, int PrintfulProductoId, string? NombreProducto, string Color, string CodigoColor,
    string Talla, decimal CostoBaseUsd);

public sealed class DisenoSolicitud
{
    public string? Nombre { get; init; }
    public string? Descripcion { get; init; }
    public Guid? ArchivoArteId { get; init; }
    public List<VarianteDisenoDto>? Variantes { get; init; }
    public string? Estado { get; init; }
}

public sealed record DisenoDto(
    Guid Id, string Nombre, string Slug, string? Descripcion, string Estado, string? ArchivoArteUrl,
    IReadOnlyList<VarianteDisenoDto> Variantes, string Version, IReadOnlyList<string> Avisos);

public sealed record PaginaDisenosDto(IReadOnlyList<DisenoDto> Elementos, int Pagina, int Tamano, int Total);

public enum ResultadoGuardado { Correcto, Duplicado, Concurrencia }

public interface IRepositorioDisenos
{
    Task<Diseno?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    Task<bool> ExisteNombreOSlugAsync(string nombre, string slug, Guid? excepto, CancellationToken cancelacion);

    Task<ArchivoArte?> ObtenerArteAsync(Guid id, CancellationToken cancelacion);

    /// <summary>Devuelve las variantes persistentes (las crea o actualiza, y el producto base si falta).</summary>
    Task<IReadOnlyList<Variante>> ObtenerOCrearVariantesAsync(IReadOnlyList<VarianteDisenoDto> variantes, CancellationToken cancelacion);

    void Agregar(Diseno diseno);

    /// <summary>Guarda; si se pasa <paramref name="versionEsperada"/> falla con Concurrencia cuando ya cambió.</summary>
    Task<ResultadoGuardado> GuardarAsync(Diseno diseno, byte[]? versionEsperada, CancellationToken cancelacion);

    Task<(IReadOnlyList<Diseno> Elementos, int Total)> ListarAsync(EstadoDiseno? estado, int pagina, int tamano, CancellationToken cancelacion);
}

/// <summary>Términos (marcas, bandas) que obligan a revisar el diseño; solo avisan, no bloquean (constitución X).</summary>
public interface ITerminosBloqueados
{
    IReadOnlyList<string> Terminos { get; }
}

public static class MapeoDisenos
{
    public static string VersionComoTexto(byte[] version) => Convert.ToBase64String(version);

    public static byte[]? VersionDesdeTexto(string? texto)
    {
        try { return string.IsNullOrWhiteSpace(texto) ? null : Convert.FromBase64String(texto.Trim('"')); }
        catch (FormatException) { return null; }
    }

    public static DisenoDto ADto(Diseno d, ITerminosBloqueados terminos) => new(
        d.Id, d.Nombre, d.Slug, d.Descripcion, d.Estado.ToString(), d.ArchivoArte?.Url,
        d.Variantes.Select(v => new VarianteDisenoDto(v.PrintfulVarianteId, v.ProductoBase.PrintfulProductoId, v.ProductoBase.Nombre,
            v.Color, v.CodigoColor, v.Talla, v.CostoBaseUsd)).OrderBy(v => v.Color).ThenBy(v => v.Talla).ToList(),
        VersionComoTexto(d.Version),
        terminos.Terminos.Where(t => d.Nombre.Contains(t, StringComparison.OrdinalIgnoreCase)
                || (d.Descripcion?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(t => $"El diseño menciona «{t}»: confirma que no usa marcas ni nombres de terceros.").ToList());
}

public sealed record CrearDisenoComando(DisenoSolicitud Solicitud) : ICommand<ResultadoCasoDeUso<DisenoDto>>;

public sealed record EditarDisenoComando(Guid Id, string? Version, DisenoSolicitud Solicitud) : ICommand<ResultadoCasoDeUso<DisenoDto>>;

public sealed record ObtenerDisenoQuery(Guid Id) : IQuery<ResultadoCasoDeUso<DisenoDto>>;

public sealed record ListarDisenosQuery(string? Estado, int Pagina, int Tamano) : IQuery<ResultadoCasoDeUso<PaginaDisenosDto>>;

public static class ReglasSolicitudDiseno
{
    public static List<ErrorCampo> Validar(DisenoSolicitud s)
    {
        var errores = new List<ErrorCampo>();
        var nombre = (s.Nombre ?? "").Trim();
        if (nombre.Length is < Diseno.NombreMinimo or > Diseno.NombreMaximo || Diseno.GenerarSlug(nombre).Length == 0)
            errores.Add(new ErrorCampo("nombre", $"El nombre debe tener entre {Diseno.NombreMinimo} y {Diseno.NombreMaximo} caracteres, con letras o números."));
        if (s.Descripcion is { Length: > Diseno.DescripcionMaxima })
            errores.Add(new ErrorCampo("descripcion", $"La descripción no puede superar {Diseno.DescripcionMaxima} caracteres."));
        if (s.Estado is not null && !Enum.TryParse<EstadoDiseno>(s.Estado, out _))
            errores.Add(new ErrorCampo("estado", "El estado debe ser Borrador, Publicado o Archivado."));
        var variantes = s.Variantes ?? [];
        if (variantes.Any(v => v.PrintfulVarianteId < 1 || v.PrintfulProductoId < 1 || v.CostoBaseUsd < 0
                || string.IsNullOrWhiteSpace(v.Color) || string.IsNullOrWhiteSpace(v.Talla)))
            errores.Add(new ErrorCampo("variantes", "Cada variante necesita ids positivos, color, talla y un costo no negativo."));
        else if (variantes.Select(v => v.PrintfulProductoId).Distinct().Count() > 1)
            errores.Add(new ErrorCampo("variantes", "Todas las variantes deben ser del mismo producto base."));
        return errores;
    }
}

public sealed class ValidadorCrearDiseno : IValidadorSolicitud<CrearDisenoComando>
{
    public IReadOnlyList<ErrorCampo> Validar(CrearDisenoComando c) => ReglasSolicitudDiseno.Validar(c.Solicitud);
}

public sealed class ValidadorEditarDiseno : IValidadorSolicitud<EditarDisenoComando>
{
    public IReadOnlyList<ErrorCampo> Validar(EditarDisenoComando c)
    {
        var errores = ReglasSolicitudDiseno.Validar(c.Solicitud);
        if (MapeoDisenos.VersionDesdeTexto(c.Version) is null)
            errores.Add(new ErrorCampo("ifMatch", "Falta la cabecera If-Match con la versión del diseño."));
        return errores;
    }
}

public sealed class ValidadorListarDisenos : IValidadorSolicitud<ListarDisenosQuery>
{
    public IReadOnlyList<ErrorCampo> Validar(ListarDisenosQuery q)
    {
        var errores = new List<ErrorCampo>();
        if (q.Pagina < 1) errores.Add(new ErrorCampo("pagina", "La página debe ser 1 o mayor."));
        if (q.Tamano is < 1 or > 100) errores.Add(new ErrorCampo("tamano", "El tamaño debe estar entre 1 y 100."));
        if (q.Estado is not null && !Enum.TryParse<EstadoDiseno>(q.Estado, out _))
            errores.Add(new ErrorCampo("estado", "El estado debe ser Borrador, Publicado o Archivado."));
        return errores;
    }
}

/// <summary>Aplica una solicitud a un diseño (alta y edición comparten el flujo).</summary>
internal sealed class AplicadorDiseno(IRepositorioDisenos repositorio, TimeProvider reloj, IContextoEjecucion contexto)
{
    public async Task<ErrorUniforme?> AplicarAsync(Diseno diseno, DisenoSolicitud s, bool esAlta, CancellationToken cancelacion)
    {
        var ahora = reloj.GetUtcNow();
        try
        {
            if (!esAlta) diseno.EditarDatos(s.Nombre!, s.Descripcion, ahora);

            if (await repositorio.ExisteNombreOSlugAsync(diseno.Nombre, diseno.Slug, esAlta ? null : diseno.Id, cancelacion))
                return ErrorUniforme.Conflicto("Ya existe un diseño con ese nombre.", contexto.IdCorrelacion);

            if (s.ArchivoArteId is { } arteId)
            {
                var arte = await repositorio.ObtenerArteAsync(arteId, cancelacion);
                if (arte is null)
                    return ErrorUniforme.DeValidacion([new ErrorCampo("archivoArteId", "El archivo de arte no existe.")], contexto.IdCorrelacion);
                diseno.AsignarArte(arte, ahora);
            }

            if (s.Variantes is not null)
                diseno.ReemplazarVariantes(await repositorio.ObtenerOCrearVariantesAsync(s.Variantes, cancelacion), ahora);

            if (s.Estado is not null) diseno.CambiarEstado(Enum.Parse<EstadoDiseno>(s.Estado), ahora);
            return null;
        }
        catch (ReglaDeNegocioException regla)
        {
            return ErrorUniforme.Conflicto(regla.Message, contexto.IdCorrelacion);
        }
    }
}

public sealed class ManejadorCrearDiseno(
    IRepositorioDisenos repositorio, ITerminosBloqueados terminos, TimeProvider reloj, IContextoEjecucion contexto)
    : ICommandHandler<CrearDisenoComando, ResultadoCasoDeUso<DisenoDto>>
{
    public async ValueTask<ResultadoCasoDeUso<DisenoDto>> Handle(CrearDisenoComando c, CancellationToken cancelacion)
    {
        var s = c.Solicitud;
        var diseno = Diseno.Crear(s.Nombre!, s.Descripcion, reloj.GetUtcNow());
        var error = await new AplicadorDiseno(repositorio, reloj, contexto).AplicarAsync(diseno, s, true, cancelacion);
        if (error is not null) return ResultadoCasoDeUso<DisenoDto>.Fallo(error);

        repositorio.Agregar(diseno);
        return await repositorio.GuardarAsync(diseno, null, cancelacion) switch
        {
            ResultadoGuardado.Correcto => ResultadoCasoDeUso<DisenoDto>.Exito(MapeoDisenos.ADto(diseno, terminos)),
            _ => ResultadoCasoDeUso<DisenoDto>.Fallo(ErrorUniforme.Conflicto("Ya existe un diseño con ese nombre.", contexto.IdCorrelacion))
        };
    }
}

public sealed class ManejadorEditarDiseno(
    IRepositorioDisenos repositorio, ITerminosBloqueados terminos, TimeProvider reloj, IContextoEjecucion contexto)
    : ICommandHandler<EditarDisenoComando, ResultadoCasoDeUso<DisenoDto>>
{
    public async ValueTask<ResultadoCasoDeUso<DisenoDto>> Handle(EditarDisenoComando c, CancellationToken cancelacion)
    {
        var diseno = await repositorio.ObtenerAsync(c.Id, cancelacion);
        if (diseno is null)
            return ResultadoCasoDeUso<DisenoDto>.Fallo(ErrorUniforme.NoEncontrado("No existe un diseño con ese id.", contexto.IdCorrelacion));

        var esperada = MapeoDisenos.VersionDesdeTexto(c.Version)!;
        if (!diseno.Version.SequenceEqual(esperada))
            return ResultadoCasoDeUso<DisenoDto>.Fallo(ErrorUniforme.PrecondicionFallida(contexto.IdCorrelacion));

        var error = await new AplicadorDiseno(repositorio, reloj, contexto).AplicarAsync(diseno, c.Solicitud, false, cancelacion);
        if (error is not null) return ResultadoCasoDeUso<DisenoDto>.Fallo(error);

        return await repositorio.GuardarAsync(diseno, esperada, cancelacion) switch
        {
            ResultadoGuardado.Correcto => ResultadoCasoDeUso<DisenoDto>.Exito(MapeoDisenos.ADto(diseno, terminos)),
            ResultadoGuardado.Concurrencia => ResultadoCasoDeUso<DisenoDto>.Fallo(ErrorUniforme.PrecondicionFallida(contexto.IdCorrelacion)),
            _ => ResultadoCasoDeUso<DisenoDto>.Fallo(ErrorUniforme.Conflicto("Ya existe un diseño con ese nombre.", contexto.IdCorrelacion))
        };
    }
}

public sealed class ManejadorObtenerDiseno(IRepositorioDisenos repositorio, ITerminosBloqueados terminos, IContextoEjecucion contexto)
    : IQueryHandler<ObtenerDisenoQuery, ResultadoCasoDeUso<DisenoDto>>
{
    public async ValueTask<ResultadoCasoDeUso<DisenoDto>> Handle(ObtenerDisenoQuery q, CancellationToken cancelacion)
    {
        var diseno = await repositorio.ObtenerAsync(q.Id, cancelacion);
        return diseno is null
            ? ResultadoCasoDeUso<DisenoDto>.Fallo(ErrorUniforme.NoEncontrado("No existe un diseño con ese id.", contexto.IdCorrelacion))
            : ResultadoCasoDeUso<DisenoDto>.Exito(MapeoDisenos.ADto(diseno, terminos));
    }
}

public sealed class ManejadorListarDisenos(IRepositorioDisenos repositorio, ITerminosBloqueados terminos)
    : IQueryHandler<ListarDisenosQuery, ResultadoCasoDeUso<PaginaDisenosDto>>
{
    public async ValueTask<ResultadoCasoDeUso<PaginaDisenosDto>> Handle(ListarDisenosQuery q, CancellationToken cancelacion)
    {
        var estado = q.Estado is null ? (EstadoDiseno?)null : Enum.Parse<EstadoDiseno>(q.Estado);
        var (elementos, total) = await repositorio.ListarAsync(estado, q.Pagina, q.Tamano, cancelacion);
        return ResultadoCasoDeUso<PaginaDisenosDto>.Exito(
            new PaginaDisenosDto(elementos.Select(d => MapeoDisenos.ADto(d, terminos)).ToList(), q.Pagina, q.Tamano, total));
    }
}
