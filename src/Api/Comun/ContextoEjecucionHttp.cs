using BlackHellHeim.Aplicacion.Comun.Correlacion;

namespace BlackHellHeim.Api.Comun;

/// <summary>Adaptador de <see cref="IContextoEjecucion"/> con ámbito de petición HTTP.</summary>
public sealed class ContextoEjecucionHttp : IContextoEjecucion
{
    public string IdCorrelacion { get; set; } = string.Empty;
}
