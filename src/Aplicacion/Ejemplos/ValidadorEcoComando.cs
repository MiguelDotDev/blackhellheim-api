using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;

namespace BlackHellHeim.Aplicacion.Ejemplos;

public sealed class ValidadorEcoComando : IValidadorSolicitud<EcoComando>
{
    public IReadOnlyList<ErrorCampo> Validar(EcoComando solicitud) =>
        string.IsNullOrWhiteSpace(solicitud.Texto)
            ? [new ErrorCampo("texto", "El texto es obligatorio.")]
            : [];
}
