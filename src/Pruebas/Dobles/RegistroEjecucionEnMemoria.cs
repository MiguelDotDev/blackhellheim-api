using BlackHellHeim.Aplicacion.Comun.Registro;

namespace BlackHellHeim.Pruebas.Dobles;

public sealed class RegistroEjecucionEnMemoria : IRegistroEjecucion
{
    private readonly List<RegistroCasoDeUso> _registros = [];

    /// <summary>Si es verdadero, <see cref="Registrar"/> lanza para probar la tolerancia a fallos.</summary>
    public bool Falla { get; set; }

    public IReadOnlyList<RegistroCasoDeUso> Registros => _registros;

    public void Registrar(RegistroCasoDeUso registro)
    {
        if (Falla)
        {
            throw new InvalidOperationException("Fallo simulado del registro.");
        }

        _registros.Add(registro);
    }
}
