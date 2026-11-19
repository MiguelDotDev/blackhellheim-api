using BlackHellHeim.Dominio.Disenos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackHellHeim.Infraestructura.Persistencia.Configuraciones;

public sealed class DisenoConfiguracion : IEntityTypeConfiguration<Diseno>
{
    public void Configure(EntityTypeBuilder<Diseno> c)
    {
        c.ToTable("Disenos", "dbo");
        c.HasKey(d => d.Id);
        c.Property(d => d.Id).ValueGeneratedNever();
        c.Property(d => d.Nombre).IsRequired().HasMaxLength(Diseno.NombreMaximo);
        c.Property(d => d.Slug).IsRequired().HasMaxLength(120).IsUnicode(false);
        c.Property(d => d.Descripcion).HasMaxLength(Diseno.DescripcionMaxima);
        c.Property(d => d.Estado).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        c.Property(d => d.Version).IsRowVersion();
        c.HasIndex(d => d.Nombre).IsUnique();
        c.HasIndex(d => d.Slug).IsUnique();
        c.HasIndex(d => new { d.Estado, d.CreadoUtc });
        c.HasOne(d => d.ArchivoArte).WithMany().HasForeignKey(d => d.ArchivoArteId).OnDelete(DeleteBehavior.Restrict);
        c.HasMany(d => d.Variantes).WithMany().UsingEntity("DisenosVariantes",
            r => r.HasOne(typeof(Variante)).WithMany().HasForeignKey("VarianteId").OnDelete(DeleteBehavior.Restrict),
            l => l.HasOne(typeof(Diseno)).WithMany().HasForeignKey("DisenoId").OnDelete(DeleteBehavior.Cascade));
        c.Navigation(d => d.Variantes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductoBaseConfiguracion : IEntityTypeConfiguration<ProductoBase>
{
    public void Configure(EntityTypeBuilder<ProductoBase> c)
    {
        c.ToTable("ProductosBase", "dbo");
        c.HasKey(p => p.Id);
        c.Property(p => p.Id).ValueGeneratedNever();
        c.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
        c.Property(p => p.Tipo).IsRequired().HasMaxLength(100);
        c.HasIndex(p => p.PrintfulProductoId).IsUnique();
    }
}

public sealed class VarianteConfiguracion : IEntityTypeConfiguration<Variante>
{
    public void Configure(EntityTypeBuilder<Variante> c)
    {
        c.ToTable("Variantes", "dbo");
        c.HasKey(v => v.Id);
        c.Property(v => v.Id).ValueGeneratedNever();
        c.Property(v => v.Color).IsRequired().HasMaxLength(60);
        c.Property(v => v.CodigoColor).IsRequired().HasMaxLength(20);
        c.Property(v => v.Talla).IsRequired().HasMaxLength(20);
        c.Property(v => v.CostoBaseUsd).HasPrecision(10, 2);
        c.HasIndex(v => v.PrintfulVarianteId).IsUnique();
        c.HasOne(v => v.ProductoBase).WithMany().HasForeignKey(v => v.ProductoBaseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ArchivoArteConfiguracion : IEntityTypeConfiguration<ArchivoArte>
{
    public void Configure(EntityTypeBuilder<ArchivoArte> c)
    {
        c.ToTable("ArchivosArte", "dbo");
        c.HasKey(a => a.Id);
        c.Property(a => a.Id).ValueGeneratedNever();
        c.Property(a => a.Url).IsRequired().HasMaxLength(500);
        c.Property(a => a.NombreBlob).IsRequired().HasMaxLength(200);
        c.Property(a => a.TipoContenido).IsRequired().HasMaxLength(100);
        c.Property(a => a.HashSha256).IsRequired().HasMaxLength(64).IsUnicode(false).IsFixedLength();
        c.HasIndex(a => a.HashSha256).IsUnique();
    }
}
