using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using Mediator;

namespace BlackHellHeim.Aplicacion.Impresion;

/// <summary>Modelos propios del puerto; ningún tipo de Printful sale de Infraestructura.</summary>
public sealed record ProductoPrintfulDto(int Id, string Nombre, string Tipo, IReadOnlyList<string> Tecnicas);

public sealed record VariantePrintfulDto(
    int Id, int ProductoId, string Color, string CodigoColor, string Talla,
    decimal CostoBaseUsd, string Tecnica, bool DisponiblePais);

/// <summary>Puerto del proveedor de impresión (Printful). Los errores del proveedor llegan como ExcepcionCasoDeUso.</summary>
public interface IProveedorImpresion
{
    Task<IReadOnlyList<ProductoPrintfulDto>> ListarProductosAsync(
        string categoria, int pagina, int tamano, CancellationToken cancelacion);

    /// <summary>Null si el producto no existe.</summary>
    Task<ProductoPrintfulDto?> ObtenerProductoAsync(int id, CancellationToken cancelacion);

    /// <summary>Variantes con la técnica indicada y disponibles para el país. Null si el producto no existe.</summary>
    Task<IReadOnlyList<VariantePrintfulDto>?> ListarVariantesAsync(
        int productoId, string tecnica, string pais, CancellationToken cancelacion);
}

public static class ValoresImpresion
{
    public static readonly string[] Tecnicas = ["dtg", "dtfilm", "embroidery", "sublimation", "cut-sew", "digital"];
}

public sealed record ListarProductosQuery(string? Categoria, int Pagina, int Tamano)
    : IQuery<ResultadoCasoDeUso<IReadOnlyList<ProductoPrintfulDto>>>;

public sealed record ObtenerProductoQuery(int Id) : IQuery<ResultadoCasoDeUso<ProductoPrintfulDto>>;

public sealed record ListarVariantesQuery(int ProductoId, string? Tecnica, string? Pais)
    : IQuery<ResultadoCasoDeUso<IReadOnlyList<VariantePrintfulDto>>>;

public sealed class ValidadorListarProductos : IValidadorSolicitud<ListarProductosQuery>
{
    public IReadOnlyList<ErrorCampo> Validar(ListarProductosQuery s)
    {
        var errores = new List<ErrorCampo>();
        if (s.Pagina < 1) errores.Add(new ErrorCampo("pagina", "La página debe ser 1 o mayor."));
        if (s.Tamano is < 1 or > 100) errores.Add(new ErrorCampo("tamano", "El tamaño debe estar entre 1 y 100."));
        return errores;
    }
}

public sealed class ValidadorListarVariantes : IValidadorSolicitud<ListarVariantesQuery>
{
    public IReadOnlyList<ErrorCampo> Validar(ListarVariantesQuery s)
    {
        var errores = new List<ErrorCampo>();
        if (s.ProductoId < 1) errores.Add(new ErrorCampo("id", "El id del producto debe ser positivo."));
        if (s.Tecnica is null || !ValoresImpresion.Tecnicas.Contains(s.Tecnica.ToLowerInvariant()))
            errores.Add(new ErrorCampo("tecnica", "Técnica no admitida. Valores: " + string.Join(", ", ValoresImpresion.Tecnicas) + "."));
        if (s.Pais is null || s.Pais.Length != 2 || !s.Pais.All(char.IsAsciiLetterUpper))
            errores.Add(new ErrorCampo("pais", "El país debe ser un código ISO de dos letras mayúsculas (por ejemplo, MX)."));
        return errores;
    }
}

public sealed class ManejadorListarProductos(IProveedorImpresion proveedor)
    : IQueryHandler<ListarProductosQuery, ResultadoCasoDeUso<IReadOnlyList<ProductoPrintfulDto>>>
{
    public async ValueTask<ResultadoCasoDeUso<IReadOnlyList<ProductoPrintfulDto>>> Handle(
        ListarProductosQuery q, CancellationToken cancelacion) =>
        ResultadoCasoDeUso<IReadOnlyList<ProductoPrintfulDto>>.Exito(
            await proveedor.ListarProductosAsync(string.IsNullOrWhiteSpace(q.Categoria) ? "playeras" : q.Categoria.Trim(),
                q.Pagina, q.Tamano, cancelacion));
}

public sealed class ManejadorObtenerProducto(IProveedorImpresion proveedor, IContextoEjecucion contexto)
    : IQueryHandler<ObtenerProductoQuery, ResultadoCasoDeUso<ProductoPrintfulDto>>
{
    public async ValueTask<ResultadoCasoDeUso<ProductoPrintfulDto>> Handle(ObtenerProductoQuery q, CancellationToken cancelacion)
    {
        var producto = await proveedor.ObtenerProductoAsync(q.Id, cancelacion);
        return producto is null
            ? ResultadoCasoDeUso<ProductoPrintfulDto>.Fallo(
                ErrorUniforme.NoEncontrado("No existe un producto con ese id en el catálogo.", contexto.IdCorrelacion))
            : ResultadoCasoDeUso<ProductoPrintfulDto>.Exito(producto);
    }
}

public sealed class ManejadorListarVariantes(IProveedorImpresion proveedor, IContextoEjecucion contexto)
    : IQueryHandler<ListarVariantesQuery, ResultadoCasoDeUso<IReadOnlyList<VariantePrintfulDto>>>
{
    public async ValueTask<ResultadoCasoDeUso<IReadOnlyList<VariantePrintfulDto>>> Handle(
        ListarVariantesQuery q, CancellationToken cancelacion)
    {
        var variantes = await proveedor.ListarVariantesAsync(q.ProductoId, q.Tecnica!.ToLowerInvariant(), q.Pais!, cancelacion);
        return variantes is null
            ? ResultadoCasoDeUso<IReadOnlyList<VariantePrintfulDto>>.Fallo(
                ErrorUniforme.NoEncontrado("No existe un producto con ese id en el catálogo.", contexto.IdCorrelacion))
            : ResultadoCasoDeUso<IReadOnlyList<VariantePrintfulDto>>.Exito(variantes);
    }
}
