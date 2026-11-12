using BlackHellHeim.Dominio.Autenticacion;
using BlackHellHeim.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackHellHeim.Api.Comun;

/// <summary>
/// Crea el administrador inicial si <c>Administrador__Correo</c> y <c>Administrador__Contrasena</c>
/// están definidos. Nunca registra la contraseña. Un fallo (por ejemplo, base sin migrar) solo
/// se avisa: la API no depende de la base al arrancar.
/// </summary>
public sealed class SembradoAdministrador(
    IServiceScopeFactory ambitos, IConfiguration configuracion, ILogger<SembradoAdministrador> registro)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        var correo = configuracion["Administrador:Correo"]?.Trim().ToLowerInvariant();
        var contrasena = configuracion["Administrador:Contrasena"];
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(contrasena))
        {
            return;
        }

        try
        {
            await using var ambito = ambitos.CreateAsyncScope();
            var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            if (await usuarios.FindByEmailAsync(correo) is not null)
            {
                return;
            }

            var admin = new Usuario { Id = Guid.NewGuid(), UserName = correo, Email = correo };
            var resultado = await usuarios.CreateAsync(admin, contrasena);
            if (resultado.Succeeded)
            {
                await usuarios.AddToRoleAsync(admin, Roles.Administrador);
                registro.LogInformation("Administrador inicial creado.");
            }
            else
            {
                registro.LogWarning("No se creó el administrador inicial: {Codigos}",
                    string.Join(",", resultado.Errors.Select(e => e.Code)));
            }
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            registro.LogWarning("No se pudo sembrar el administrador inicial ({Tipo}).", excepcion.GetType().Name);
        }
    }
}
