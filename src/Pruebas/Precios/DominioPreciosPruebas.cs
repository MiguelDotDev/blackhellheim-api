using BlackHellHeim.Dominio.Precios;
using Xunit;

namespace BlackHellHeim.Pruebas.Precios;

public class RedondeoComercialPruebas
{
    [Theory]
    [InlineData("412.30", "419")]
    [InlineData("449.01", "459")]
    [InlineData("489.99", "499")]
    [InlineData("490.00", "499")]
    [InlineData("499.00", "499")]
    [InlineData("499.01", "509")]
    [InlineData("0", "9")]
    [InlineData("9", "9")]
    [InlineData("9.01", "19")]
    [InlineData("19", "19")]
    [InlineData("19.0000000000000000000000001", "29")]
    [InlineData("1000", "1009")]
    public void RedondeaAlEnteroTerminadoEn9HaciaArriba(string bruto, string esperado) =>
        Assert.Equal(decimal.Parse(esperado), RedondeoComercial.AEntero9(decimal.Parse(bruto)));

    [Fact]
    public void UnBrutoNegativoLanza() => Assert.Throws<ArgumentOutOfRangeException>(() => RedondeoComercial.AEntero9(-0.01m));

    [Fact]
    public void ElPrecioTerminaEn9NuncaBajaDelBrutoYSeQuedaALoMasADiezPesos()
    {
        var azar = new Random(20261111);
        for (var i = 0; i < 1000; i++)
        {
            var bruto = Math.Round((decimal)azar.NextDouble() * 5000m, azar.Next(0, 6));
            var final = RedondeoComercial.AEntero9(bruto);
            Assert.Equal(0, final % 1);
            Assert.Equal(9, final % 10);
            Assert.True(final >= bruto, $"{final} < {bruto}");
            Assert.True(final - bruto < 10 || bruto < 9, $"{final} está demasiado lejos de {bruto}");
        }
    }
}

public class CalculadoraPreciosPruebas
{
    [Theory]
    [InlineData("10", "0", "18", "0.03", "0.4", "0.036", "329")]
    [InlineData("0", "0", "18", "0.03", "0.4", "0.036", "9")]
    [InlineData("12.5", "0", "17.5", "0.03", "0.4", "0.036", "409")]
    [InlineData("12.5", "2", "17.5", "0.03", "0.4", "0.036", "469")]
    [InlineData("8.95", "0", "19.2345", "0.05", "0.35", "0.03", "299")]
    [InlineData("15", "0", "16", "0", "0", "0", "249")]
    [InlineData("15", "0", "16", "0", "0.5", "0", "489")]
    [InlineData("100", "0", "20", "0.03", "0.4", "0.036", "3659")]
    [InlineData("5", "0", "1", "0", "0", "0", "9")]
    [InlineData("9", "0", "1", "0", "0", "0", "9")]
    [InlineData("0.01", "0", "1", "0", "0", "0", "9")]
    [InlineData("30", "5", "18.1234", "0.03", "0.4", "0.036", "1159")]
    [InlineData("7.77", "0", "18.5", "0.02", "0.3", "0.036", "229")]
    [InlineData("22.4", "1.5", "17.9", "0.04", "0.45", "0.036", "869")]
    [InlineData("3", "0", "18", "0.03", "0.9", "0", "559")]
    [InlineData("3", "0", "18", "0.03", "0", "0.5", "119")]
    [InlineData("11", "0", "18", "0", "0.4", "0.036", "359")]
    [InlineData("11", "0", "18", "1", "0.4", "0.036", "709")]
    [InlineData("20", "0", "25", "0.03", "0.4", "0.036", "919")]
    [InlineData("2.5", "0", "18", "0.03", "0.4", "0.036", "89")]
    [InlineData("13.25", "0", "18.4321", "0.03", "0.38", "0.036", "439")]
    [InlineData("45", "3", "18", "0.03", "0.4", "0.036", "1579")]
    [InlineData("9.99", "0", "20.0001", "0.03", "0.4", "0.036", "369")]
    public void CoincideConLaFormulaDeLaConstitucion(string costo, string extra, string tc, string colchon, string margen, string comision, string esperado)
    {
        var d = CalculadoraPrecios.Calcular(decimal.Parse(costo), decimal.Parse(extra), decimal.Parse(tc),
            decimal.Parse(colchon), decimal.Parse(margen), decimal.Parse(comision));

        Assert.Equal(decimal.Parse(esperado), d.PrecioFinalMxn);
        Assert.Equal((decimal.Parse(costo) + decimal.Parse(extra)) * decimal.Parse(tc) * (1 + decimal.Parse(colchon)), d.CostoMxn);
        Assert.True(d.PrecioFinalMxn >= d.PrecioBrutoMxn);
    }

    [Fact]
    public void ElEjemploDeLaSpecDaElBrutoExactoEnDecimal()
    {
        var d = CalculadoraPrecios.Calcular(10m, 0m, 18m, 0.03m, 0.40m, 0.036m);

        Assert.Equal(185.4m, d.CostoMxn);
        Assert.Equal(185.4m / 0.564m, d.PrecioBrutoMxn);
        Assert.Equal(329m, d.PrecioFinalMxn);
    }

    [Fact]
    public void ElCalculoEsDeterminista() =>
        Assert.Equal(CalculadoraPrecios.Calcular(12.5m, 1m, 17.77m, 0.03m, 0.4m, 0.036m), CalculadoraPrecios.Calcular(12.5m, 1m, 17.77m, 0.03m, 0.4m, 0.036m));

    [Theory]
    [InlineData("-0.01", "0", "18", "0.03", "0.4", "0.036")] // costo negativo
    [InlineData("10", "-1", "18", "0.03", "0.4", "0.036")] // extra negativo
    [InlineData("10", "0", "0", "0.03", "0.4", "0.036")] // tipo de cambio 0
    [InlineData("10", "0", "-18", "0.03", "0.4", "0.036")] // tipo de cambio negativo
    [InlineData("10", "0", "18", "0.03", "0.5", "0.5")] // margen + comisión = 1
    [InlineData("10", "0", "18", "0.03", "0.9", "0.2")] // margen + comisión > 1
    [InlineData("10", "0", "18", "-0.01", "0.4", "0.036")] // colchón negativo
    [InlineData("10", "0", "18", "1.01", "0.4", "0.036")] // colchón > 1
    [InlineData("10", "0", "18", "0.03", "0.91", "0")] // margen > 0.9
    [InlineData("10", "0", "18", "0.03", "0.4", "0.51")] // comisión > 0.5
    public void LosParametrosInvalidosLanzanSinCalcular(string costo, string extra, string tc, string colchon, string margen, string comision) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraPrecios.Calcular(decimal.Parse(costo), decimal.Parse(extra),
            decimal.Parse(tc), decimal.Parse(colchon), decimal.Parse(margen), decimal.Parse(comision)));

    [Fact]
    public void ElCostoCeroDaElPrecioMinimoDe9() =>
        Assert.Equal(9m, CalculadoraPrecios.Calcular(0m, 0m, 18m, 0.03m, 0.4m, 0.036m).PrecioFinalMxn);
}
