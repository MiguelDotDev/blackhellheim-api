using BlackHellHeim.Aplicacion.Comun.Validacion;
using BlackHellHeim.Aplicacion.Precios;
using BlackHellHeim.Infraestructura.Precios;

namespace BlackHellHeim.Api.Comun;

public sealed class AdvertenciasLogger(ILogger<AdvertenciasLogger> registro) : IAdvertencias
{
    public void Advertir(string mensaje) => registro.LogWarning("{Mensaje}", mensaje);
}

public static class RegistroPrecios
{
    /// <summary>Motor de precios: tipo de cambio de Banxico (<c>Banxico__Token</c>, opcional) y parámetros en base de datos.</summary>
    public static IServiceCollection AgregarPrecios(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var banxico = configuracion.GetSection("Banxico").Get<OpcionesBanxico>() ?? new OpcionesBanxico();
        servicios.AddSingleton(banxico);
        servicios.AddHttpClient<IProveedorTipoCambio, ProveedorTipoCambioBanxico>(c => c.Timeout = TimeSpan.FromSeconds(10));
        servicios.AddSingleton<IAdvertencias, AdvertenciasLogger>();
        servicios.AddScoped<IRepositorioPrecios, RepositorioPrecios>();
        servicios.AddScoped<ServicioTipoCambio>();
        servicios.AddScoped<ICalculadoraPrecios, ServicioPrecios>();
        servicios.AddScoped<IValidadorSolicitud<ActualizarParametrosPrecioComando>, ValidadorActualizarParametrosPrecio>();
        return servicios;
    }
}
