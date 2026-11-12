using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Disenos;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Pruebas.Aplicacion.Autenticacion;
using BlackHellHeim.Pruebas.Dominio;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Disenos;

internal sealed class RepositorioDisenosFalso : IRepositorioDisenos
{
    public List<Diseno> Disenos { get; } = [];
    public Dictionary<Guid, ArchivoArte> Artes { get; } = [];
    public ResultadoGuardado Resultado { get; set; } = ResultadoGuardado.Correcto;
    public byte[]? UltimaVersionEsperada { get; private set; }

    public Task<Diseno?> ObtenerAsync(Guid id, CancellationToken c) => Task.FromResult(Disenos.FirstOrDefault(d => d.Id == id));
    public Task<bool> ExisteNombreOSlugAsync(string n, string s, Guid? excepto, CancellationToken c) =>
        Task.FromResult(Disenos.Any(d => (d.Nombre == n || d.Slug == s) && d.Id != excepto));
    public Task<ArchivoArte?> ObtenerArteAsync(Guid id, CancellationToken c) => Task.FromResult(Artes.GetValueOrDefault(id));
    public Task<IReadOnlyList<Variante>> ObtenerOCrearVariantesAsync(IReadOnlyList<VarianteDisenoDto> v, CancellationToken c)
    {
        var productos = new Dictionary<int, ProductoBase>();
        return Task.FromResult<IReadOnlyList<Variante>>(v.Select(x => Variante.Crear(
            productos.TryGetValue(x.PrintfulProductoId, out var p) ? p : productos[x.PrintfulProductoId] = ProductoBase.Crear(x.PrintfulProductoId, x.NombreProducto ?? "P", "T"),
            x.PrintfulVarianteId, x.Color, x.CodigoColor, x.Talla, x.CostoBaseUsd)).ToList());
    }
    public void Agregar(Diseno d)
    {
        // La versión (rowversion) la asigna la base; el doble simula una ya guardada.
        typeof(Diseno).GetProperty(nameof(Diseno.Version))!.SetValue(d, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        Disenos.Add(d);
    }
    public Task<ResultadoGuardado> GuardarAsync(Diseno d, byte[]? v, CancellationToken c)
    {
        UltimaVersionEsperada = v;
        return Task.FromResult(Resultado);
    }
    public Task<(IReadOnlyList<Diseno>, int)> ListarAsync(EstadoDiseno? e, int p, int t, CancellationToken c)
    {
        var f = Disenos.Where(d => e == null || d.Estado == e).ToList();
        return Task.FromResult<(IReadOnlyList<Diseno>, int)>((f.Skip((p - 1) * t).Take(t).ToList(), f.Count));
    }
}

internal sealed class TerminosFalsos(params string[] t) : ITerminosBloqueados
{
    public IReadOnlyList<string> Terminos { get; } = t;
}

public class DisenosPruebas
{
    private readonly RepositorioDisenosFalso _repo = new();
    private readonly TimeProvider _reloj = TimeProvider.System;
    private readonly ContextoFalso _ctx = new();
    private static VarianteDisenoDto V(int id = 1, int prod = 71) => new(id, prod, "Playera", "Black", "#000", "S", 12.5m);

    private ManejadorCrearDiseno Crear(params string[] terminos) => new(_repo, new TerminosFalsos(terminos), _reloj, _ctx);
    private ManejadorEditarDiseno Editar() => new(_repo, new TerminosFalsos(), _reloj, _ctx);

    // ----- Validación -----
    [Theory]
    [InlineData("ab", 1)]
    [InlineData("Diseño bueno", 0)]
    [InlineData(null, 1)]
    public void ElNombreSeValida(string? nombre, int errores) =>
        Assert.Equal(errores, new ValidadorCrearDiseno().Validar(new CrearDisenoComando(new DisenoSolicitud { Nombre = nombre })).Count);

    [Fact]
    public void ElEstadoYLasVariantesSeValidan()
    {
        var v = new ValidadorCrearDiseno();

        Assert.Single(v.Validar(new(new DisenoSolicitud { Nombre = "Diseño", Estado = "Raro" })));
        Assert.Single(v.Validar(new(new DisenoSolicitud { Nombre = "Diseño", Variantes = [V(1, 71), V(2, 99)] })));
        Assert.Single(v.Validar(new(new DisenoSolicitud { Nombre = "Diseño", Variantes = [V(0)] })));
    }

    [Fact]
    public void EditarExigeIfMatch()
    {
        var v = new ValidadorEditarDiseno();
        var s = new DisenoSolicitud { Nombre = "Diseño" };

        Assert.Contains(v.Validar(new(Guid.NewGuid(), null, s)), e => e.Campo == "ifMatch");
        Assert.Contains(v.Validar(new(Guid.NewGuid(), "no-es-base64!", s)), e => e.Campo == "ifMatch");
        Assert.Empty(v.Validar(new(Guid.NewGuid(), "AAAA", s)));
    }

    [Theory]
    [InlineData(0, 20, null, 1)]
    [InlineData(1, 101, null, 1)]
    [InlineData(1, 20, "Otro", 1)]
    [InlineData(1, 20, "Publicado", 0)]
    public void ElListadoSeValida(int pagina, int tamano, string? estado, int errores) =>
        Assert.Equal(errores, new ValidadorListarDisenos().Validar(new ListarDisenosQuery(estado, pagina, tamano)).Count);

    // ----- Crear -----
    [Fact]
    public async Task CrearGuardaUnBorradorConVariantes()
    {
        var r = await Crear().Handle(new(new DisenoSolicitud { Nombre = "Calavera", Variantes = [V(1), V(2)] }), default);

        Assert.True(r.EsExito);
        Assert.Equal("Borrador", r.Valor!.Estado);
        Assert.Equal("calavera", r.Valor.Slug);
        Assert.Equal(2, r.Valor.Variantes.Count);
        Assert.Single(_repo.Disenos);
    }

    [Fact]
    public async Task CrearConNombreRepetidoDa409()
    {
        await Crear().Handle(new(new DisenoSolicitud { Nombre = "Calavera" }), default);

        var r = await Crear().Handle(new(new DisenoSolicitud { Nombre = "CALAVERA" }), default);

        Assert.Equal(TipoError.Conflicto, r.Error!.Tipo);
    }

    [Fact]
    public async Task CrearConArteInexistenteDa400()
    {
        var r = await Crear().Handle(new(new DisenoSolicitud { Nombre = "Calavera", ArchivoArteId = Guid.NewGuid() }), default);

        Assert.Equal(TipoError.Validacion, r.Error!.Tipo);
    }

    [Fact]
    public async Task CrearPublicadoSinArteDa409YNoGuarda()
    {
        var r = await Crear().Handle(new(new DisenoSolicitud { Nombre = "Calavera", Estado = "Publicado", Variantes = [V()] }), default);

        Assert.Equal(TipoError.Conflicto, r.Error!.Tipo);
        Assert.Empty(_repo.Disenos);
    }

    [Fact]
    public async Task CrearPublicadoCompletoFunciona()
    {
        var arte = DisenoPruebas.Arte();
        _repo.Artes[arte.Id] = arte;

        var r = await Crear().Handle(new(new DisenoSolicitud
        {
            Nombre = "Calavera", Estado = "Publicado", ArchivoArteId = arte.Id, Variantes = [V(1), V(2)]
        }), default);

        Assert.Equal("Publicado", r.Valor!.Estado);
        Assert.Equal("http://x/arte.png", r.Valor.ArchivoArteUrl);
    }

    [Fact]
    public async Task UnTerminoBloqueadoGeneraAvisoPeroNoImpide()
    {
        var r = await Crear("metallica").Handle(new(new DisenoSolicitud { Nombre = "Estilo Metallica" }), default);

        Assert.True(r.EsExito);
        Assert.Single(r.Valor!.Avisos);
        Assert.Contains("metallica", r.Valor.Avisos[0], StringComparison.OrdinalIgnoreCase);
    }

    // ----- Editar -----
    private async Task<DisenoDto> Existente()
    {
        var r = await Crear().Handle(new(new DisenoSolicitud { Nombre = "Calavera" }), default);
        return r.Valor!;
    }

    [Fact]
    public async Task EditarCambiaDatosYReemplazaVariantes()
    {
        var d = await Existente();

        var r = await Editar().Handle(new(d.Id, d.Version, new DisenoSolicitud { Nombre = "Calavera 2", Variantes = [V(5)] }), default);

        Assert.Equal("calavera-2", r.Valor!.Slug);
        Assert.Equal(5, r.Valor.Variantes.Single().PrintfulVarianteId);
        Assert.NotNull(_repo.UltimaVersionEsperada);
    }

    [Fact]
    public async Task EditarConVersionVencidaDa412()
    {
        var d = await Existente();

        var r = await Editar().Handle(new(d.Id, Convert.ToBase64String([9, 9, 9]), new DisenoSolicitud { Nombre = "Otro" }), default);

        Assert.Equal(TipoError.PrecondicionFallida, r.Error!.Tipo);
    }

    [Fact]
    public async Task UnaCarreraAlGuardarDa412()
    {
        var d = await Existente();
        _repo.Resultado = ResultadoGuardado.Concurrencia;

        var r = await Editar().Handle(new(d.Id, d.Version, new DisenoSolicitud { Nombre = "Otro" }), default);

        Assert.Equal(TipoError.PrecondicionFallida, r.Error!.Tipo);
    }

    [Fact]
    public async Task EditarInexistenteDa404YNombreDeOtroDa409()
    {
        var a = await Existente();
        await Crear().Handle(new(new DisenoSolicitud { Nombre = "Otro diseño" }), default);

        var noExiste = await Editar().Handle(new(Guid.NewGuid(), "AAAA", new DisenoSolicitud { Nombre = "X nombre" }), default);
        var choque = await Editar().Handle(new(a.Id, a.Version, new DisenoSolicitud { Nombre = "Otro diseño" }), default);

        Assert.Equal(TipoError.NoEncontrado, noExiste.Error!.Tipo);
        Assert.Equal(TipoError.Conflicto, choque.Error!.Tipo);
    }

    // ----- Consultas -----
    [Fact]
    public async Task ObtenerYListarDevuelvenLosDisenos()
    {
        var d = await Existente();
        var terminos = new TerminosFalsos();

        var uno = await new ManejadorObtenerDiseno(_repo, terminos, _ctx).Handle(new(d.Id), default);
        var falta = await new ManejadorObtenerDiseno(_repo, terminos, _ctx).Handle(new(Guid.NewGuid()), default);
        var lista = await new ManejadorListarDisenos(_repo, terminos).Handle(new(null, 1, 20), default);
        var borradores = await new ManejadorListarDisenos(_repo, terminos).Handle(new("Publicado", 1, 20), default);

        Assert.True(uno.EsExito);
        Assert.Equal(TipoError.NoEncontrado, falta.Error!.Tipo);
        Assert.Equal(1, lista.Valor!.Total);
        Assert.Empty(borradores.Valor!.Elementos);
    }
}
