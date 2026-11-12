using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Infraestructura.Almacen;

namespace BlackHellHeim.Api.Comun;

public static class RegistroArte
{
    /// <summary>Registra la subida de arte: límites <c>Arte__*</c> validados al arrancar y almacén <c>AlmacenArchivos__*</c>.</summary>
    public static IServiceCollection AgregarArte(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var arte = configuracion.GetSection("Arte").Get<OpcionesArte>() ?? new OpcionesArte();
        arte.Validar();
        servicios.AddSingleton(arte);
        servicios.AddSingleton(configuracion.GetSection("AlmacenArchivos").Get<OpcionesAlmacen>() ?? new OpcionesAlmacen());
        servicios.AddSingleton<IAlmacenArchivos, AlmacenBlobAzure>();
        servicios.AddSingleton<IMuestreadorTransparencia, MuestreadorTransparenciaSkia>();
        servicios.AddSingleton<ValidadorArte>();
        servicios.AddScoped<IRepositorioArte, RepositorioArte>();
        return servicios;
    }
}
