using BlackHellHeim.Dominio.Autenticacion;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Dominio.Tiendas;
using BlackHellHeim.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlackHellHeim.Infraestructura.Persistencia;

/// <summary>Contexto de EF Core de BlackHellHeim. El esquema se gestiona solo con migraciones.</summary>
public class ContextoBlackHellHeim(DbContextOptions<ContextoBlackHellHeim> opciones) : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(opciones)
{
    public DbSet<Tienda> Tiendas => Set<Tienda>();

    public DbSet<TokenRefresco> TokensRefresco => Set<TokenRefresco>();
    public DbSet<Diseno> Disenos => Set<Diseno>();
    public DbSet<ProductoBase> ProductosBase => Set<ProductoBase>();
    public DbSet<Variante> Variantes => Set<Variante>();
    public DbSet<ArchivoArte> ArchivosArte => Set<ArchivoArte>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        base.OnModelCreating(constructor);
        constructor.ApplyConfigurationsFromAssembly(typeof(ContextoBlackHellHeim).Assembly);
    }
}
