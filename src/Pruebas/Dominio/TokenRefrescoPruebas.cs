using BlackHellHeim.Dominio.Autenticacion;
using Xunit;

namespace BlackHellHeim.Pruebas.Dominio;

public class TokenRefrescoPruebas
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UnTokenRecienCreadoEstaActivoHastaQueCaduca()
    {
        var token = TokenRefresco.Crear(Guid.NewGuid(), Guid.NewGuid(), "hash", Ahora, TimeSpan.FromDays(7));

        Assert.True(token.EstaActivo(Ahora));
        Assert.True(token.EstaActivo(Ahora.AddDays(7).AddTicks(-1)));
        Assert.False(token.EstaActivo(Ahora.AddDays(7)));
        Assert.False(token.FueConsumido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ElHashEsObligatorio(string hash) =>
        Assert.Throws<ArgumentException>(() => TokenRefresco.Crear(Guid.NewGuid(), Guid.NewGuid(), hash, Ahora, TimeSpan.FromDays(1)));

    [Fact]
    public void LaVidaDebeSerPositiva() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenRefresco.Crear(Guid.NewGuid(), Guid.NewGuid(), "h", Ahora, TimeSpan.Zero));
}
