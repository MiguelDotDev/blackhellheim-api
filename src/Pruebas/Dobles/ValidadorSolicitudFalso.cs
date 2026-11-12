using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;

namespace BlackHellHeim.Pruebas.Dobles;

public sealed class ValidadorSolicitudFalso<TSolicitud>(params ErrorCampo[] errores)
    : IValidadorSolicitud<TSolicitud>
{
    public IReadOnlyList<ErrorCampo> Validar(TSolicitud solicitud) => errores;
}
