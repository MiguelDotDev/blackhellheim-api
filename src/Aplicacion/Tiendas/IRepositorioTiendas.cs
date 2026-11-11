using BlackHellHeim.Dominio.Tiendas;

namespace BlackHellHeim.Aplicacion.Tiendas;

/// <summary>Puerto de lectura de tiendas.</summary>
public interface IRepositorioTiendas
{
    Task<Tienda?> ObtenerPorCodigoAsync(CodigoTienda codigo, CancellationToken cancelacion);
}
