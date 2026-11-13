using BlackHellHeim.Aplicacion.Catalogo;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BlackHellHeim.Infraestructura.Catalogo;

/// <summary>Lecturas del catálogo con proyección directa (sin grafos de entidades ni seguimiento).</summary>
public sealed class ConsultasCatalogo(ContextoBlackHellHeim contexto) : IConsultasCatalogo
{
    private const string ColacionInsensible = "Latin1_General_100_CI_AI";

    public async Task<IReadOnlyList<FilaDiseno>> ListarPublicadosAsync(string? texto, string? color, string? talla, CancellationToken cancelacion)
    {
        var consulta = contexto.Disenos.AsNoTracking().Where(d => d.Estado == EstadoDiseno.Publicado);
        // Sin color ni talla basta con que haya una variante activa; con ellos, la misma variante debe cumplir ambos.
        consulta = consulta.Where(d => d.Variantes.Any(v => v.Activa
            && (color == null || EF.Functions.Collate(v.Color, ColacionInsensible) == color)
            && (talla == null || EF.Functions.Collate(v.Talla, ColacionInsensible) == talla)));
        if (texto is not null)
        {
            var patron = "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[") + "%";
            consulta = consulta.Where(d => EF.Functions.Like(EF.Functions.Collate(d.Nombre, ColacionInsensible), patron, "\\")
                || (d.Descripcion != null && EF.Functions.Like(EF.Functions.Collate(d.Descripcion, ColacionInsensible), patron, "\\")));
        }

        return await Proyectar(consulta).ToListAsync(cancelacion);
    }

    public Task<FilaDiseno?> ObtenerPublicadoAsync(string slug, CancellationToken cancelacion) =>
        Proyectar(contexto.Disenos.AsNoTracking().Where(d => d.Estado == EstadoDiseno.Publicado && d.Slug == slug
            && d.Variantes.Any(v => v.Activa))).FirstOrDefaultAsync(cancelacion);

    private static IQueryable<FilaDiseno> Proyectar(IQueryable<Diseno> consulta) =>
        consulta.Select(d => new FilaDiseno(d.Id, d.Slug, d.Nombre, d.Descripcion,
            d.ArchivoArte != null ? d.ArchivoArte.Url : null, d.CreadoUtc,
            d.Variantes.Where(v => v.Activa).Select(v => new FilaVariante(v.Color, v.CodigoColor, v.Talla, v.CostoBaseUsd)).ToList()));
}
