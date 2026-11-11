using BlackHellHeim.Aplicacion.Comun.Correlacion;

namespace BlackHellHeim.Pruebas.Dobles;

public sealed class ContextoEjecucionFalso : IContextoEjecucion
{
    public string IdCorrelacion { get; set; } = string.Empty;
}
