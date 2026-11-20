using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Dominio.Mockups;
using BlackHellHeim.Infraestructura.Mockups;
using BlackHellHeim.Infraestructura.Persistencia;
using BlackHellHeim.Pruebas.Dobles;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlackHellHeim.Pruebas.Mockups;

/// <summary>CE-002 y el índice de «una pendiente por diseño» sobre SQL Server real (contenedor desechable).</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class MockupsSqlServerPruebas(ContenedorSqlServer sql)
{
    private static ContextoBlackHellHeim Contexto(string cadena) =>
        new(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);

    private async Task<(string Cadena, EscenarioMockups E)> BaseConDisenoAsync()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = Contexto(cadena);
        await contexto.Database.MigrateAsync();
        var e = new EscenarioMockups();
        contexto.Disenos.Add(e.Diseno);
        await contexto.SaveChangesAsync();
        return (cadena, e);
    }

    [Fact]
    public async Task DiezFinalizacionesConcurrentesDejanUnSoloConjuntoDeImagenes()
    {
        var (cadena, e) = await BaseConDisenoAsync();
        await using (var c = Contexto(cadena))
        {
            Assert.True(await new RepositorioMockups(c).AgregarAsync(TareaMockup.Crear(e.Diseno.Id, "task-1", e.Reloj.Ahora), default));
        }

        e.Proveedor.Completar("task-1", (1, "front", EscenarioMockups.Png), (3, "front", EscenarioMockups.Png));
        var tareaId = await sql.ConsultarEscalarAsync<Guid>(cadena, "SELECT Id FROM TareasMockup");

        var resultados = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await using var c = Contexto(cadena);
            var manejador = new ManejadorFinalizarTareaMockup(new RepositorioMockups(c), e.Disenos, e.Proveedor, e.Almacen, e.Opciones, e.Reloj, e.Contexto);
            return await manejador.Handle(new FinalizarTareaMockupComando(tareaId), default);
        })));

        Assert.All(resultados, r => Assert.Equal(EstadoTareaMockup.Completada, r.Valor));
        Assert.Equal(2, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM ImagenesMockup"));
        Assert.Equal("Completada", await sql.ConsultarEscalarAsync<string>(cadena, "SELECT Estado FROM TareasMockup"));
        Assert.Equal(2, e.Almacen.Blobs.Count); // nombres deterministas: los perdedores solo reescribieron
    }

    [Fact]
    public async Task ElIndiceFiltradoPermiteUnaSolaPendientePorDisenoPeroNoBloqueaLasTerminadas()
    {
        var (cadena, e) = await BaseConDisenoAsync();
        await using var c = Contexto(cadena);
        var repo = new RepositorioMockups(c);

        Assert.True(await repo.AgregarAsync(TareaMockup.Crear(e.Diseno.Id, "t1", e.Reloj.Ahora), default));
        Assert.False(await repo.AgregarAsync(TareaMockup.Crear(e.Diseno.Id, "t2", e.Reloj.Ahora), default)); // otra pendiente
        Assert.False(await repo.AgregarAsync(TareaMockup.Crear(Guid.NewGuid(), "t1", e.Reloj.Ahora), default)); // mismo id de Printful (y FK)

        var t1 = (await repo.ObtenerPorPrintfulIdAsync("t1", default))!;
        Assert.True(await repo.FallarAsync(t1.Id, "motivo", e.Reloj.Ahora, default));
        Assert.False(await repo.FallarAsync(t1.Id, "otra vez", e.Reloj.Ahora, default)); // ya no está pendiente
        Assert.True(await repo.AgregarAsync(TareaMockup.Crear(e.Diseno.Id, "t3", e.Reloj.Ahora), default));
        Assert.Equal(2, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM TareasMockup"));
        Assert.Equal("motivo", await sql.ConsultarEscalarAsync<string>(cadena, "SELECT Motivo FROM TareasMockup WHERE PrintfulTaskId = 't1'"));
    }

    [Fact]
    public async Task ElRepositorioListaPendientesPorAntiguedadYLasTareasConSusImagenes()
    {
        var (cadena, e) = await BaseConDisenoAsync();
        await using var c = Contexto(cadena);
        var repo = new RepositorioMockups(c);
        var tarea = TareaMockup.Crear(e.Diseno.Id, "t1", e.Reloj.Ahora);
        await repo.AgregarAsync(tarea, default);

        Assert.Empty(await repo.ListarPendientesAntesDeAsync(e.Reloj.Ahora.AddSeconds(-1), 10, default));
        Assert.Equal([tarea.Id], await repo.ListarPendientesAntesDeAsync(e.Reloj.Ahora, 10, default));

        var imagen = ImagenMockup.Crear(tarea.Id, "Black", "front", "https://x.test/a.png", "mockups/a.png");
        Assert.True(await repo.CompletarAsync(tarea.Id, [imagen], e.Reloj.Ahora, default));
        Assert.False(await repo.CompletarAsync(tarea.Id, [ImagenMockup.Crear(tarea.Id, "White", "front", "https://x.test/b.png", "mockups/b.png")], e.Reloj.Ahora, default));

        var lista = await new RepositorioMockups(Contexto(cadena)).ListarPorDisenoAsync(e.Diseno.Id, default);
        var leida = Assert.Single(lista);
        Assert.Equal(EstadoTareaMockup.Completada, leida.Estado);
        Assert.Equal("Black", Assert.Single(leida.Imagenes).Color);
        Assert.Empty(await repo.ListarPendientesAntesDeAsync(e.Reloj.Ahora, 10, default));
    }
}
