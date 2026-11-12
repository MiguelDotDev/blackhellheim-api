using BlackHellHeim.Dominio.Autenticacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackHellHeim.Infraestructura.Persistencia.Configuraciones;

public sealed class TokenRefrescoConfiguracion : IEntityTypeConfiguration<TokenRefresco>
{
    public void Configure(EntityTypeBuilder<TokenRefresco> constructor)
    {
        constructor.ToTable("TokensRefresco", "dbo");
        constructor.HasKey(t => t.Id);
        constructor.Property(t => t.Id).ValueGeneratedNever();
        constructor.Property(t => t.HashToken).IsRequired().HasMaxLength(64).IsUnicode(false).IsFixedLength();
        constructor.HasIndex(t => t.HashToken).IsUnique();
        constructor.HasIndex(t => t.FamiliaId);
        constructor.HasIndex(t => t.ExpiraUtc);
        constructor.HasOne<Identidad.Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
