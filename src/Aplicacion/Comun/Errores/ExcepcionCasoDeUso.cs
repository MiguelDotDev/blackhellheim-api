namespace BlackHellHeim.Aplicacion.Comun.Errores;

/// <summary>Excepción que transporta un <see cref="ErrorUniforme"/> ya clasificado.</summary>
public class ExcepcionCasoDeUso : Exception
{
    public ExcepcionCasoDeUso(ErrorUniforme error, Exception? interna = null)
        : base(error.Mensaje, interna)
    {
        Error = error;
    }

    public ErrorUniforme Error { get; }
}
