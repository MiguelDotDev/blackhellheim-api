using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Dominio.Mockups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackHellHeim.Infraestructura.Persistencia.Configuraciones;

public sealed class TareaMockupConfiguracion : IEntityTypeConfiguration<TareaMockup>
{
    public void Configure(EntityTypeBuilder<TareaMockup> c)
    {
        c.ToTable("TareasMockup", "dbo");
        c.HasKey(t => t.Id);
        c.Property(t => t.Id).ValueGeneratedNever();
        c.Property(t => t.PrintfulTaskId).IsRequired().HasMaxLength(60).IsUnicode(false);
        c.Property(t => t.Estado).HasConversion<string>().HasMaxLength(12).IsUnicode(false);
        c.Property(t => t.Motivo).HasMaxLength(TareaMockup.MotivoMaximo);
        c.HasOne<Diseno>().WithMany().HasForeignKey(t => t.DisenoId).OnDelete(DeleteBehavior.Cascade);
        c.HasMany(t => t.Imagenes).WithOne().HasForeignKey(i => i.TareaId).OnDelete(DeleteBehavior.Cascade);
        c.HasIndex(t => t.PrintfulTaskId).IsUnique();
        // Una sola tarea pendiente por diseño.
        c.HasIndex(t => t.DisenoId).IsUnique().HasFilter("[Estado] = 'Pendiente'").HasDatabaseName("UX_TareasMockup_DisenoId_Pendiente");
        c.HasIndex(t => new { t.Estado, t.CreadaUtc });
    }
}

public sealed class ImagenMockupConfiguracion : IEntityTypeConfiguration<ImagenMockup>
{
    public void Configure(EntityTypeBuilder<ImagenMockup> c)
    {
        c.ToTable("ImagenesMockup", "dbo");
        c.HasKey(i => i.Id);
        c.Property(i => i.Id).ValueGeneratedNever();
        c.Property(i => i.Color).IsRequired().HasMaxLength(60);
        c.Property(i => i.Posicion).IsRequired().HasMaxLength(40);
        c.Property(i => i.Url).IsRequired().HasMaxLength(500);
        c.Property(i => i.NombreBlob).IsRequired().HasMaxLength(200);
        c.HasIndex(i => new { i.TareaId, i.Color, i.Posicion }).IsUnique();
    }
}
