using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlackHellHeim.Infraestructura.Identidad;

public static class ExtensionesIdentidad
{
    /// <summary>Registra Identity, el emisor JWT, el repositorio de refresh y el sembrado del administrador.</summary>
    public static IServiceCollection AgregarIdentidad(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var jwt = configuracion.GetSection("Jwt").Get<OpcionesJwt>() ?? new OpcionesJwt();
        jwt.Validar();
        servicios.Configure<OpcionesJwt>(configuracion.GetSection("Jwt"));

        servicios.AddIdentityCore<Usuario>(o =>
            {
                o.Password.RequiredLength = 10;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ContextoBlackHellHeim>();

        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<IServicioIdentidad, ServicioIdentidad>();
        servicios.AddScoped<IEmisorTokens, EmisorTokensJwt>();
        servicios.AddScoped<IRepositorioTokensRefresco, RepositorioTokensRefresco>();
        return servicios;
    }
}
