using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using BlackHellHeim.Aplicacion.Precios;
using Mediator;

namespace BlackHellHeim.Aplicacion.Catalogo;

public sealed record ColorDto(string Nombre, string Codigo);

public sealed record PlayeraResumenDto(string Slug, string Nombre, string? ImagenUrl, decimal? PrecioDesdeMxn, IReadOnlyList<ColorDto> Colores);

public sealed record PaginaPlayerasDto(IReadOnlyList<PlayeraResumenDto> Elementos, int Pagina, int Tamano, int Total);

public sealed record VarianteCatalogoDto(string Color, string CodigoColor, string Talla, decimal? PrecioMxn, bool Disponible);

public sealed record PlayeraDetalleDto(
    string Slug, string Nombre, string? Descripcion, IReadOnlyList<string> Imagenes, IReadOnlyList<ColorDto> Colores,
    IReadOnlyList<string> Tallas, IReadOnlyList<VarianteCatalogoDto> Variantes);

/// <summary>Proyección ligera de un diseño publicado con sus variantes activas (incluye el costo base: nunca sale en un DTO).</summary>
public sealed record FilaVariante(string Color, string CodigoColor, string Talla, decimal CostoBaseUsd);

public sealed record FilaDiseno(
    Guid Id, string Slug, string Nombre, string? Descripcion, string? ImagenUrl, DateTimeOffset CreadoUtc, IReadOnlyList<FilaVariante> Variantes);

/// <summary>Puerto de lectura del catálogo (solo diseños Publicado con al menos una variante activa).</summary>
public interface IConsultasCatalogo
{
    /// <summary>Aplica en la base el texto, el color y la talla (misma variante activa); el resto se resuelve en memoria.</summary>
    Task<IReadOnlyList<FilaDiseno>> ListarPublicadosAsync(string? texto, string? color, string? talla, CancellationToken cancelacion);

    Task<FilaDiseno?> ObtenerPublicadoAsync(string slug, CancellationToken cancelacion);
}

public static class OrdenCatalogo
{
    public const string Novedad = "novedad", PrecioAsc = "precio_asc", PrecioDesc = "precio_desc", Nombre = "nombre";
    public static readonly string[] Validos = [Novedad, PrecioAsc, PrecioDesc, Nombre];
}

public sealed record ListarCatalogoQuery(
    int? Pagina, int? Tamano, string? Texto, string? Color, string? Talla, decimal? PrecioMin, decimal? PrecioMax, string? Orden)
    : IQuery<ResultadoCasoDeUso<PaginaPlayerasDto>>;

public sealed record ObtenerPlayeraQuery(string Slug) : IQuery<ResultadoCasoDeUso<PlayeraDetalleDto>>;

public sealed class ValidadorListarCatalogo : IValidadorSolicitud<ListarCatalogoQuery>
{
    public IReadOnlyList<ErrorCampo> Validar(ListarCatalogoQuery q)
    {
        var e = new List<ErrorCampo>();
        if (q.Pagina is < 1) e.Add(new ErrorCampo("pagina", "La página debe ser 1 o mayor."));
        if (q.Tamano is < 1 or > 50) e.Add(new ErrorCampo("tamano", "El tamaño debe estar entre 1 y 50."));
        if (q.Texto is { Length: > 100 }) e.Add(new ErrorCampo("texto", "El texto no puede superar 100 caracteres."));
        if (q.PrecioMin is < 0) e.Add(new ErrorCampo("precioMin", "El precio mínimo no puede ser negativo."));
        if (q.PrecioMax is < 0) e.Add(new ErrorCampo("precioMax", "El precio máximo no puede ser negativo."));
        if (q.PrecioMin > q.PrecioMax) e.Add(new ErrorCampo("precioMin", "El precio mínimo no puede ser mayor que el máximo."));
        if (q.Orden is not null && !OrdenCatalogo.Validos.Contains(q.Orden))
            e.Add(new ErrorCampo("orden", $"El orden debe ser uno de: {string.Join(", ", OrdenCatalogo.Validos)}."));
        return e;
    }
}

internal static class OrdenTallas
{
    private static readonly string[] Conocidas = ["XXS", "XS", "S", "M", "L", "XL", "2XL", "3XL", "4XL", "5XL"];

    public static int Peso(string talla)
    {
        var i = Array.IndexOf(Conocidas, talla.ToUpperInvariant());
        return i < 0 ? int.MaxValue : i;
    }
}

public sealed class ManejadorListarCatalogo(IConsultasCatalogo consultas, ICalculadoraPrecios precios)
    : IQueryHandler<ListarCatalogoQuery, ResultadoCasoDeUso<PaginaPlayerasDto>>
{
    private sealed record Candidato(FilaDiseno Fila, decimal? PrecioDesde);

    public async ValueTask<ResultadoCasoDeUso<PaginaPlayerasDto>> Handle(ListarCatalogoQuery q, CancellationToken cancelacion)
    {
        var pagina = q.Pagina ?? 1;
        var tamano = q.Tamano ?? 12;
        var filas = await consultas.ListarPublicadosAsync(Limpio(q.Texto), Limpio(q.Color), Limpio(q.Talla), cancelacion);
        var calculo = await precios.PrepararAsync(cancelacion); // un solo tipo de cambio por petición
        decimal? Precio(FilaVariante v) => calculo?.Calcular(v.CostoBaseUsd).PrecioFinalMxn;

        var conPrecio = q.PrecioMin is not null || q.PrecioMax is not null;
        var candidatos = new List<Candidato>();
        foreach (var f in filas)
        {
            var precios_ = f.Variantes.Select(Precio).ToList();
            if (conPrecio)
            {
                // Las variantes que cuentan para el rango son las que cumplen color y talla.
                var considerar = f.Variantes.Select((v, i) => (v, p: precios_[i]))
                    .Where(x => (q.Color is null || Igual(x.v.Color, q.Color)) && (q.Talla is null || Igual(x.v.Talla, q.Talla)));
                if (!considerar.Any(x => x.p is { } p && p >= (q.PrecioMin ?? 0) && p <= (q.PrecioMax ?? decimal.MaxValue))) continue;
            }

            candidatos.Add(new Candidato(f, precios_.Any(p => p is null) ? null : precios_.Min()));
        }

        IEnumerable<Candidato> ordenados = (q.Orden ?? OrdenCatalogo.Novedad) switch
        {
            OrdenCatalogo.PrecioAsc => candidatos.OrderBy(c => c.PrecioDesde is null).ThenBy(c => c.PrecioDesde).ThenBy(c => c.Fila.Id),
            OrdenCatalogo.PrecioDesc => candidatos.OrderBy(c => c.PrecioDesde is null).ThenByDescending(c => c.PrecioDesde).ThenBy(c => c.Fila.Id),
            OrdenCatalogo.Nombre => candidatos.OrderBy(c => c.Fila.Nombre, StringComparer.CurrentCultureIgnoreCase).ThenBy(c => c.Fila.Id),
            _ => candidatos.OrderByDescending(c => c.Fila.CreadoUtc).ThenBy(c => c.Fila.Id)
        };

        var elementos = ordenados.Skip((pagina - 1) * tamano).Take(tamano)
            .Select(c => new PlayeraResumenDto(c.Fila.Slug, c.Fila.Nombre, c.Fila.ImagenUrl, c.PrecioDesde, Colores(c.Fila.Variantes)))
            .ToList();
        return ResultadoCasoDeUso<PaginaPlayerasDto>.Exito(new PaginaPlayerasDto(elementos, pagina, tamano, candidatos.Count));
    }

    internal static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static bool Igual(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    internal static IReadOnlyList<ColorDto> Colores(IEnumerable<FilaVariante> variantes) =>
        variantes.Select(v => new ColorDto(v.Color, v.CodigoColor)).Distinct().OrderBy(c => c.Nombre).ToList();
}

public sealed class ManejadorObtenerPlayera(IConsultasCatalogo consultas, ICalculadoraPrecios precios, IContextoEjecucion contexto)
    : IQueryHandler<ObtenerPlayeraQuery, ResultadoCasoDeUso<PlayeraDetalleDto>>
{
    public async ValueTask<ResultadoCasoDeUso<PlayeraDetalleDto>> Handle(ObtenerPlayeraQuery q, CancellationToken cancelacion)
    {
        var fila = await consultas.ObtenerPublicadoAsync(q.Slug.Trim().ToLowerInvariant(), cancelacion);
        if (fila is null)
            return ResultadoCasoDeUso<PlayeraDetalleDto>.Fallo(ErrorUniforme.NoEncontrado("No existe una playera con ese slug.", contexto.IdCorrelacion));

        var calculo = await precios.PrepararAsync(cancelacion);
        var variantes = fila.Variantes.OrderBy(v => v.Color).ThenBy(v => OrdenTallas.Peso(v.Talla)).ThenBy(v => v.Talla)
            .Select(v => new VarianteCatalogoDto(v.Color, v.CodigoColor, v.Talla, calculo?.Calcular(v.CostoBaseUsd).PrecioFinalMxn, true))
            .ToList();
        return ResultadoCasoDeUso<PlayeraDetalleDto>.Exito(new PlayeraDetalleDto(
            fila.Slug, fila.Nombre, fila.Descripcion, fila.ImagenUrl is null ? [] : [fila.ImagenUrl],
            ManejadorListarCatalogo.Colores(fila.Variantes),
            fila.Variantes.Select(v => v.Talla).Distinct().OrderBy(OrdenTallas.Peso).ThenBy(t => t).ToList(), variantes));
    }
}
