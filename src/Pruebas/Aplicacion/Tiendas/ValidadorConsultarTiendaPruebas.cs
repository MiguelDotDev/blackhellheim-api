using BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Tiendas;

public class ValidadorConsultarTiendaPruebas
{
    private readonly ValidadorConsultarTienda _validador = new();

    [Theory]
    [InlineData("BHH")]
    [InlineData("DEM")]
    [InlineData("ZZZ")]
    public void AceptaCodigosDeTresLetrasMayusculas(string codigo)
    {
        Assert.Empty(_validador.Validar(new ConsultarTiendaQuery(codigo)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("B")]
    [InlineData("BH")]
    [InlineData("BHHH")]
    [InlineData("bhh")]
    [InlineData("B1H")]
    [InlineData("B-H")]
    [InlineData("BÁH")]
    public void RechazaCodigosInvalidosIndicandoElCampo(string? codigo)
    {
        var errores = _validador.Validar(new ConsultarTiendaQuery(codigo!));

        var error = Assert.Single(errores);
        Assert.Equal("codigo", error.Campo);
        Assert.Equal("El código debe tener exactamente tres letras mayúsculas.", error.Mensaje);
    }
}
