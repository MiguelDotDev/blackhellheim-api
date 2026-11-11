using BlackHellHeim.Aplicacion.Comun.Comportamientos;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Registro;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using BlackHellHeim.Aplicacion.Ejemplos;
using BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;
using BlackHellHeim.Aplicacion.Autenticacion;
using Mediator;
using BlackHellHeim.Aplicacion.Tiendas;
using BlackHellHeim.Infraestructura.Tiendas;

namespace BlackHellHeim.Api.Comun;

/// <summary>Composición de servicios de la API: mediador, pipeline y adaptadores.</summary>
public static class RegistroServicios
{
    public static IServiceCollection AgregarCasosDeUso(this IServiceCollection servicios)
    {
        servicios.AddMediator(opciones => opciones.ServiceLifetime = ServiceLifetime.Scoped);

        servicios.AddScoped<IContextoEjecucion, ContextoEjecucionHttp>();
        servicios.AddScoped<IRegistroEjecucion, RegistroEjecucionLogger>();
        servicios.AddScoped<IRepositorioTiendas, RepositorioTiendasEnMemoria>();

        // Comportamientos del pipeline (genéricos abiertos, con ámbito). El primero registrado
        // es el más externo. El orden NO cambia; cada tarea ocupa su ranura:
        // 1 CorrelacionBehavior
        // 2 ErroresBehavior
        // 3 RegistroBehavior
        // 4 ValidacionBehavior
        servicios.AddScoped(typeof(IPipelineBehavior<,>), typeof(CorrelacionBehavior<,>)); // ranura 1
        servicios.AddScoped(typeof(IPipelineBehavior<,>), typeof(ErroresBehavior<,>)); // ranura 2
        servicios.AddScoped(typeof(IPipelineBehavior<,>), typeof(RegistroBehavior<,>)); // ranura 3
        servicios.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidacionBehavior<,>)); // ranura 4

        servicios.AddScoped<IValidadorSolicitud<ConsultarTiendaQuery>, ValidadorConsultarTienda>();
        servicios.AddScoped<IValidadorSolicitud<EcoComando>, ValidadorEcoComando>();

        servicios.AddScoped<EmisorSesion>();
        servicios.AddScoped<IValidadorSolicitud<RegistrarComando>, ValidadorRegistrar>();
        servicios.AddScoped<IValidadorSolicitud<IniciarSesionComando>, ValidadorIniciarSesion>();

        return servicios;
    }
}
