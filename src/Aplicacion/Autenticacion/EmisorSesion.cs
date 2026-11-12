using System.Security.Cryptography;
using BlackHellHeim.Dominio.Autenticacion;

namespace BlackHellHeim.Aplicacion.Autenticacion;

/// <summary>Crea sesiones: token de acceso más un refresh opaco (32 bytes CSPRNG) guardado con hash.</summary>
public sealed class EmisorSesion(IEmisorTokens emisor, IRepositorioTokensRefresco repositorio, TimeProvider reloj)
{
    public static readonly TimeSpan VidaRefresco = TimeSpan.FromDays(7);

    public static string CalcularHash(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    public async Task<SesionEmitida> EmitirAsync(
        UsuarioIdentidad usuario, Guid? familia, CancellationToken cancelacion)
    {
        var bruto = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var ahora = reloj.GetUtcNow();
        var token = TokenRefresco.Crear(usuario.Id, familia ?? Guid.NewGuid(), CalcularHash(bruto), ahora, VidaRefresco);
        await repositorio.AgregarAsync(token, cancelacion);

        var sesion = new SesionDto(emisor.CrearTokenAcceso(usuario), (int)emisor.VidaAcceso.TotalSeconds, usuario.Rol);
        return new SesionEmitida(sesion, bruto, token.ExpiraUtc);
    }
}
