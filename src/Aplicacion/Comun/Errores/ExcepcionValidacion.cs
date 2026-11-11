namespace BlackHellHeim.Aplicacion.Comun.Errores;

/// <summary>Se lanza cuando una solicitud no pasa la validación, antes del manejador.</summary>
public sealed class ExcepcionValidacion : ExcepcionCasoDeUso
{
    public ExcepcionValidacion(IReadOnlyList<ErrorCampo> errores, string idCorrelacion)
        : base(ErrorUniforme.DeValidacion(errores, idCorrelacion))
    {
        Errores = errores;
    }

    public IReadOnlyList<ErrorCampo> Errores { get; }
}
