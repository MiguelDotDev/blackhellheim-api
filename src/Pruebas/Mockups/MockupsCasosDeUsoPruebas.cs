using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Dominio.Mockups;
using BlackHellHeim.Pruebas.Dobles;
using Xunit;

namespace BlackHellHeim.Pruebas.Mockups;

/// <summary>Historia 021: casos de uso con dobles de Printful y almacén (nunca red real).</summary>
public class MockupsCasosDeUsoPruebas
{
    private static readonly byte[] Png = EscenarioMockups.Png;

    // --- Pedir (US1) ---
    [Fact]
    public async Task PedirCreaLaTareaEnPrintfulYLaGuardaPendiente()
    {
        var e = new EscenarioMockups();

        var dto = await e.PedirOkAsync();

        Assert.Equal("Pendiente", dto.Estado);
        Assert.Equal(e.Diseno.Id, dto.DisenoId);
        Assert.Equal(1, e.Proveedor.Creadas);
        var s = e.Proveedor.UltimaSolicitud!;
        Assert.Equal("https://almacen.test/arte/a.png", s.ArteUrl);
        Assert.Equal(("front", "dtg"), (s.Posicion, s.Tecnica));
        var producto = Assert.Single(s.Productos);
        Assert.Equal(71, producto.ProductoId);
        Assert.Equal([1, 2, 3], producto.VarianteIds);
        Assert.NotNull(await e.Repo.ObtenerPorPrintfulIdAsync("task-1", default));
    }

    [Fact]
    public async Task PedirConUnaTareaPendienteDaConflictoYNoLlamaAPrintful()
    {
        var e = new EscenarioMockups();
        await e.PedirOkAsync();

        var r = await e.Pedir.Handle(new PedirMockupsComando(e.Diseno.Id), default);

        Assert.Equal(TipoError.Conflicto, r.Error!.Tipo);
        Assert.Equal(1, e.Proveedor.Creadas);
    }

    [Fact]
    public async Task PedirDespuesDeTerminarLaAnteriorEsPosible()
    {
        var e = new EscenarioMockups();
        var primera = await e.PedirOkAsync();
        e.Proveedor.Completar("task-1", (1, "front", Png));
        await e.Finalizar.Handle(new FinalizarTareaMockupComando(primera.Id), default);

        var segunda = await e.PedirOkAsync();

        Assert.NotEqual(primera.Id, segunda.Id);
    }

    [Theory]
    [InlineData(false, true, "arte")]
    [InlineData(true, false, "variantes")]
    public async Task PedirSinArteOSinVariantesActivasEsValidacion(bool conArte, bool conVariantes, string campo)
    {
        var e = new EscenarioMockups(conArte, conVariantes);

        var r = await e.Pedir.Handle(new PedirMockupsComando(e.Diseno.Id), default);

        Assert.Equal(TipoError.Validacion, r.Error!.Tipo);
        Assert.Contains(campo, r.Error.Campos!.Keys);
        Assert.Equal(0, e.Proveedor.Creadas);
    }

    [Fact]
    public async Task PedirDeUnDisenoInexistenteEsNoEncontrado()
    {
        var e = new EscenarioMockups();

        var r = await e.Pedir.Handle(new PedirMockupsComando(Guid.NewGuid()), default);

        Assert.Equal(TipoError.NoEncontrado, r.Error!.Tipo);
    }

    [Fact]
    public async Task SiPrintfulFallaAlCrearNoQuedaTareaYElErrorSube()
    {
        var e = new EscenarioMockups();
        e.Proveedor.FalloCrear = () => new ExcepcionCasoDeUso(ErrorUniforme.ProveedorFallo("c"));

        await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => e.Pedir.Handle(new PedirMockupsComando(e.Diseno.Id), default).AsTask());

        Assert.False(await e.Repo.ExistePendienteAsync(e.Diseno.Id, default));
    }

    // --- Finalizar (US2) ---
    private static async Task<(EscenarioMockups E, TareaMockupDto T)> ConTareaAsync()
    {
        var e = new EscenarioMockups();
        return (e, await e.PedirOkAsync());
    }

    [Fact]
    public async Task FinalizarCompletaCopiaUnaImagenPorColorYPosicionAlAlmacen()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Completar("task-1", (1, "front", Png), (2, "front", Png), (3, "front", EscenarioMockups.Jpg));

        var r = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(EstadoTareaMockup.Completada, r.Valor);
        var tarea = (await e.Repo.ObtenerAsync(t.Id, default))!;
        Assert.Equal(EstadoTareaMockup.Completada, tarea.Estado);
        Assert.Equal(["black", "white"], tarea.Imagenes.Select(i => i.Color.ToLowerInvariant()).Order().ToArray());
        Assert.Equal(2, e.Almacen.Blobs.Count);
        Assert.Contains($"mockups/{t.Id:N}/black-front.png", e.Almacen.Blobs.Keys);
        Assert.Contains($"mockups/{t.Id:N}/white-front.jpg", e.Almacen.Blobs.Keys);
        Assert.All(tarea.Imagenes, i => Assert.StartsWith("https://almacen.test/", i.Url));
    }

    [Fact]
    public async Task FinalizarDosVecesNoDuplicaImagenesNiBlobs()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Completar("task-1", (1, "front", Png));
        await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        var consultasAntes = e.Proveedor.Consultas;
        var segunda = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(EstadoTareaMockup.Completada, segunda.Valor);
        Assert.Equal(consultasAntes, e.Proveedor.Consultas); // ya no está pendiente: ni siquiera consulta
        Assert.Single((await e.Repo.ObtenerAsync(t.Id, default))!.Imagenes);
        Assert.Single(e.Almacen.Blobs);
    }

    [Fact]
    public async Task FinalizarConTareaFallidaEnPrintfulGuardaElMotivoRecortado()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Remotas["task-1"] = new("task-1", EstadoMockupProveedor.Fallida, new string('x', 500), []);

        var r = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(EstadoTareaMockup.Fallida, r.Valor);
        var tarea = (await e.Repo.ObtenerAsync(t.Id, default))!;
        Assert.Equal(300, tarea.Motivo!.Length);
        Assert.Empty(e.Almacen.Blobs);
    }

    [Fact]
    public async Task FinalizarUnaTareaTodaviaPendienteLaDejaPendienteHastaElTiempoMaximo()
    {
        var (e, t) = await ConTareaAsync();
        e.Reloj.Ahora = e.Reloj.Ahora.AddMinutes(29);

        var antes = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);
        e.Reloj.Ahora = e.Reloj.Ahora.AddMinutes(2);
        var despues = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(EstadoTareaMockup.Pendiente, antes.Valor);
        Assert.Equal(EstadoTareaMockup.Fallida, despues.Valor);
        Assert.StartsWith("Tiempo agotado", (await e.Repo.ObtenerAsync(t.Id, default))!.Motivo);
    }

    [Fact]
    public async Task UnaTareaQuePrintfulNoConoceTambienSeAgotaAlPasarElTiempoMaximo()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Remotas.Clear();
        e.Reloj.Ahora = e.Reloj.Ahora.AddMinutes(31);

        var r = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(EstadoTareaMockup.Fallida, r.Valor);
    }

    [Fact]
    public async Task UnaDescargaQueFallaDejaLaTareaPendienteYElSiguienteIntentoLaCompleta()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Completar("task-1", (1, "front", Png));
        e.Proveedor.FalloDescarga = () => new ExcepcionCasoDeUso(ErrorUniforme.ProveedorFallo("c"));

        await Assert.ThrowsAsync<ExcepcionCasoDeUso>(() => e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default).AsTask());
        Assert.Equal(EstadoTareaMockup.Pendiente, (await e.Repo.ObtenerAsync(t.Id, default))!.Estado);

        e.Proveedor.FalloDescarga = () => null;
        var r = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);
        Assert.Equal(EstadoTareaMockup.Completada, r.Valor);
    }

    [Fact]
    public async Task UnAlmacenCaidoDejaLaTareaPendiente()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Completar("task-1", (1, "front", Png));
        e.Almacen.Falla = true;

        await Assert.ThrowsAnyAsync<Exception>(() => e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default).AsTask());

        Assert.Equal(EstadoTareaMockup.Pendiente, (await e.Repo.ObtenerAsync(t.Id, default))!.Estado);
    }

    [Fact]
    public async Task SeDescartanImagenesNoPermitidasNoPngJpegOdeVariantesDesconocidas()
    {
        var (e, t) = await ConTareaAsync();
        // 1: válida · 3: descarga rechazada (host/tamaño → null) · 99: variante que no existe localmente
        e.Proveedor.Completar("task-1", (1, "front", Png), (3, "front", null), (99, "front", Png));

        var r = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(EstadoTareaMockup.Completada, r.Valor);
        Assert.Single((await e.Repo.ObtenerAsync(t.Id, default))!.Imagenes);
    }

    [Fact]
    public async Task SiNingunaImagenEsValidaLaTareaQuedaFallida()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Completar("task-1", (1, "front", [1, 2, 3, 4, 5, 6, 7, 8]), (3, "front", null));

        var r = await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(EstadoTareaMockup.Fallida, r.Valor);
        Assert.Contains("imágenes válidas", (await e.Repo.ObtenerAsync(t.Id, default))!.Motivo);
        Assert.Empty(e.Almacen.Blobs);
    }

    [Theory]
    [InlineData(EstadoMockupProveedor.Completada, "Printful no devolvió imágenes válidas. validacion: arte ilegible")]
    [InlineData(EstadoMockupProveedor.Fallida, "Printful reportó un fallo: validacion: arte ilegible")]
    public async Task LosMotivosDePrintfulQuedanEnLaTareaYEnUnWarning(EstadoMockupProveedor estado, string motivo)
    {
        var e = new EscenarioMockups();
        var t = await e.PedirOkAsync();
        e.Proveedor.Remotas["task-1"] = new("task-1", estado, "validacion: arte ilegible",
            [new MockupGenerado(1, "front", new Uri("https://evil.test/a.png?sig=secreto"))]);

        await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);

        Assert.Equal(motivo, (await e.Repo.ObtenerAsync(t.Id, default))!.Motivo);
        var mensaje = Assert.Single(e.Advertencias.Mensajes);
        Assert.Contains("arte ilegible", mensaje);
        Assert.DoesNotContain("secreto", mensaje);
        if (estado == EstadoMockupProveedor.Completada) Assert.Contains("evil.test", mensaje);
    }

    [Fact]
    public async Task FinalizarUnaTareaInexistenteEsNoEncontrado()
    {
        var e = new EscenarioMockups();

        var r = await e.Finalizar.Handle(new FinalizarTareaMockupComando(Guid.NewGuid()), default);

        Assert.Equal(TipoError.NoEncontrado, r.Error!.Tipo);
    }

    [Fact]
    public async Task DiezFinalizacionesConcurrentesDejanUnSoloConjuntoDeImagenes()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Completar("task-1", (1, "front", Png), (3, "front", Png));

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            Task.Run(async () => await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default))));

        Assert.Equal(2, (await e.Repo.ObtenerAsync(t.Id, default))!.Imagenes.Count);
    }

    // --- Listar (US4) ---
    [Fact]
    public async Task ListarDevuelveLasTareasMasRecientesPrimeroConSusImagenes()
    {
        var (e, t) = await ConTareaAsync();
        e.Proveedor.Completar("task-1", (1, "front", Png));
        await e.Finalizar.Handle(new FinalizarTareaMockupComando(t.Id), default);
        e.Reloj.Ahora = e.Reloj.Ahora.AddMinutes(1);
        var segunda = await e.PedirOkAsync();

        var r = await new ManejadorListarMockups(e.Repo, e.Contexto).Handle(new ListarMockupsQuery(e.Diseno.Id), default);

        Assert.Equal([segunda.Id, t.Id], r.Valor!.Select(x => x.Id).ToArray());
        Assert.Single(r.Valor![1].Imagenes);
    }

    [Fact]
    public async Task ListarDeUnDisenoInexistenteEsNoEncontrado()
    {
        var e = new EscenarioMockups();

        var r = await new ManejadorListarMockups(e.Repo, e.Contexto).Handle(new ListarMockupsQuery(Guid.NewGuid()), default);

        Assert.Equal(TipoError.NoEncontrado, r.Error!.Tipo);
    }

    // --- Utilidades y opciones ---
    [Theory]
    [InlineData("""{"data":{"mockup_task":{"id":"abc"}}}""", "abc")]
    [InlineData("""{"data":{"task":{"id":77}}}""", "77")]
    [InlineData("""{"data":{"id":"z"}}""", "z")]
    [InlineData("""{"data":{}}""", null)]
    [InlineData("no json", null)]
    public void ElIdDeTareaSeExtraeDeLasFormasSupuestasDelEvento(string cuerpo, string? esperado) =>
        Assert.Equal(esperado, ManejadorMockupTaskFinished.ExtraerIdTarea(cuerpo));

    [Theory]
    [InlineData("Black", "black")]
    [InlineData("Navy / White!", "navy-white")]
    [InlineData("¡¡", "x")]
    public void ElSlugEsSeguroParaNombresDeBlob(string color, string esperado) =>
        Assert.Equal(esperado, ManejadorFinalizarTareaMockup.Slug(color));

    [Fact]
    public void LasOpcionesPorOmisionSonValidas() => new OpcionesMockups().Validar();

    [Theory]
    [InlineData(0, 120, 30, "dtg", "IntervaloPollingSegundos")]
    [InlineData(30, -1, 30, "dtg", "EsperaMinimaSegundos")]
    [InlineData(30, 120, 0, "dtg", "TiempoMaximoMinutos")]
    [InlineData(30, 1800, 30, "dtg", "menor que TiempoMaximoMinutos")]
    [InlineData(30, 120, 30, "otra", "Tecnica")]
    public void LasOpcionesInvalidasFallanAlArrancar(int intervalo, int espera, int maximo, string tecnica, string mensaje)
    {
        var o = new OpcionesMockups { IntervaloPollingSegundos = intervalo, EsperaMinimaSegundos = espera, TiempoMaximoMinutos = maximo, Tecnica = tecnica };

        Assert.Contains(mensaje, Assert.Throws<InvalidOperationException>(o.Validar).Message);
    }

    [Fact]
    public void SinHostsPermitidosLasOpcionesSonInvalidas() =>
        Assert.Throws<InvalidOperationException>(new OpcionesMockups { HostsDescargaPermitidos = [] }.Validar);
}
