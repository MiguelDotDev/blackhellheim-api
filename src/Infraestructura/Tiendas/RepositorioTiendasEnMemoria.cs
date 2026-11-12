using BlackHellHeim.Aplicacion.Tiendas;
using BlackHellHeim.Dominio.Tiendas;

namespace BlackHellHeim.Infraestructura.Tiendas;

/// <summary>Adaptador de ejemplo en memoria; los datos son ilustrativos, no reglas de negocio.</summary>
public sealed class RepositorioTiendasEnMemoria : IRepositorioTiendas
{
    private readonly Dictionary<CodigoTienda, Tienda> _tiendas = new[]
    {
        new Tienda(CodigoTienda.Crear("BHH"), "BlackHellHeim", "MXN"),
        new Tienda(CodigoTienda.Crear("DEM"), "Tienda de demostración", "MXN")
    }.ToDictionary(t => t.Codigo);

    public Task<Tienda?> ObtenerPorCodigoAsync(CodigoTienda codigo, CancellationToken cancelacion) =>
        Task.FromResult(_tiendas.GetValueOrDefault(codigo));
}
