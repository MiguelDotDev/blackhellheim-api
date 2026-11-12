using Microsoft.AspNetCore.Identity;

namespace BlackHellHeim.Infraestructura.Identidad;

/// <summary>Usuario de Identity. Existe solo en Infraestructura; el resto de capas usa <c>UsuarioIdentidad</c>.</summary>
public sealed class Usuario : IdentityUser<Guid>
{
    public DateTimeOffset FechaCreacionUtc { get; set; } = DateTimeOffset.UtcNow;
}
