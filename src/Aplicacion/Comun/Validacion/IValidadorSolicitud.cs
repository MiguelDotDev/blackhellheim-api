using BlackHellHeim.Aplicacion.Comun.Errores;

namespace BlackHellHeim.Aplicacion.Comun.Validacion;

/// <summary>Valida una solicitud antes de que llegue a su manejador.</summary>
public interface IValidadorSolicitud<in TSolicitud>
{
    IReadOnlyList<ErrorCampo> Validar(TSolicitud solicitud);
}
