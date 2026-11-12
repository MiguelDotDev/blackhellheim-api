using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Dominio.Autenticacion;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Autenticacion;

internal sealed class ContextoFalso : IContextoEjecucion
{
    public string IdCorrelacion { get; set; } = "corr-1";
}

internal sealed class IdentidadFalsa : IServicioIdentidad
{
    public Dictionary<string, (UsuarioIdentidad Usuario, string Contrasena)> Usuarios { get; } = new();
    public HashSet<string> Bloqueados { get; } = [];

    public Task<(ResultadoCreacion, UsuarioIdentidad?)> CrearClienteAsync(string correo, string contrasena, CancellationToken c)
    {
        if (Usuarios.ContainsKey(correo)) return Task.FromResult<(ResultadoCreacion, UsuarioIdentidad?)>((ResultadoCreacion.CorreoDuplicado, null));
        var u = new UsuarioIdentidad(Guid.NewGuid(), correo, Roles.Cliente);
        Usuarios[correo] = (u, contrasena);
        return Task.FromResult<(ResultadoCreacion, UsuarioIdentidad?)>((ResultadoCreacion.Creado, u));
    }

    public Task<(ResultadoCredenciales, UsuarioIdentidad?)> VerificarCredencialesAsync(string correo, string contrasena, CancellationToken c)
    {
        if (Bloqueados.Contains(correo)) return Task.FromResult<(ResultadoCredenciales, UsuarioIdentidad?)>((ResultadoCredenciales.Bloqueado, null));
        return Task.FromResult<(ResultadoCredenciales, UsuarioIdentidad?)>(
            Usuarios.TryGetValue(correo, out var e) && e.Contrasena == contrasena
                ? (ResultadoCredenciales.Correctas, e.Usuario)
                : (ResultadoCredenciales.Incorrectas, null));
    }

    public Task<UsuarioIdentidad?> ObtenerPorIdAsync(Guid id, CancellationToken c) =>
        Task.FromResult(Usuarios.Values.Select(v => v.Usuario).FirstOrDefault(u => u.Id == id));
}

internal sealed class EmisorFalso : IEmisorTokens
{
    public TimeSpan VidaAcceso => TimeSpan.FromMinutes(15);
    public string CrearTokenAcceso(UsuarioIdentidad usuario) => "acceso-" + usuario.Id;
}

internal sealed class RepositorioFalso : IRepositorioTokensRefresco
{
    public List<TokenRefresco> Tokens { get; } = [];
    public Task AgregarAsync(TokenRefresco t, CancellationToken c) { Tokens.Add(t); return Task.CompletedTask; }
    public Task<TokenRefresco?> ObtenerPorHashAsync(string h, CancellationToken c) => Task.FromResult(Tokens.FirstOrDefault(t => t.HashToken == h));
    public Task<bool> MarcarUsadoAsync(Guid id, DateTimeOffset ahora, CancellationToken c)
    {
        var t = Tokens.First(x => x.Id == id);
        if (!t.EstaActivo(ahora)) return Task.FromResult(false);
        typeof(TokenRefresco).GetProperty(nameof(TokenRefresco.UsadoUtc))!.SetValue(t, ahora);
        return Task.FromResult(true);
    }
    public Task RevocarFamiliaAsync(Guid familia, DateTimeOffset ahora, CancellationToken c)
    {
        foreach (var t in Tokens.Where(x => x.FamiliaId == familia && x.RevocadoUtc is null))
            typeof(TokenRefresco).GetProperty(nameof(TokenRefresco.RevocadoUtc))!.SetValue(t, ahora);
        return Task.CompletedTask;
    }
    public Task<int> PurgarCaducadosAsync(DateTimeOffset limite, CancellationToken c)
    {
        var n = Tokens.RemoveAll(t => t.ExpiraUtc < limite);
        return Task.FromResult(n);
    }
}

internal sealed class RelojFalso(DateTimeOffset inicio) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = inicio;
    public override DateTimeOffset GetUtcNow() => Ahora;
}

public class AutenticacionPruebas
{
    private readonly IdentidadFalsa _identidad = new();
    private readonly RepositorioFalso _repositorio = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero));
    private readonly ContextoFalso _contexto = new();

    private EmisorSesion Sesiones => new(new EmisorFalso(), _repositorio, _reloj);

    // ----- Validadores -----
    [Theory]
    [InlineData("a@b.com", "abcdefghi1", 0)]
    [InlineData("a@b.com", "corta1", 1)]
    [InlineData("a@b.com", "soloLetrasLargas", 1)]
    [InlineData("a@b.com", "1234567890123", 1)]
    [InlineData("no-es-correo", "abcdefghi1", 1)]
    [InlineData("", "", 2)]
    public void ElRegistroValidaCorreoYPolitica(string correo, string contrasena, int errores) =>
        Assert.Equal(errores, new ValidadorRegistrar().Validar(new RegistrarComando(correo, contrasena)).Count);

    [Fact]
    public void LaContrasenaNoPuedeSuperar128Caracteres() =>
        Assert.Contains(new ValidadorRegistrar().Validar(new RegistrarComando("a@b.com", new string('a', 129) + "1")),
            e => e.Campo == "contrasena");

    [Fact]
    public void ElInicioDeSesionNoAplicaLaPoliticaSoloExigeDatos()
    {
        Assert.Empty(new ValidadorIniciarSesion().Validar(new IniciarSesionComando("a@b.com", "x")));
        Assert.NotEmpty(new ValidadorIniciarSesion().Validar(new IniciarSesionComando("a@b.com", "")));
    }

    // ----- Registro -----
    [Fact]
    public async Task RegistrarCreaClienteYEmiteSesionConRefreshHasheado()
    {
        var manejador = new ManejadorRegistrar(_identidad, Sesiones, _contexto);

        var resultado = await manejador.Handle(new RegistrarComando("  Ana@Correo.com ", "contrasena123"), default);

        Assert.True(resultado.EsExito);
        Assert.Equal(Roles.Cliente, resultado.Valor!.Sesion.Rol);
        Assert.Equal(900, resultado.Valor.Sesion.ExpiraEnSegundos);
        var guardado = Assert.Single(_repositorio.Tokens);
        Assert.NotEqual(resultado.Valor.TokenRefresco, guardado.HashToken);
        Assert.Equal(EmisorSesion.CalcularHash(resultado.Valor.TokenRefresco), guardado.HashToken);
        Assert.True(_identidad.Usuarios.ContainsKey("ana@correo.com"));
    }

    [Fact]
    public async Task RegistrarConCorreoDuplicadoDaConflicto()
    {
        var manejador = new ManejadorRegistrar(_identidad, Sesiones, _contexto);
        await manejador.Handle(new RegistrarComando("a@b.com", "contrasena123"), default);

        var segundo = await manejador.Handle(new RegistrarComando("A@B.com", "contrasena123"), default);

        Assert.Equal(TipoError.Conflicto, segundo.Error!.Tipo);
    }

    // ----- Inicio de sesión -----
    [Fact]
    public async Task IniciarSesionConCredencialesCorrectasEmiteSesion()
    {
        await new ManejadorRegistrar(_identidad, Sesiones, _contexto).Handle(new RegistrarComando("a@b.com", "contrasena123"), default);

        var resultado = await new ManejadorIniciarSesion(_identidad, Sesiones, _contexto)
            .Handle(new IniciarSesionComando("A@b.com", "contrasena123"), default);

        Assert.True(resultado.EsExito);
    }

    [Theory]
    [InlineData("a@b.com", "equivocada")]
    [InlineData("otro@b.com", "contrasena123")]
    public async Task CredencialesIncorrectasDanElMismoErrorSinDistinguirElMotivo(string correo, string contrasena)
    {
        await new ManejadorRegistrar(_identidad, Sesiones, _contexto).Handle(new RegistrarComando("a@b.com", "contrasena123"), default);

        var resultado = await new ManejadorIniciarSesion(_identidad, Sesiones, _contexto)
            .Handle(new IniciarSesionComando(correo, contrasena), default);

        Assert.Equal(TipoError.NoAutenticado, resultado.Error!.Tipo);
        Assert.Equal("Credenciales inválidas o sesión no válida.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task UnaCuentaBloqueadaDaError423()
    {
        _identidad.Bloqueados.Add("a@b.com");

        var resultado = await new ManejadorIniciarSesion(_identidad, Sesiones, _contexto)
            .Handle(new IniciarSesionComando("a@b.com", "x"), default);

        Assert.Equal(TipoError.Bloqueado, resultado.Error!.Tipo);
    }

    // ----- Renovación y reuso -----
    private async Task<SesionEmitida> RegistrarAsync() =>
        (await new ManejadorRegistrar(_identidad, Sesiones, _contexto).Handle(new RegistrarComando("a@b.com", "contrasena123"), default)).Valor!;

    private ManejadorRenovar Renovador => new(_repositorio, _identidad, Sesiones, _reloj, _contexto);

    [Fact]
    public async Task RenovarRotaElRefreshYConservaLaFamilia()
    {
        var inicial = await RegistrarAsync();

        var renovada = await Renovador.Handle(new RenovarComando(inicial.TokenRefresco), default);

        Assert.True(renovada.EsExito);
        Assert.NotEqual(inicial.TokenRefresco, renovada.Valor!.TokenRefresco);
        Assert.Equal(2, _repositorio.Tokens.Count);
        Assert.Single(_repositorio.Tokens.Select(t => t.FamiliaId).Distinct());
        Assert.NotNull(_repositorio.Tokens[0].UsadoUtc);
    }

    [Fact]
    public async Task ReusarUnRefreshYaRotadoRevocaTodaLaFamilia()
    {
        var inicial = await RegistrarAsync();
        var renovada = await Renovador.Handle(new RenovarComando(inicial.TokenRefresco), default);

        var reuso = await Renovador.Handle(new RenovarComando(inicial.TokenRefresco), default);
        var hijo = await Renovador.Handle(new RenovarComando(renovada.Valor!.TokenRefresco), default);

        Assert.Equal(TipoError.NoAutenticado, reuso.Error!.Tipo);
        Assert.All(_repositorio.Tokens, t => Assert.NotNull(t.RevocadoUtc));
        Assert.False(hijo.EsExito); // el token legítimo de la familia también quedó revocado
    }

    [Fact]
    public async Task UnRefreshCaducadoOInexistenteOAusenteDa401()
    {
        var inicial = await RegistrarAsync();
        _reloj.Ahora = _reloj.Ahora.AddDays(8);

        var caducado = await Renovador.Handle(new RenovarComando(inicial.TokenRefresco), default);
        var inexistente = await Renovador.Handle(new RenovarComando("no-existe"), default);
        var ausente = await Renovador.Handle(new RenovarComando(null), default);

        Assert.All([caducado, inexistente, ausente], r => Assert.Equal(TipoError.NoAutenticado, r.Error!.Tipo));
    }

    [Fact]
    public async Task RenovarConUsuarioEliminadoRevocaLaFamilia()
    {
        var inicial = await RegistrarAsync();
        _identidad.Usuarios.Clear();

        var resultado = await Renovador.Handle(new RenovarComando(inicial.TokenRefresco), default);

        Assert.False(resultado.EsExito);
        Assert.All(_repositorio.Tokens, t => Assert.NotNull(t.RevocadoUtc));
    }

    // ----- Cierre de sesión y perfil -----
    [Fact]
    public async Task CerrarSesionRevocaLaFamiliaYEsIdempotenteSinCookie()
    {
        var inicial = await RegistrarAsync();
        var manejador = new ManejadorCerrarSesion(_repositorio, _reloj);

        Assert.True((await manejador.Handle(new CerrarSesionComando(inicial.TokenRefresco), default)).EsExito);
        Assert.True((await manejador.Handle(new CerrarSesionComando(null), default)).EsExito);
        Assert.All(_repositorio.Tokens, t => Assert.NotNull(t.RevocadoUtc));
    }

    [Fact]
    public async Task ObtenerPerfilDevuelveCorreoYRolOError()
    {
        var inicial = await RegistrarAsync();
        var id = _identidad.Usuarios["a@b.com"].Usuario.Id;
        var manejador = new ManejadorObtenerPerfil(_identidad, _contexto);

        var ok = await manejador.Handle(new ObtenerPerfilQuery(id), default);
        var error = await manejador.Handle(new ObtenerPerfilQuery(Guid.NewGuid()), default);

        Assert.Equal("a@b.com", ok.Valor!.Correo);
        Assert.Equal(TipoError.NoAutenticado, error.Error!.Tipo);
        Assert.NotNull(inicial);
    }

    [Fact]
    public async Task PurgarEliminaSoloLosCaducados()
    {
        await RegistrarAsync();
        var limite = _reloj.Ahora.AddDays(8);

        Assert.Equal(0, await _repositorio.PurgarCaducadosAsync(_reloj.Ahora, default));
        Assert.Equal(1, await _repositorio.PurgarCaducadosAsync(limite, default));
    }
}
