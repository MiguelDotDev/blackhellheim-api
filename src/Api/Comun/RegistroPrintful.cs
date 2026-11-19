using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Infraestructura.Printful;
using Microsoft.Extensions.Options;

namespace BlackHellHeim.Api.Comun;

public static class RegistroPrintful
{
    /// <summary>Registra el puerto <see cref="IProveedorImpresion"/> con su cliente HTTP y los reintentos.</summary>
    public static IServiceCollection AgregarPrintful(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = configuracion.GetSection("Printful").Get<OpcionesPrintful>() ?? new OpcionesPrintful();
        opciones.Validar();
        servicios.Configure<OpcionesPrintful>(configuracion.GetSection("Printful"));
        servicios.AddMemoryCache();
        servicios.AddHttpClient<IProveedorImpresion, ProveedorImpresionPrintful>()
            // El tiempo por intento lo controla el manejador; el del cliente cubre todos los reintentos.
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(5))
            .AddHttpMessageHandler(sp => new ManejadorReintentos(
                TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<OpcionesPrintful>>().Value.TiempoEsperaSegundos),
                sp.GetRequiredService<TimeProvider>()));
        return servicios;
    }
}
