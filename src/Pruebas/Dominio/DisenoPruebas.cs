using BlackHellHeim.Dominio.Disenos;
using Xunit;

namespace BlackHellHeim.Pruebas.Dominio;

public class DisenoPruebas
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    internal static Variante Var(int id, int producto = 71, string talla = "S", decimal costo = 12.5m) =>
        Variante.Crear(ProductoBase.Crear(producto, "Playera", "T-SHIRT"), id, "Black", "#000000", talla, costo);

    internal static ArchivoArte Arte() =>
        ArchivoArte.Crear("http://x/arte.png", "a.png", "image/png", 10, 4500, 5400, new string('a', 64), Ahora);

    [Fact]
    public void UnDisenoNuevoEsBorradorConSlugSinAcentos()
    {
        var d = Diseno.Crear("  Calavera Ñandú Épica! ", " descripción ", Ahora);

        Assert.Equal(EstadoDiseno.Borrador, d.Estado);
        Assert.Equal("Calavera Ñandú Épica!", d.Nombre);
        Assert.Equal("calavera-nandu-epica", d.Slug);
        Assert.Equal("descripción", d.Descripcion);
        Assert.Empty(d.Variantes);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("   ")]
    [InlineData("¡¡¡")]
    public void UnNombreInvalidoSeRechaza(string nombre) =>
        Assert.Throws<ArgumentException>(() => Diseno.Crear(nombre, null, Ahora));

    [Fact]
    public void LosLimitesDeNombreYDescripcionSonExactos()
    {
        Assert.NotNull(Diseno.Crear(new string('a', 80), new string('d', 2000), Ahora));
        Assert.Throws<ArgumentException>(() => Diseno.Crear(new string('a', 81), null, Ahora));
        Assert.Throws<ArgumentException>(() => Diseno.Crear("Nombre", new string('d', 2001), Ahora));
    }

    [Fact]
    public void PublicarSinArteOSinVariantesActivasEsConflicto()
    {
        var d = Diseno.Crear("Diseño uno", null, Ahora);

        Assert.Equal("publicar_sin_arte", Assert.Throws<ReglaDeNegocioException>(() => d.CambiarEstado(EstadoDiseno.Publicado, Ahora)).Codigo);
        d.AsignarArte(Arte(), Ahora);
        Assert.Equal("publicado_sin_variantes", Assert.Throws<ReglaDeNegocioException>(() => d.CambiarEstado(EstadoDiseno.Publicado, Ahora)).Codigo);
    }

    [Fact]
    public void ConArteYVariantesSePublicaSeArchivaYSeRestaura()
    {
        var d = Diseno.Crear("Diseño uno", null, Ahora);
        d.AsignarArte(Arte(), Ahora);
        d.ReemplazarVariantes([Var(1), Var(2, talla: "M")], Ahora);

        d.CambiarEstado(EstadoDiseno.Publicado, Ahora);
        d.CambiarEstado(EstadoDiseno.Archivado, Ahora);
        d.CambiarEstado(EstadoDiseno.Publicado, Ahora);

        Assert.Equal(EstadoDiseno.Publicado, d.Estado);
        Assert.Equal(2, d.Variantes.Count);
    }

    [Fact]
    public void NoSePuedeVolverABorradorDesdePublicado()
    {
        var d = Diseno.Crear("Diseño uno", null, Ahora);
        d.AsignarArte(Arte(), Ahora);
        d.ReemplazarVariantes([Var(1)], Ahora);
        d.CambiarEstado(EstadoDiseno.Publicado, Ahora);

        Assert.Equal("transicion_invalida", Assert.Throws<ReglaDeNegocioException>(() => d.CambiarEstado(EstadoDiseno.Borrador, Ahora)).Codigo);
    }

    [Fact]
    public void LasVariantesDeDosProductosBaseDistintosSeRechazan()
    {
        var d = Diseno.Crear("Diseño uno", null, Ahora);

        Assert.Equal("variantes_de_varios_productos",
            Assert.Throws<ReglaDeNegocioException>(() => d.ReemplazarVariantes([Var(1, 71), Var(2, 99)], Ahora)).Codigo);
    }

    [Fact]
    public void UnDisenoPublicadoNoPuedeQuedarseSinVariantes()
    {
        var d = Diseno.Crear("Diseño uno", null, Ahora);
        d.AsignarArte(Arte(), Ahora);
        d.ReemplazarVariantes([Var(1)], Ahora);
        d.CambiarEstado(EstadoDiseno.Publicado, Ahora);

        Assert.Equal("publicado_sin_variantes", Assert.Throws<ReglaDeNegocioException>(() => d.ReemplazarVariantes([], Ahora)).Codigo);
    }

    [Fact]
    public void LasVariantesRepetidasSeDeduplicanPorIdDePrintful()
    {
        var d = Diseno.Crear("Diseño uno", null, Ahora);
        var producto = ProductoBase.Crear(71, "P", "T");

        d.ReemplazarVariantes([Variante.Crear(producto, 1, "a", "b", "S", 1), Variante.Crear(producto, 1, "a", "b", "S", 1)], Ahora);

        Assert.Single(d.Variantes);
    }

    [Fact]
    public void ElCostoBaseNoPuedeSerNegativo() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Var(1, costo: -1));
}
