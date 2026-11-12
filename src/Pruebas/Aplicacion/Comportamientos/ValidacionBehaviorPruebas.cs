using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Comportamientos;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Comun.Validacion;
using BlackHellHeim.Aplicacion.Ejemplos;
using BlackHellHeim.Pruebas.Dobles;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Comportamientos;

public class ValidacionBehaviorPruebas
{
    private static ValidacionBehavior<EcoComando, ResultadoCasoDeUso<string>> Crear(
        params IValidadorSolicitud<EcoComando>[] validadores) =>
        new(validadores, new ContextoEjecucionFalso { IdCorrelacion = "corr-v" });

    [Fact]
    public async Task SinValidadoresPasaAlManejador()
    {
        var invocado = false;

        var resultado = await Crear().Handle(new EcoComando("x"), (m, c) =>
        {
            invocado = true;
            return ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito("ok"));
        }, CancellationToken.None);

        Assert.True(invocado);
        Assert.Equal("ok", resultado.Valor);
    }

    [Fact]
    public async Task ConErroresLanzaExcepcionDeValidacionYNoInvocaAlManejador()
    {
        var invocado = false;
        var comportamiento = Crear(new ValidadorSolicitudFalso<EcoComando>(new ErrorCampo("texto", "Es obligatorio.")));

        var excepcion = await Assert.ThrowsAsync<ExcepcionValidacion>(async () =>
            await comportamiento.Handle(new EcoComando(""), (m, c) =>
            {
                invocado = true;
                return ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito("ok"));
            }, CancellationToken.None));

        Assert.False(invocado);
        Assert.Equal(TipoError.Validacion, excepcion.Error.Tipo);
        Assert.Equal("corr-v", excepcion.Error.IdCorrelacion);
        Assert.Equal(["Es obligatorio."], excepcion.Error.Campos!["texto"]);
    }

    [Fact]
    public async Task AcumulaLosErroresDeVariosValidadores()
    {
        var comportamiento = Crear(
            new ValidadorSolicitudFalso<EcoComando>(new ErrorCampo("texto", "Primero.")),
            new ValidadorSolicitudFalso<EcoComando>(new ErrorCampo("texto", "Segundo."), new ErrorCampo("otro", "Tercero.")));

        var excepcion = await Assert.ThrowsAsync<ExcepcionValidacion>(async () =>
            await comportamiento.Handle(new EcoComando(""), (m, c) =>
                ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito("ok")), CancellationToken.None));

        Assert.Equal(["Primero.", "Segundo."], excepcion.Error.Campos!["texto"]);
        Assert.Equal(["Tercero."], excepcion.Error.Campos["otro"]);
    }

    [Fact]
    public async Task ValidadoresSinErroresPasanAlManejador()
    {
        var comportamiento = Crear(new ValidadorSolicitudFalso<EcoComando>());

        var resultado = await comportamiento.Handle(new EcoComando("x"), (m, c) =>
            ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito("ok")), CancellationToken.None);

        Assert.True(resultado.EsExito);
    }
}
