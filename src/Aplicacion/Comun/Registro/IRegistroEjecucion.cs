namespace BlackHellHeim.Aplicacion.Comun.Registro;

/// <summary>Puerto para registrar cada ejecución de un caso de uso.</summary>
public interface IRegistroEjecucion
{
    void Registrar(RegistroCasoDeUso registro);
}
