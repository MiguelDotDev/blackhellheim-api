using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Dominio.Webhooks;
using BlackHellHeim.Infraestructura.Persistencia;
using BlackHellHeim.Infraestructura.Webhooks;
using BlackHellHeim.Pruebas.Dobles;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlackHellHeim.Pruebas.Webhooks;

/// <summary>CE-002: la unicidad en SQL Server real resuelve la concurrencia (contenedor desechable).</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class WebhooksSqlServerPruebas(ContenedorSqlServer sql)
{
    private const string Cuerpo = """{"id":"ev-1","type":"mockup_task_finished"}""";

    private static ContextoBlackHellHeim Contexto(string cadena) =>
        new(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);

    private static ManejadorRecibirWebhook Manejador(ContextoBlackHellHeim contexto, ConsumidorFalso consumidor) =>
        new(new RepositorioEventos(contexto), [consumidor], TimeProvider.System, new ContextoEjecucionFalso { IdCorrelacion = "c" });

    private async Task<string> BaseMigradaAsync()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var c = Contexto(cadena);
        await c.Database.MigrateAsync();
        return cadena;
    }

    [Fact]
    public async Task VeinteEntregasParalelasDelMismoEventoProducenUnSoloProcesamiento()
    {
        var cadena = await BaseMigradaAsync();
        var consumidor = new ConsumidorFalso("printful", "mockup_task_finished");

        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            await using var contexto = Contexto(cadena);
            return await Manejador(contexto, consumidor).Handle(new("printful", "ev-1", "mockup_task_finished", Cuerpo), default);
        })));

        Assert.All(resultados, r => Assert.True(r.EsExito));
        Assert.Equal(1, consumidor.Llamadas);
        Assert.Equal(1, resultados.Count(r => !r.Valor!.Repetido));
        Assert.Equal(1, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM EventosWebhook"));
        Assert.Equal("Procesado", await sql.ConsultarEscalarAsync<string>(cadena, "SELECT Estado FROM EventosWebhook"));
    }

    [Fact]
    public async Task UnEventoFallidoSeReintentaEnSqlServerYSoloUnLlamadorLoReclama()
    {
        var cadena = await BaseMigradaAsync();
        var consumidor = new ConsumidorFalso("printful", "mockup_task_finished") { Fallo = () => new InvalidOperationException("boom") };
        await using (var contexto = Contexto(cadena))
        {
            var r = await Manejador(contexto, consumidor).Handle(new("printful", "ev-1", "mockup_task_finished", Cuerpo), default);
            Assert.False(r.EsExito);
        }

        Assert.Equal("Fallido", await sql.ConsultarEscalarAsync<string>(cadena, "SELECT Estado FROM EventosWebhook"));
        Assert.Equal("InvalidOperationException", await sql.ConsultarEscalarAsync<string>(cadena, "SELECT Error FROM EventosWebhook"));

        consumidor.Fallo = () => null;
        var resultados = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await using var contexto = Contexto(cadena);
            return await Manejador(contexto, consumidor).Handle(new("printful", "ev-1", "mockup_task_finished", Cuerpo), default);
        })));

        Assert.All(resultados, r => Assert.True(r.EsExito));
        Assert.Equal(2, consumidor.Llamadas); // el intento fallido + un único reintento
        Assert.Equal("Procesado", await sql.ConsultarEscalarAsync<string>(cadena, "SELECT Estado FROM EventosWebhook"));
    }

    [Fact]
    public async Task ElIndiceUnicoImpideDuplicarProveedorEIdPeroPermiteOtroProveedor()
    {
        var cadena = await BaseMigradaAsync();
        await using var contexto = Contexto(cadena);
        var repo = new RepositorioEventos(contexto);
        var ahora = DateTimeOffset.UtcNow;

        Assert.True(await repo.AgregarAsync(EventoWebhook.Crear("printful", "x", "t", "{}", ahora), default));
        Assert.False(await repo.AgregarAsync(EventoWebhook.Crear("printful", "x", "t", "{}", ahora), default));
        Assert.True(await repo.AgregarAsync(EventoWebhook.Crear("stripe", "x", "t", "{}", ahora), default));
    }
}
