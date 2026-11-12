using BlackHellHeim.Dominio.Autenticacion;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackHellHeim.Infraestructura.Persistencia.Configuraciones;

/// <summary>Siembra determinista de los roles Cliente y Administrador (forma parte de la migración).</summary>
public sealed class RolesSemillaConfiguracion : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public static readonly Guid IdCliente = Guid.Parse("b1c0a8f2-3d4e-4f60-9a71-0c2d5e8f1a01");
    public static readonly Guid IdAdministrador = Guid.Parse("b1c0a8f2-3d4e-4f60-9a71-0c2d5e8f1a02");

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> constructor) =>
        constructor.HasData(
            new IdentityRole<Guid>(Roles.Cliente) { Id = IdCliente, NormalizedName = "CLIENTE", ConcurrencyStamp = "rol-cliente-v1" },
            new IdentityRole<Guid>(Roles.Administrador) { Id = IdAdministrador, NormalizedName = "ADMINISTRADOR", ConcurrencyStamp = "rol-administrador-v1" });
}
