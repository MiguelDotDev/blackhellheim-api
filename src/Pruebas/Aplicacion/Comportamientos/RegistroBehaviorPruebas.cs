using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Comportamientos;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Ejemplos;
using BlackHellHeim.Pruebas.Dobles;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Comportamientos;

public class RegistroBehaviorPruebas
{
    private readonly RegistroEjecucionEnMemoria _registro = new();

    private RegistroBehavior<EcoComando, ResultadoCasoDeUso<string>> Crear() =>
        new(_registro, new ContextoEjecucionFalso { IdCorrelacion = "corr-r" });

    [Fact]
    public async Task RegistraUnaEjecucionExitosa()
    {
        var antes = DateTimeOffset.UtcNow;

        var resultado = await Crear().Handle(new EcoComando("x"),
            (m, c) => ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito("ok")), CancellationToken.None);

        Assert.Equal("ok", resultado.Valor);
        var registro = Assert.Single(_registro.Registros);
        Assert.Equal("EcoComando", registro.TipoSolicitud);
        Assert.True(registro.Exito);
        Assert.True(registro.DuracionMs >= 0);
        Assert.Equal("corr-r", registro.IdCorrelacion);
        Assert.True(registro.Momento >= antes);
        Assert.Null(registro.CodigoError);
        Assert.Null(registro.TipoExcepcion);
    }

    [Fact]
    public async Task RegistraUnResultadoDeFalloSinAlterarLaRespuesta()
    {
        var fallo = ResultadoCasoDeUso<string>.Fallo(ErrorUniforme.NoEncontrado("No existe.", "corr-r"));

        var resultado = await Crear().Handle(new EcoComando("x"), (m, c) => ValueTask.FromResult(fallo), CancellationToken.None);

        Assert.Same(fallo, resultado);
        var registro = Assert.Single(_registro.Registros);
        Assert.False(registro.Exito);
        Assert.Equal("recurso_no_encontrado", registro.CodigoError);
        Assert.Null(registro.TipoExcepcion);
    }

    [Fact]
    public async Task RegistraUnaExcepcionSinMensajeNiTrazaYLaRelanza()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Crear().Handle(new EcoComando("x"),
                (m, c) => throw new InvalidOperationException("secreto-interno"), CancellationToken.None));

        var registro = Assert.Single(_registro.Registros);
        Assert.False(registro.Exito);
        Assert.Equal("InvalidOperationException", registro.TipoExcepcion);
        Assert.Equal("error_inesperado", registro.CodigoError);
        Assert.DoesNotContain("secreto-interno", registro.ToString());
    }

    [Fact]
    public async Task RegistraElCodigoDeUnaExcepcionDeValidacion()
    {
        await Assert.ThrowsAsync<ExcepcionValidacion>(async () =>
            await Crear().Handle(new EcoComando("x"),
                (m, c) => throw new ExcepcionValidacion([new ErrorCampo("texto", "Mal.")], "corr-r"), CancellationToken.None));

        var registro = Assert.Single(_registro.Registros);
        Assert.Equal("solicitud_invalida", registro.CodigoError);
        Assert.Equal(nameof(ExcepcionValidacion), registro.TipoExcepcion);
    }

    [Fact]
    public async Task UnFalloDelRegistroNoAlteraLaRespuesta()
    {
        _registro.Falla = true;

        var resultado = await Crear().Handle(new EcoComando("x"),
            (m, c) => ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito("ok")), CancellationToken.None);

        Assert.Equal("ok", resultado.Valor);
    }

    [Fact]
    public async Task UnFalloDelRegistroNoOcultaLaExcepcionOriginal()
    {
        _registro.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Crear().Handle(new EcoComando("x"),
                (m, c) => throw new InvalidOperationException("original"), CancellationToken.None));
    }
}
