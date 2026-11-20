using BlackHellHeim.Dominio.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackHellHeim.Infraestructura.Persistencia.Configuraciones;

public sealed class EventoWebhookConfiguracion : IEntityTypeConfiguration<EventoWebhook>
{
    public void Configure(EntityTypeBuilder<EventoWebhook> c)
    {
        c.ToTable("EventosWebhook", "dbo");
        c.HasKey(e => e.Id);
        c.Property(e => e.Id).ValueGeneratedNever();
        c.Property(e => e.Proveedor).IsRequired().HasMaxLength(20).IsUnicode(false);
        c.Property(e => e.IdExterno).IsRequired().HasMaxLength(100);
        c.Property(e => e.Tipo).IsRequired().HasMaxLength(80).IsUnicode(false);
        c.Property(e => e.Estado).HasConversion<string>().HasMaxLength(12).IsUnicode(false);
        c.Property(e => e.Cuerpo).IsRequired();
        c.Property(e => e.Error).HasMaxLength(500);
        c.HasIndex(e => new { e.Proveedor, e.IdExterno }).IsUnique();
        c.HasIndex(e => new { e.Estado, e.RecibidoUtc });
    }
}
