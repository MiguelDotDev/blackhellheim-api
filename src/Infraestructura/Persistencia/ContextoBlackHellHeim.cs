using BlackHellHeim.Dominio.Tiendas;
using Microsoft.EntityFrameworkCore;

namespace BlackHellHeim.Infraestructura.Persistencia;

/// <summary>Contexto de EF Core de BlackHellHeim. El esquema se gestiona solo con migraciones.</summary>
public class ContextoBlackHellHeim(DbContextOptions<ContextoBlackHellHeim> opciones) : DbContext(opciones)
{
    public DbSet<Tienda> Tiendas => Set<Tienda>();

    protected override void OnModelCreating(ModelBuilder constructor) =>
        constructor.ApplyConfigurationsFromAssembly(typeof(ContextoBlackHellHeim).Assembly);
}
