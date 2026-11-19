using BlackHellHeim.Aplicacion.Disenos;

namespace BlackHellHeim.Api.Comun;

/// <summary>Lee <c>Disenos:TerminosBloqueados</c> (marcas y bandas de terceros que solo generan un aviso).</summary>
public sealed class TerminosBloqueadosConfiguracion(IConfiguration configuracion) : ITerminosBloqueados
{
    public IReadOnlyList<string> Terminos { get; } =
        configuracion.GetSection("Disenos:TerminosBloqueados").Get<string[]>()?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray() ?? [];
}
