using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Dominio.Autenticacion;
using Microsoft.AspNetCore.Identity;

namespace BlackHellHeim.Infraestructura.Identidad;

public sealed class ServicioIdentidad(UserManager<Usuario> usuarios) : IServicioIdentidad
{
    public async Task<(ResultadoCreacion, UsuarioIdentidad?)> CrearClienteAsync(
        string correo, string contrasena, CancellationToken cancelacion)
    {
        var usuario = new Usuario { Id = Guid.NewGuid(), UserName = correo, Email = correo };
        var resultado = await usuarios.CreateAsync(usuario, contrasena);
        if (!resultado.Succeeded)
        {
            return resultado.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName")
                ? (ResultadoCreacion.CorreoDuplicado, null)
                : (ResultadoCreacion.ContrasenaInvalida, null);
        }

        await usuarios.AddToRoleAsync(usuario, Roles.Cliente);
        return (ResultadoCreacion.Creado, new UsuarioIdentidad(usuario.Id, correo, Roles.Cliente));
    }

    public async Task<(ResultadoCredenciales, UsuarioIdentidad?)> VerificarCredencialesAsync(
        string correo, string contrasena, CancellationToken cancelacion)
    {
        var usuario = await usuarios.FindByEmailAsync(correo);
        if (usuario is null)
        {
            return (ResultadoCredenciales.Incorrectas, null);
        }

        if (await usuarios.IsLockedOutAsync(usuario))
        {
            return (ResultadoCredenciales.Bloqueado, null);
        }

        if (!await usuarios.CheckPasswordAsync(usuario, contrasena))
        {
            await usuarios.AccessFailedAsync(usuario);
            return (ResultadoCredenciales.Incorrectas, null);
        }

        await usuarios.ResetAccessFailedCountAsync(usuario);
        return (ResultadoCredenciales.Correctas, await ProyectarAsync(usuario));
    }

    public async Task<UsuarioIdentidad?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion)
    {
        var usuario = await usuarios.FindByIdAsync(id.ToString());
        return usuario is null ? null : await ProyectarAsync(usuario);
    }

    private async Task<UsuarioIdentidad> ProyectarAsync(Usuario usuario)
    {
        var roles = await usuarios.GetRolesAsync(usuario);
        var rol = roles.Contains(Roles.Administrador) ? Roles.Administrador : Roles.Cliente;
        return new UsuarioIdentidad(usuario.Id, usuario.Email!, rol);
    }
}
