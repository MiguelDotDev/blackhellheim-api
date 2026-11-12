using System.Text.RegularExpressions;
using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using Mediator;

namespace BlackHellHeim.Aplicacion.Autenticacion;

public sealed record RegistrarComando(string Correo, string Contrasena)
    : ICommand<ResultadoCasoDeUso<SesionEmitida>>;

public sealed record IniciarSesionComando(string Correo, string Contrasena)
    : ICommand<ResultadoCasoDeUso<SesionEmitida>>;

public sealed record RenovarComando(string? TokenRefresco) : ICommand<ResultadoCasoDeUso<SesionEmitida>>;

public sealed record CerrarSesionComando(string? TokenRefresco) : ICommand<ResultadoCasoDeUso<bool>>;

public sealed record ObtenerPerfilQuery(Guid UsuarioId) : IQuery<ResultadoCasoDeUso<PerfilDto>>;

/// <summary>Reglas de entrada comunes: correo bien formado y contraseña de 10 a 128 con letra y número.</summary>
public static partial class ReglasCredenciales
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex PatronCorreo();

    public static string Normalizar(string? correo) => (correo ?? "").Trim().ToLowerInvariant();

    public static List<ErrorCampo> Validar(string? correo, string? contrasena, bool politica)
    {
        var errores = new List<ErrorCampo>();
        var normal = Normalizar(correo);
        if (normal.Length is 0 or > 254 || !PatronCorreo().IsMatch(normal))
        {
            errores.Add(new ErrorCampo("correo", "El correo no tiene un formato válido."));
        }

        if (string.IsNullOrEmpty(contrasena))
        {
            errores.Add(new ErrorCampo("contrasena", "La contraseña es obligatoria."));
        }
        else if (contrasena.Length > 128)
        {
            errores.Add(new ErrorCampo("contrasena", "La contraseña no puede superar 128 caracteres."));
        }
        else if (politica && (contrasena.Length < 10 || !contrasena.Any(char.IsLetter) || !contrasena.Any(char.IsDigit)))
        {
            errores.Add(new ErrorCampo("contrasena", "La contraseña debe tener al menos 10 caracteres, con letras y números."));
        }

        return errores;
    }
}

public sealed class ValidadorRegistrar : IValidadorSolicitud<RegistrarComando>
{
    public IReadOnlyList<ErrorCampo> Validar(RegistrarComando s) => ReglasCredenciales.Validar(s.Correo, s.Contrasena, true);
}

public sealed class ValidadorIniciarSesion : IValidadorSolicitud<IniciarSesionComando>
{
    public IReadOnlyList<ErrorCampo> Validar(IniciarSesionComando s) => ReglasCredenciales.Validar(s.Correo, s.Contrasena, false);
}

public sealed class ManejadorRegistrar(IServicioIdentidad identidad, EmisorSesion sesiones, IContextoEjecucion contexto)
    : ICommandHandler<RegistrarComando, ResultadoCasoDeUso<SesionEmitida>>
{
    public async ValueTask<ResultadoCasoDeUso<SesionEmitida>> Handle(RegistrarComando c, CancellationToken cancelacion)
    {
        var (resultado, usuario) = await identidad.CrearClienteAsync(
            ReglasCredenciales.Normalizar(c.Correo), c.Contrasena, cancelacion);
        return resultado switch
        {
            ResultadoCreacion.Creado => ResultadoCasoDeUso<SesionEmitida>.Exito(
                await sesiones.EmitirAsync(usuario!, null, cancelacion)),
            ResultadoCreacion.CorreoDuplicado => ResultadoCasoDeUso<SesionEmitida>.Fallo(
                ErrorUniforme.Conflicto("No fue posible completar el registro con ese correo.", contexto.IdCorrelacion)),
            _ => ResultadoCasoDeUso<SesionEmitida>.Fallo(ErrorUniforme.DeValidacion(
                [new ErrorCampo("contrasena", "La contraseña no cumple la política de seguridad.")], contexto.IdCorrelacion))
        };
    }
}

public sealed class ManejadorIniciarSesion(IServicioIdentidad identidad, EmisorSesion sesiones, IContextoEjecucion contexto)
    : ICommandHandler<IniciarSesionComando, ResultadoCasoDeUso<SesionEmitida>>
{
    public async ValueTask<ResultadoCasoDeUso<SesionEmitida>> Handle(IniciarSesionComando c, CancellationToken cancelacion)
    {
        var (resultado, usuario) = await identidad.VerificarCredencialesAsync(
            ReglasCredenciales.Normalizar(c.Correo), c.Contrasena, cancelacion);
        return resultado switch
        {
            ResultadoCredenciales.Correctas => ResultadoCasoDeUso<SesionEmitida>.Exito(
                await sesiones.EmitirAsync(usuario!, null, cancelacion)),
            ResultadoCredenciales.Bloqueado => ResultadoCasoDeUso<SesionEmitida>.Fallo(
                ErrorUniforme.Bloqueado(contexto.IdCorrelacion)),
            _ => ResultadoCasoDeUso<SesionEmitida>.Fallo(ErrorUniforme.NoAutenticado(contexto.IdCorrelacion))
        };
    }
}

public sealed class ManejadorRenovar(
    IRepositorioTokensRefresco repositorio, IServicioIdentidad identidad, EmisorSesion sesiones,
    TimeProvider reloj, IContextoEjecucion contexto)
    : ICommandHandler<RenovarComando, ResultadoCasoDeUso<SesionEmitida>>
{
    public async ValueTask<ResultadoCasoDeUso<SesionEmitida>> Handle(RenovarComando c, CancellationToken cancelacion)
    {
        var fallo = ResultadoCasoDeUso<SesionEmitida>.Fallo(ErrorUniforme.NoAutenticado(contexto.IdCorrelacion));
        if (string.IsNullOrWhiteSpace(c.TokenRefresco))
        {
            return fallo;
        }

        var ahora = reloj.GetUtcNow();
        var token = await repositorio.ObtenerPorHashAsync(EmisorSesion.CalcularHash(c.TokenRefresco), cancelacion);
        if (token is null)
        {
            return fallo;
        }

        // Reuso de un token ya rotado o revocado, o carrera perdida: se revoca toda la familia.
        if (token.FueConsumido || !await repositorio.MarcarUsadoAsync(token.Id, ahora, cancelacion))
        {
            await repositorio.RevocarFamiliaAsync(token.FamiliaId, ahora, cancelacion);
            return fallo;
        }

        var usuario = token.ExpiraUtc > ahora ? await identidad.ObtenerPorIdAsync(token.UsuarioId, cancelacion) : null;
        if (usuario is null)
        {
            await repositorio.RevocarFamiliaAsync(token.FamiliaId, ahora, cancelacion);
            return fallo;
        }

        return ResultadoCasoDeUso<SesionEmitida>.Exito(await sesiones.EmitirAsync(usuario, token.FamiliaId, cancelacion));
    }
}

public sealed class ManejadorCerrarSesion(IRepositorioTokensRefresco repositorio, TimeProvider reloj)
    : ICommandHandler<CerrarSesionComando, ResultadoCasoDeUso<bool>>
{
    public async ValueTask<ResultadoCasoDeUso<bool>> Handle(CerrarSesionComando c, CancellationToken cancelacion)
    {
        if (!string.IsNullOrWhiteSpace(c.TokenRefresco))
        {
            var token = await repositorio.ObtenerPorHashAsync(EmisorSesion.CalcularHash(c.TokenRefresco), cancelacion);
            if (token is not null)
            {
                await repositorio.RevocarFamiliaAsync(token.FamiliaId, reloj.GetUtcNow(), cancelacion);
            }
        }

        return ResultadoCasoDeUso<bool>.Exito(true);
    }
}

public sealed class ManejadorObtenerPerfil(IServicioIdentidad identidad, IContextoEjecucion contexto)
    : IQueryHandler<ObtenerPerfilQuery, ResultadoCasoDeUso<PerfilDto>>
{
    public async ValueTask<ResultadoCasoDeUso<PerfilDto>> Handle(ObtenerPerfilQuery q, CancellationToken cancelacion)
    {
        var usuario = await identidad.ObtenerPorIdAsync(q.UsuarioId, cancelacion);
        return usuario is null
            ? ResultadoCasoDeUso<PerfilDto>.Fallo(ErrorUniforme.NoAutenticado(contexto.IdCorrelacion))
            : ResultadoCasoDeUso<PerfilDto>.Exito(new PerfilDto(usuario.Id, usuario.Correo, usuario.Rol));
    }
}
