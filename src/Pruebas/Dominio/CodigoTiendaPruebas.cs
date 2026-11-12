using BlackHellHeim.Dominio.Tiendas;
using Xunit;

namespace BlackHellHeim.Pruebas.Dominio;

public class CodigoTiendaPruebas
{
    [Theory]
    [InlineData("BHH", "BHH")]
    [InlineData("dem", "DEM")]
    [InlineData("  bhh  ", "BHH")]
    public void CreaCodigosValidosNormalizados(string entrada, string esperado)
    {
        var codigo = CodigoTienda.Crear(entrada);

        Assert.Equal(esperado, codigo.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("B")]
    [InlineData("BH")]
    [InlineData("BHHH")]
    [InlineData("B1H")]
    [InlineData("BÁH")]
    [InlineData("B H")]
    [InlineData("B-H")]
    public void RechazaCodigosInvalidos(string entrada)
    {
        Assert.Throws<ArgumentException>(() => CodigoTienda.Crear(entrada));
    }

    [Fact]
    public void RechazaCodigoNulo()
    {
        Assert.Throws<ArgumentException>(() => CodigoTienda.Crear(null!));
    }

    [Fact]
    public void LaIgualdadEsPorValor()
    {
        Assert.Equal(CodigoTienda.Crear("bhh"), CodigoTienda.Crear("BHH"));
        Assert.NotEqual(CodigoTienda.Crear("BHH"), CodigoTienda.Crear("DEM"));
    }
}
