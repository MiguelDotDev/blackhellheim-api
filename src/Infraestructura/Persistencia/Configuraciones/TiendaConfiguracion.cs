using BlackHellHeim.Dominio.Tiendas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackHellHeim.Infraestructura.Persistencia.Configuraciones;

/// <summary>Mapeo de <see cref="Tienda"/> a <c>dbo.Tiendas</c> con las dos tiendas de referencia.</summary>
public sealed class TiendaConfiguracion : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<Tienda>
{
    public void Configure(EntityTypeBuilder<Tienda> constructor)
    {
        constructor.ToTable("Tiendas", "dbo");

        constructor.HasKey(t => t.Codigo);
        constructor.Property(t => t.Codigo)
            .HasConversion(codigo => codigo.Valor, valor => CodigoTienda.Crear(valor))
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsFixedLength()
            .ValueGeneratedNever();

        constructor.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(120);

        constructor.Property(t => t.Moneda)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsFixedLength();

        // Semillas deterministas: forman parte de la migración inicial.
        constructor.HasData(
            new Tienda(CodigoTienda.Crear("BHH"), "BlackHellHeim", "MXN"),
            new Tienda(CodigoTienda.Crear("DEM"), "Tienda de demostración", "MXN"));
    }
}
