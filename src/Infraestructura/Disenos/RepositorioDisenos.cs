using BlackHellHeim.Aplicacion.Disenos;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BlackHellHeim.Infraestructura.Disenos;

public sealed class RepositorioDisenos(ContextoBlackHellHeim contexto) : IRepositorioDisenos
{
    private IQueryable<Diseno> Consulta => contexto.Disenos.Include(d => d.ArchivoArte)
        .Include(d => d.Variantes).ThenInclude(v => v.ProductoBase).AsSplitQuery();

    public Task<Diseno?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
        Consulta.FirstOrDefaultAsync(d => d.Id == id, cancelacion);

    public Task<bool> ExisteNombreOSlugAsync(string nombre, string slug, Guid? excepto, CancellationToken cancelacion) =>
        contexto.Disenos.AnyAsync(d => (d.Nombre == nombre || d.Slug == slug) && d.Id != excepto, cancelacion);

    public Task<ArchivoArte?> ObtenerArteAsync(Guid id, CancellationToken cancelacion) =>
        contexto.ArchivosArte.FirstOrDefaultAsync(a => a.Id == id, cancelacion);

    public async Task<IReadOnlyList<Variante>> ObtenerOCrearVariantesAsync(
        IReadOnlyList<VarianteDisenoDto> variantes, CancellationToken cancelacion)
    {
        var resultado = new List<Variante>();
        foreach (var dto in variantes.DistinctBy(v => v.PrintfulVarianteId))
        {
            var variante = await contexto.Variantes.Include(v => v.ProductoBase)
                .FirstOrDefaultAsync(v => v.PrintfulVarianteId == dto.PrintfulVarianteId, cancelacion);
            if (variante is null)
            {
                // El producto base se reutiliza por su id de Printful (también el recién creado en esta misma llamada).
                var producto = contexto.ProductosBase.Local.FirstOrDefault(p => p.PrintfulProductoId == dto.PrintfulProductoId)
                    ?? await contexto.ProductosBase.FirstOrDefaultAsync(p => p.PrintfulProductoId == dto.PrintfulProductoId, cancelacion);
                if (producto is null)
                {
                    producto = ProductoBase.Crear(dto.PrintfulProductoId, dto.NombreProducto ?? $"Producto {dto.PrintfulProductoId}", "T-SHIRT");
                    contexto.ProductosBase.Add(producto);
                }

                variante = Variante.Crear(producto, dto.PrintfulVarianteId, dto.Color, dto.CodigoColor, dto.Talla, dto.CostoBaseUsd);
                contexto.Variantes.Add(variante);
            }
            else
            {
                variante.ActualizarDatos(dto.Color, dto.CodigoColor, dto.Talla, dto.CostoBaseUsd);
            }

            resultado.Add(variante);
        }

        return resultado;
    }

    public void Agregar(Diseno diseno) => contexto.Disenos.Add(diseno);

    public async Task<ResultadoGuardado> GuardarAsync(Diseno diseno, byte[]? versionEsperada, CancellationToken cancelacion)
    {
        if (versionEsperada is not null)
        {
            contexto.Entry(diseno).Property(d => d.Version).OriginalValue = versionEsperada;
        }

        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return ResultadoGuardado.Correcto;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoGuardado.Concurrencia;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return ResultadoGuardado.Duplicado;
        }
    }

    public async Task<(IReadOnlyList<Diseno> Elementos, int Total)> ListarAsync(
        EstadoDiseno? estado, int pagina, int tamano, CancellationToken cancelacion)
    {
        var filtro = Consulta.AsNoTracking().Where(d => estado == null || d.Estado == estado);
        var total = await filtro.CountAsync(cancelacion);
        var elementos = await filtro.OrderByDescending(d => d.CreadoUtc).ThenBy(d => d.Id)
            .Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(cancelacion);
        return (elementos, total);
    }
}
