using BlackHellHeim.Aplicacion.Comun.Errores;

namespace BlackHellHeim.Aplicacion.Comun.CasosDeUso;

/// <summary>Vista no genérica de un resultado, para comportamientos que no conocen el tipo de valor.</summary>
public interface IResultadoCasoDeUso
{
    bool EsExito { get; }

    ErrorUniforme? Error { get; }
}

/// <summary>Resultado de un caso de uso: un valor en éxito o un error uniforme en fallo.</summary>
public sealed class ResultadoCasoDeUso<T> : IResultadoCasoDeUso
{
    private ResultadoCasoDeUso(T? valor, ErrorUniforme? error)
    {
        Valor = valor;
        Error = error;
    }

    public bool EsExito => Error is null;

    public T? Valor { get; }

    public ErrorUniforme? Error { get; }

    public static ResultadoCasoDeUso<T> Exito(T valor) => new(valor, null);

    public static ResultadoCasoDeUso<T> Fallo(ErrorUniforme error) =>
        new(default, error ?? throw new ArgumentNullException(nameof(error)));
}
