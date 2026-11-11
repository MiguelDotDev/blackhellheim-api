using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BlackHellHeim.Infraestructura.Persistencia;

/// <summary>
/// Fábrica de tiempo de diseño para <c>dotnet ef</c>. Lee <c>ConnectionStrings__BlackHellHeim</c>;
/// si falta, falla nombrando la variable y sin imprimir ningún valor.
/// </summary>
public sealed class FabricaContextoDiseno : IDesignTimeDbContextFactory<ContextoBlackHellHeim>
{
    public const string NombreVariable = "ConnectionStrings__BlackHellHeim";

    public ContextoBlackHellHeim CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable(NombreVariable);
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                $"Falta la variable de entorno {NombreVariable}. Copia .env.example a .env, define la contraseña y cárgalo.");
        }

        var opciones = new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options;
        return new ContextoBlackHellHeim(opciones);
    }
}
