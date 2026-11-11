using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Infraestructura.Identidad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BlackHellHeim.Api.Comun;

/// <summary>Validación del JWT de acceso y respuestas 401/403 uniformes como ProblemDetails.</summary>
public static class ConfiguracionAutenticacion
{
    public static IServiceCollection AgregarAutenticacionJwt(this IServiceCollection servicios)
    {
        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        servicios.AddAuthorization();
        servicios.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<OpcionesJwt>>((opciones, jwt) =>
            {
                opciones.MapInboundClaims = false;
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Emisor,
                    ValidAudience = jwt.Value.Audiencia,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.Value.BytesClave),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
                opciones.Events = new JwtBearerEvents
                {
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        await Responder(contexto.HttpContext, ErrorUniforme.NoAutenticado);
                    },
                    OnForbidden = contexto => Responder(contexto.HttpContext, ErrorUniforme.Prohibido)
                };
            });
        return servicios;
    }

    private static Task Responder(HttpContext http, Func<string, ErrorUniforme> fabrica)
    {
        var correlacion = http.RequestServices.GetRequiredService<IContextoEjecucion>().IdCorrelacion;
        return TraductorProblemas.Traducir(fabrica(correlacion), http.Request.Path.Value ?? "/").ExecuteAsync(http);
    }
}
