using BlackHellHeim.Aplicacion.Comun.Registro;

namespace BlackHellHeim.Api.Comun;

/// <summary>Adaptador de <see cref="IRegistroEjecucion"/> que escribe con <see cref="ILogger"/>.</summary>
public sealed class RegistroEjecucionLogger(ILogger<RegistroEjecucionLogger> registrador) : IRegistroEjecucion
{
    public void Registrar(RegistroCasoDeUso registro)
    {
        registrador.LogInformation(
            "Caso de uso {TipoSolicitud} {Resultado} en {DuracionMs} ms [{IdCorrelacion}] {CodigoError} {TipoExcepcion}",
            registro.TipoSolicitud,
            registro.Exito ? "exitoso" : "fallido",
            registro.DuracionMs,
            registro.IdCorrelacion,
            registro.CodigoError ?? "-",
            registro.TipoExcepcion ?? "-");
    }
}
