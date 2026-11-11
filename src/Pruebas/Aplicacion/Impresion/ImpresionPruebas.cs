using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Pruebas.Aplicacion.Autenticacion;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Impresion;

public class ImpresionPruebas
{
    private sealed class ProveedorFalso : IProveedorImpresion
    {
        public string? UltimaCategoria { get; private set; }
        public bool Existe { get; set; } = true;
        public Task<IReadOnlyList<ProductoPrintfulDto>> ListarProductosAsync(string categoria, int pagina, int tamano, CancellationToken c)
        {
            UltimaCategoria = categoria;
            return Task.FromResult<IReadOnlyList<ProductoPrintfulDto>>([new(71, "Playera", "T-SHIRT", ["dtg"])]);
        }
        public Task<ProductoPrintfulDto?> ObtenerProductoAsync(int id, CancellationToken c) =>
            Task.FromResult(Existe ? new ProductoPrintfulDto(id, "Playera", "T-SHIRT", ["dtg"]) : null);
        public Task<IReadOnlyList<VariantePrintfulDto>?> ListarVariantesAsync(int id, string t, string p, CancellationToken c) =>
            Task.FromResult<IReadOnlyList<VariantePrintfulDto>?>(Existe ? [] : null);
    }

    [Theory]
    [InlineData("dtg", "MX", 0)]
    [InlineData("DTG", "MX", 0)] // la técnica se compara sin distinguir mayúsculas
    [InlineData("tinta", "MX", 1)]
    [InlineData("dtg", "mx", 1)]
    [InlineData("dtg", "MEX", 1)]
    [InlineData(null, null, 2)]
    public void ElValidadorDeVariantesExigeTecnicaYPais(string? tecnica, string? pais, int esperados)
    {
        var errores = new ValidadorListarVariantes().Validar(new ListarVariantesQuery(71, tecnica, pais));
        Assert.Equal(esperados, errores.Count);
    }

    [Theory]
    [InlineData(0, 20, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 101, 1)]
    [InlineData(1, 100, 0)]
    public void ElValidadorDeListadoControlaLaPaginacion(int pagina, int tamano, int esperados) =>
        Assert.Equal(esperados, new ValidadorListarProductos().Validar(new ListarProductosQuery(null, pagina, tamano)).Count);

    [Fact]
    public async Task ListarUsaPlayerasPorOmision()
    {
        var proveedor = new ProveedorFalso();

        var r = await new ManejadorListarProductos(proveedor).Handle(new ListarProductosQuery(null, 1, 20), default);

        Assert.True(r.EsExito);
        Assert.Equal("playeras", proveedor.UltimaCategoria);
    }

    [Fact]
    public async Task ObtenerProductoInexistenteDa404()
    {
        var r = await new ManejadorObtenerProducto(new ProveedorFalso { Existe = false }, new ContextoFalso())
            .Handle(new ObtenerProductoQuery(9), default);

        Assert.Equal(TipoError.NoEncontrado, r.Error!.Tipo);
    }

    [Fact]
    public async Task ListarVariantesDevuelveListaVaciaOError404()
    {
        var vacio = await new ManejadorListarVariantes(new ProveedorFalso(), new ContextoFalso())
            .Handle(new ListarVariantesQuery(71, "dtg", "MX"), default);
        var noExiste = await new ManejadorListarVariantes(new ProveedorFalso { Existe = false }, new ContextoFalso())
            .Handle(new ListarVariantesQuery(9, "dtg", "MX"), default);

        Assert.True(vacio.EsExito);
        Assert.Empty(vacio.Valor!);
        Assert.Equal(TipoError.NoEncontrado, noExiste.Error!.Tipo);
    }
}
