using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Comportamientos;
using BlackHellHeim.Aplicacion.Ejemplos;
using BlackHellHeim.Pruebas.Dobles;
using Xunit;

namespace BlackHellHeim.Pruebas.Aplicacion.Comportamientos;

public class CorrelacionBehaviorPruebas
{
    private static async Task<string> Ejecutar(ContextoEjecucionFalso contexto)
    {
        var comportamiento = new CorrelacionBehavior<EcoComando, ResultadoCasoDeUso<string>>(contexto);
        string? vista = null;

        await comportamiento.Handle(new EcoComando("x"), (m, c) =>
        {
            vista = contexto.IdCorrelacion;
            return ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito("ok"));
        }, CancellationToken.None);

        return vista!;
    }

    [Fact]
    public async Task ConservaLaCorrelacionExistente()
    {
        Assert.Equal("ya-existe", await Ejecutar(new ContextoEjecucionFalso { IdCorrelacion = "ya-existe" }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GeneraUnaCorrelacionSiEstaVacia(string vacia)
    {
        var generada = await Ejecutar(new ContextoEjecucionFalso { IdCorrelacion = vacia });

        Assert.False(string.IsNullOrWhiteSpace(generada));
    }

    [Fact]
    public async Task LasCorrelacionesGeneradasSonDistintas()
    {
        var a = await Ejecutar(new ContextoEjecucionFalso());
        var b = await Ejecutar(new ContextoEjecucionFalso());

        Assert.NotEqual(a, b);
    }
}
