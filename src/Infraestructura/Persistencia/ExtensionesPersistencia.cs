using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlackHellHeim.Infraestructura.Persistencia;

public static class ExtensionesPersistencia
{
    /// <summary>Registra el contexto con SQL Server y ámbito. No conecta al registrarse.</summary>
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string cadenaConexion)
    {
        servicios.AddDbContext<ContextoBlackHellHeim>(
            opciones => opciones.UseSqlServer(cadenaConexion), ServiceLifetime.Scoped);
        return servicios;
    }
}
