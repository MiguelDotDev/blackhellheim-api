using System.Text.RegularExpressions;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;

namespace BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;

public sealed partial class ValidadorConsultarTienda : IValidadorSolicitud<ConsultarTiendaQuery>
{
    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Patron();

    public IReadOnlyList<ErrorCampo> Validar(ConsultarTiendaQuery solicitud) =>
        solicitud.Codigo is not null && Patron().IsMatch(solicitud.Codigo)
            ? []
            : [new ErrorCampo("codigo", "El código debe tener exactamente tres letras mayúsculas.")];
}
