using BlackHellHeim.Aplicacion.Autenticacion;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BlackHellHeim.Infraestructura.Identidad;

/// <summary>Emite el JWT de acceso (HS256, ~15 min) con solo id de usuario y rol.</summary>
public sealed class EmisorTokensJwt(IOptions<OpcionesJwt> opciones, TimeProvider reloj) : IEmisorTokens
{
    private readonly OpcionesJwt _opciones = opciones.Value;

    public TimeSpan VidaAcceso => TimeSpan.FromMinutes(_opciones.MinutosAcceso);

    public string CrearTokenAcceso(UsuarioIdentidad usuario)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = ahora + VidaAcceso,
            Claims = new Dictionary<string, object> { ["sub"] = usuario.Id.ToString(), ["role"] = usuario.Rol },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_opciones.BytesClave),
                SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
