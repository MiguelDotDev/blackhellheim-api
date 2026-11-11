using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Comportamientos;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Ejemplos;
using BlackHellHeim.Pruebas.Dobles;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Comportamientos;

public class ErroresBehaviorPruebas
{
    private static ErroresBehavior<EcoComando, ResultadoCasoDeUso<string>> Crear() =>
        new(new ContextoEjecucionFalso { IdCorrelacion = "corr-e" });

    [Fact]
    public async Task SinFallosDevuelveLaRespuestaTalCual()
    {
        var esperado = ResultadoCasoDeUso<string>.Exito("ok");

        var resultado = await Crear().Handle(new EcoComando("x"), (m, c) => ValueTask.FromResult(esperado), CancellationToken.None);

        Assert.Same(esperado, resultado);
    }

    [Fact]
    public async Task UnErrorNoControladoSeConvierteEnInesperadoGenericoConLaCorrelacion()
    {
        var excepcion = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(async () =>
            await Crear().Handle(new EcoComando("x"),
                (m, c) => throw new InvalidOperationException("secreto-interno en C:\\ruta\\Clase.cs"), CancellationToken.None));

        Assert.Equal(TipoError.Inesperado, excepcion.Error.Tipo);
        Assert.Equal("error_inesperado", excepcion.Error.Codigo);
        Assert.Equal("corr-e", excepcion.Error.IdCorrelacion);
        Assert.DoesNotContain("secreto-interno", excepcion.Error.Mensaje);
        Assert.DoesNotContain(".cs", excepcion.Error.Mensaje);
    }

    [Fact]
    public async Task LaCancelacionSeConvierteEnCancelado()
    {
        var excepcion = await Assert.ThrowsAsync<ExcepcionCasoDeUso>(async () =>
            await Crear().Handle(new EcoComando("x"),
                (m, c) => throw new OperationCanceledException(), CancellationToken.None));

        Assert.Equal(TipoError.Cancelado, excepcion.Error.Tipo);
        Assert.Equal("operacion_cancelada", excepcion.Error.Codigo);
    }

    [Fact]
    public async Task LasExcepcionesYaUniformesPasanSinCambios()
    {
        var original = new ExcepcionValidacion([new ErrorCampo("texto", "Mal.")], "corr-e");

        var capturada = await Assert.ThrowsAsync<ExcepcionValidacion>(async () =>
            await Crear().Handle(new EcoComando("x"), (m, c) => throw original, CancellationToken.None));

        Assert.Same(original, capturada);
    }
}
