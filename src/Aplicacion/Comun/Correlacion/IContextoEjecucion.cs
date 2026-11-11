namespace BlackHellHeim.Aplicacion.Comun.Correlacion;

/// <summary>Puerto con el identificador de correlación de la ejecución actual.</summary>
public interface IContextoEjecucion
{
    string IdCorrelacion { get; set; }
}
