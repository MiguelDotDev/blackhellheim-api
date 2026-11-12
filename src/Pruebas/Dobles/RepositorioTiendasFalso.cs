using BlackHellHeim.Aplicacion.Tiendas;
using BlackHellHeim.Dominio.Tiendas;

namespace BlackHellHeim.Pruebas.Dobles;

public sealed class RepositorioTiendasFalso(params Tienda[] tiendas) : IRepositorioTiendas
{
    public int Consultas { get; private set; }

    public CodigoTienda? UltimoCodigo { get; private set; }

    public Task<Tienda?> ObtenerPorCodigoAsync(CodigoTienda codigo, CancellationToken cancelacion)
    {
        Consultas++;
        UltimoCodigo = codigo;
        return Task.FromResult(tiendas.FirstOrDefault(t => t.Codigo == codigo));
    }
}
