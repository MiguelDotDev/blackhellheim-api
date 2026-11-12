using BlackHellHeim.Dominio.Autenticacion;

namespace BlackHellHeim.Aplicacion.Autenticacion;

public enum ResultadoCreacion { Creado, CorreoDuplicado, ContrasenaInvalida }

public enum ResultadoCredenciales { Correctas, Incorrectas, Bloqueado }

/// <summary>Puerto hacia la identidad (usuarios, contraseñas, roles y bloqueo).</summary>
public interface IServicioIdentidad
{
    Task<(ResultadoCreacion Resultado, UsuarioIdentidad? Usuario)> CrearClienteAsync(
        string correo, string contrasena, CancellationToken cancelacion);

    Task<(ResultadoCredenciales Resultado, UsuarioIdentidad? Usuario)> VerificarCredencialesAsync(
        string correo, string contrasena, CancellationToken cancelacion);

    Task<UsuarioIdentidad?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);
}

/// <summary>Puerto que emite el token de acceso firmado.</summary>
public interface IEmisorTokens
{
    TimeSpan VidaAcceso { get; }

    string CrearTokenAcceso(UsuarioIdentidad usuario);
}

public interface IRepositorioTokensRefresco
{
    Task AgregarAsync(TokenRefresco token, CancellationToken cancelacion);

    Task<TokenRefresco?> ObtenerPorHashAsync(string hash, CancellationToken cancelacion);

    /// <summary>Marca como usado solo si sigue sin usar ni revocar; false si otra petición ganó.</summary>
    Task<bool> MarcarUsadoAsync(Guid id, DateTimeOffset ahora, CancellationToken cancelacion);

    Task RevocarFamiliaAsync(Guid familiaId, DateTimeOffset ahora, CancellationToken cancelacion);

    /// <summary>Elimina los tokens caducados antes de <paramref name="limite"/>; devuelve cuántos.</summary>
    Task<int> PurgarCaducadosAsync(DateTimeOffset limite, CancellationToken cancelacion);
}
