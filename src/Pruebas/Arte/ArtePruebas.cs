using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Infraestructura.Almacen;
using Xunit;

namespace BlackHellHeim.Pruebas.Arte;

public class ValidadorArtePruebas
{
    private static (ErrorArte? Error, InfoPng? Info) Validar(byte[] bytes, OpcionesArte? o = null)
    {
        using var flujo = new MemoryStream(bytes);
        return new ValidadorArte(o ?? OpcionesPequenas.Crear(), new MuestreadorTransparenciaSkia()).Validar(flujo);
    }

    [Fact]
    public void UnPngTransparenteConResolucionSuficienteEsValido()
    {
        var (error, info) = Validar(GeneradorPng.Crear(40, 50));
        Assert.Null(error);
        Assert.Equal(new InfoPng(40, 50, true), info);
    }

    [Fact]
    public void ElLimiteExactoDeResolucionSeAcepta() => Assert.Null(Validar(GeneradorPng.Crear(40, 50)).Error);

    [Theory]
    [InlineData(39, 50)]
    [InlineData(40, 49)]
    public void UnaResolucionPorDebajoDelMinimoSeRechaza(int ancho, int alto) =>
        Assert.Equal(ValidadorArte.ResolucionInsuficiente, Validar(GeneradorPng.Crear(ancho, alto)).Error!.Codigo);

    [Theory]
    [InlineData(1799, 2400, false)]
    [InlineData(1800, 2399, false)]
    [InlineData(1800, 2400, true)]
    public void ElMinimoPorDefectoEs1800x2400(int ancho, int alto, bool valido)
    {
        var o = new OpcionesArte();
        Assert.Equal((1800, 2400), (o.MinAncho, o.MinAlto));
        var error = Validar(GeneradorPng.Crear(ancho, alto), o).Error;
        Assert.Equal(valido ? null : ValidadorArte.ResolucionInsuficiente, error?.Codigo);
        if (!valido) Assert.Contains("1800×2400", error!.Mensaje);
    }

    [Fact]
    public void UnPngOpacoSeRechazaPorTransparencia() =>
        Assert.Equal(ValidadorArte.SinTransparencia, Validar(GeneradorPng.Crear(40, 50, transparente: false)).Error!.Codigo);

    [Fact]
    public void UnPngConEsquinasOpacasSeRechaza()
    {
        // Rectángulo opaco que cubre casi todo: las esquinas quedan cubiertas.
        Assert.Equal(ValidadorArte.SinTransparencia, Validar(GeneradorPng.Crear(40, 50, true, 1.0)).Error!.Codigo);
    }

    [Fact]
    public void UnPngConMenosDelCincoPorCientoTransparenteSeRechaza() =>
        Assert.Equal(ValidadorArte.SinTransparencia, Validar(GeneradorPng.Crear(100, 100, true, 0.99)).Error!.Codigo);

    [Fact]
    public void UnArchivoQueNoEsPngSeRechazaPorFirma() =>
        Assert.Equal(ValidadorArte.Png, Validar([0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F']).Error!.Codigo);

    [Fact]
    public void UnArchivoVacioEsIlegible() => Assert.Equal(ValidadorArte.Ilegible, Validar([]).Error!.Codigo);

    [Fact]
    public void UnPngTruncadoEsIlegible()
    {
        var png = GeneradorPng.Crear(40, 50);
        Assert.Equal(ValidadorArte.Ilegible, Validar(png[..(png.Length / 2)]).Error!.Codigo);
    }

    [Fact]
    public void UnPngAnimadoSeRechaza() =>
        Assert.Equal(ValidadorArte.Png, Validar(GeneradorPng.ConAnimacion(GeneradorPng.Crear(40, 50))).Error!.Codigo);

    [Fact]
    public void UnPngConMasMegapixelesDelMaximoSeRechazaSinDecodificar()
    {
        var o = new OpcionesArte { MinAncho = 10, MinAlto = 10, MaxMegapixeles = 1 };
        Assert.Equal(ValidadorArte.DemasiadoGrande, Validar(GeneradorPng.Crear(1100, 1000), o).Error!.Codigo);
    }

    [Fact]
    public void LasOpcionesInvalidasFallanAlValidar()
    {
        Assert.Throws<InvalidOperationException>(() => new OpcionesArte { MinAncho = 0 }.Validar());
        Assert.Throws<InvalidOperationException>(() => new OpcionesArte { MaxMegapixeles = 1 }.Validar()); // 4500×5400 no cabe
        Assert.Throws<InvalidOperationException>(() => new OpcionesArte { PorcentajeMinimoTransparente = 101 }.Validar());
        new OpcionesArte().Validar();
    }
}

public class SubirArtePruebas
{
    private readonly AlmacenEnMemoria _almacen = new();
    private readonly RepositorioArteEnMemoria _repo = new();

    private ManejadorSubirArte Crear(OpcionesArte? o = null)
    {
        o ??= OpcionesPequenas.Crear();
        return new ManejadorSubirArte(o, new ValidadorArte(o, new MuestreadorTransparenciaSkia()), _almacen, _repo,
            TimeProvider.System, new ContextoArteFalso());
    }

    private static SubirArteComando Comando(byte[] bytes, string nombre = "a.png", string tipo = "image/png") =>
        new(new MemoryStream(bytes), nombre, tipo);

    [Fact]
    public async Task UnArteValidoSeGuardaYSeRegistra()
    {
        var r = await Crear().Handle(Comando(GeneradorPng.Crear(40, 50)), default);

        Assert.True(r.EsExito);
        Assert.False(r.Valor!.Reutilizado);
        Assert.Equal((40, 50), (r.Valor.Ancho, r.Valor.Alto));
        Assert.Single(_almacen.Blobs);
        Assert.StartsWith("https://almacen.test/arte/", r.Valor.Url);
        Assert.Single(_repo.Registros);
    }

    [Fact]
    public async Task ElMismoContenidoSeReutilizaSinDuplicarElBlob()
    {
        var png = GeneradorPng.Crear(40, 50);
        var primera = await Crear().Handle(Comando(png), default);
        var segunda = await Crear().Handle(Comando(png, "otro.PNG"), default);

        Assert.True(segunda.Valor!.Reutilizado);
        Assert.Equal(primera.Valor!.Id, segunda.Valor.Id);
        Assert.Single(_almacen.Blobs);
    }

    [Fact]
    public async Task ElNombreOriginalNuncaFormaLaRutaDelBlob()
    {
        await Crear().Handle(Comando(GeneradorPng.Crear(40, 50), "../../etc/pasa.png"), default);
        Assert.Matches("^[0-9a-f]{32}\\.png$", _almacen.Blobs.Keys.Single());
    }

    [Theory]
    [InlineData("a.jpg", "image/png")]
    [InlineData("a.png", "image/jpeg")]
    [InlineData(null, "image/png")]
    public async Task LaExtensionOElTipoIncorrectoDa415(string? nombre, string tipo)
    {
        var r = await Crear().Handle(new SubirArteComando(new MemoryStream([1]), nombre, tipo), default);
        Assert.Equal(TipoError.ArchivoNoSoportado, r.Error!.Tipo);
        Assert.Equal("arte-no-png", r.Error.Codigo);
    }

    [Fact]
    public async Task UnArchivoQueSuperaElMaximoDa413SinLeerTodo()
    {
        var o = new OpcionesArte(); // 200 MB por omisión
        var flujo = new FlujoGenerado(201L * 1024 * 1024);
        var antes = GC.GetTotalMemory(true);

        var r = await Crear(o).Handle(new SubirArteComando(flujo, "a.png", "image/png"), default);

        Assert.Equal(TipoError.ArchivoDemasiadoGrande, r.Error!.Tipo);
        Assert.Equal("arte-demasiado-grande", r.Error.Codigo);
        Assert.True(flujo.Leidos <= o.MaxBytes + 81920, "se cortó en cuanto superó el tope");
        Assert.True(GC.GetTotalMemory(false) - antes < 50L * 1024 * 1024, "no se cargó en memoria");
        Assert.Empty(_almacen.Blobs);
    }

    [Fact]
    public async Task UnRechazoDelValidadorNoGuardaNada()
    {
        var r = await Crear().Handle(Comando(GeneradorPng.Crear(40, 50, transparente: false)), default);

        Assert.Equal("arte-sin-transparencia", r.Error!.Codigo);
        Assert.Equal(TipoError.Validacion, r.Error.Tipo);
        Assert.Empty(_almacen.Blobs);
        Assert.Empty(_repo.Registros);
    }

    [Fact]
    public async Task SiFallaElRegistroSeEliminaElBlob()
    {
        _repo.FallaAlRegistrar = true;
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Crear().Handle(Comando(GeneradorPng.Crear(40, 50)), default));
        Assert.Empty(_almacen.Blobs);
    }

    [Fact]
    public async Task SiElAlmacenFallaDa503()
    {
        _almacen.Falla = true;
        var r = await Crear().Handle(Comando(GeneradorPng.Crear(40, 50)), default);
        Assert.Equal(TipoError.NoDisponible, r.Error!.Tipo);
        Assert.Equal("almacen_no_disponible", r.Error.Codigo);
        Assert.Empty(_repo.Registros);
    }

    [Fact]
    public async Task LaCancelacionNoDejaBlobsNiRegistros()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Crear().Handle(Comando(GeneradorPng.Crear(40, 50)), cts.Token));
        Assert.Empty(_almacen.Blobs);
        Assert.Empty(_repo.Registros);
    }
}
