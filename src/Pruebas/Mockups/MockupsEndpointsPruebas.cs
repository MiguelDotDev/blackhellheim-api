using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Disenos;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Dominio.Mockups;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Dobles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BlackHellHeim.Pruebas.Mockups;

/// <summary>Historia 021 de extremo a extremo con dobles: endpoints, evento firmado de HU-22 y polling.</summary>
public class MockupsEndpointsPruebas(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static readonly string Secreto = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    private sealed record Sitio(IServiceProvider Servicios, Func<string?, HttpClient> Cliente);

    private Sitio Derivada(EscenarioMockups e, RepositorioEventosEnMemoria? eventos = null)
    {
        var f = fabrica.WithWebHostBuilder(b =>
        {
            b.UseSetting("Printful:SecretoWebhook", Secreto);
            b.UseSetting("Mockups:IntervaloPollingSegundos", "3600"); // el temporizador real no interfiere con las pruebas
            b.ConfigureServices(s =>
            {
                s.AddSingleton<IRepositorioMockups>(e.Repo);
                s.AddSingleton<IRepositorioDisenos>(e.Disenos);
                s.AddSingleton<IProveedorImpresion>(e.Proveedor);
                s.AddSingleton<IAlmacenArchivos>(e.Almacen);
                s.AddSingleton<TimeProvider>(e.Reloj);
                s.AddSingleton<IRepositorioEventos>(eventos ?? new RepositorioEventosEnMemoria());
            });
        });
        return new Sitio(f.Services, rol =>
        {
            var c = f.CreateClient();
            if (rol is not null)
            {
                using var ambito = f.Services.CreateScope();
                var token = ambito.ServiceProvider.GetRequiredService<IEmisorTokens>()
                    .CrearTokenAcceso(new UsuarioIdentidad(Guid.NewGuid(), "x@x.com", rol));
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return c;
        });
    }

    private static string Ruta(Guid id) => $"/admin/disenos/{id}/mockups";

    // --- POST / GET ---
    [Fact]
    public async Task PedirDa202ConLaTareaPendienteYElSegundoPedidoDa409()
    {
        var e = new EscenarioMockups();
        var c = Derivada(e).Cliente("Administrador");

        var primera = await c.PostAsync(Ruta(e.Diseno.Id), null);
        var segunda = await c.PostAsync(Ruta(e.Diseno.Id), null);

        Assert.Equal(HttpStatusCode.Accepted, primera.StatusCode);
        var cuerpo = JsonDocument.Parse(await primera.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Pendiente", cuerpo.GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        Assert.Equal("application/problem+json", segunda.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PedirSinArteDa400YDeDisenoInexistenteDa404()
    {
        var sinArte = new EscenarioMockups(conArte: false);
        var c = Derivada(sinArte).Cliente("Administrador");

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync(Ruta(sinArte.Diseno.Id), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync(Ruta(Guid.NewGuid()), null)).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Cliente", HttpStatusCode.Forbidden)]
    public async Task SinSesionOSinRolAdministradorSeRechazaPostYGet(string? rol, HttpStatusCode esperado)
    {
        var e = new EscenarioMockups();
        var c = Derivada(e).Cliente(rol);

        Assert.Equal(esperado, (await c.PostAsync(Ruta(e.Diseno.Id), null)).StatusCode);
        Assert.Equal(esperado, (await c.GetAsync(Ruta(e.Diseno.Id))).StatusCode);
        Assert.Equal(0, e.Proveedor.Creadas);
    }

    [Fact]
    public async Task SiPrintfulFallaAlPedirDa502()
    {
        var e = new EscenarioMockups();
        e.Proveedor.FalloCrear = () => new ExcepcionCasoDeUso(ErrorUniforme.ProveedorFallo("c"));
        var c = Derivada(e).Cliente("Administrador");

        Assert.Equal(HttpStatusCode.BadGateway, (await c.PostAsync(Ruta(e.Diseno.Id), null)).StatusCode);
    }

    [Fact]
    public async Task ListarDevuelve200ConLasTareasYDisenoInexistenteDa404()
    {
        var e = new EscenarioMockups();
        var c = Derivada(e).Cliente("Administrador");
        await c.PostAsync(Ruta(e.Diseno.Id), null);

        var lista = await c.GetAsync(Ruta(e.Diseno.Id));

        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.Equal(1, JsonDocument.Parse(await lista.Content.ReadAsStringAsync()).RootElement.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Ruta(Guid.NewGuid()))).StatusCode);
    }

    // --- Evento firmado (HU-22) → finalización ---
    private static Task<HttpResponseMessage> Evento(HttpClient c, string id, string tarea, string tipo = "mockup_task_finished")
    {
        var cuerpo = """{"occurred_at":"ID","type":"TIPO","data":{"mockup_task":{"id":"TAREA"}}}""".Replace("ID", id).Replace("TIPO", tipo).Replace("TAREA", tarea);
        var firma = Convert.ToHexString(HMACSHA256.HashData(Convert.FromHexString(Secreto), Encoding.UTF8.GetBytes(cuerpo))).ToLowerInvariant();
        var r = new HttpRequestMessage(HttpMethod.Post, "/webhooks/printful") { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };
        r.Headers.Add("X-Printful-Signature", firma);
        return c.SendAsync(r);
    }

    [Fact]
    public async Task ElEventoMockupTaskFinishedCompletaLaTareaYUnRepetidoNoDuplica()
    {
        var e = new EscenarioMockups();
        var f = Derivada(e);
        var c = f.Cliente("Administrador");
        await c.PostAsync(Ruta(e.Diseno.Id), null);
        e.Proveedor.Completar("task-1", (1, "front", EscenarioMockups.Png), (2, "front", EscenarioMockups.Png), (3, "front", EscenarioMockups.Png));

        var primero = await Evento(c, "ev-1", "task-1");
        var repetido = await Evento(c, "ev-1", "task-1");
        var otroEventoMismaTarea = await Evento(c, "ev-2", "task-1");

        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otroEventoMismaTarea.StatusCode);
        var tarea = JsonDocument.Parse(await (await c.GetAsync(Ruta(e.Diseno.Id))).Content.ReadAsStringAsync()).RootElement[0];
        Assert.Equal("Completada", tarea.GetProperty("estado").GetString());
        Assert.Equal(2, tarea.GetProperty("imagenes").GetArrayLength());
        Assert.Equal(2, e.Almacen.Blobs.Count);
    }

    [Fact]
    public async Task UnEventoDeUnaTareaDesconocidaOUnTipoAjenoSeAceptaSinEfectos()
    {
        var e = new EscenarioMockups();
        var c = Derivada(e).Cliente("Administrador");

        Assert.Equal(HttpStatusCode.OK, (await Evento(c, "ev-1", "no-existe")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Evento(c, "ev-2", "task-1", "package_shipped")).StatusCode);
        Assert.Equal(0, e.Proveedor.Consultas);
    }

    [Fact]
    public async Task SiPrintfulFallaAlFinalizarElEventoDa500ParaQueReintenteYElReintentoCompleta()
    {
        var e = new EscenarioMockups();
        var c = Derivada(e).Cliente("Administrador");
        await c.PostAsync(Ruta(e.Diseno.Id), null);
        e.Proveedor.Completar("task-1", (1, "front", EscenarioMockups.Png));
        e.Proveedor.FalloConsulta = _ => new ExcepcionCasoDeUso(ErrorUniforme.ProveedorFallo("c"));

        var falla = await Evento(c, "ev-1", "task-1");
        e.Proveedor.FalloConsulta = _ => null;
        var reintento = await Evento(c, "ev-1", "task-1");

        Assert.Equal(HttpStatusCode.InternalServerError, falla.StatusCode); // HU-22: el consumidor falló, Printful reintenta
        Assert.Equal(HttpStatusCode.OK, reintento.StatusCode);
        var tarea = JsonDocument.Parse(await (await c.GetAsync(Ruta(e.Diseno.Id))).Content.ReadAsStringAsync()).RootElement[0];
        Assert.Equal("Completada", tarea.GetProperty("estado").GetString());
    }

    // --- Polling de respaldo (US3) ---
    private static ServicioPollingMockups Polling(Sitio f) =>
        f.Servicios.GetServices<IHostedService>().OfType<ServicioPollingMockups>().Single();

    [Fact]
    public async Task ElPollingEsperaLaEsperaMinimaYLuegoCompletaSinNingunEvento()
    {
        var e = new EscenarioMockups();
        var f = Derivada(e);
        var c = f.Cliente("Administrador");
        await c.PostAsync(Ruta(e.Diseno.Id), null);
        e.Proveedor.Completar("task-1", (1, "front", EscenarioMockups.Png));

        e.Reloj.Ahora = e.Reloj.Ahora.AddSeconds(60);
        await Polling(f).CicloAsync(default);
        Assert.Equal(0, e.Proveedor.Consultas); // todavía dentro de los 2 min

        e.Reloj.Ahora = e.Reloj.Ahora.AddSeconds(61);
        await Polling(f).CicloAsync(default);
        var tarea = (await e.Repo.ListarPorDisenoAsync(e.Diseno.Id, default)).Single();
        Assert.Equal(EstadoTareaMockup.Completada, tarea.Estado);
        Assert.Single(tarea.Imagenes);
    }

    [Fact]
    public async Task ElPollingAgotaLasTareasQueNuncaTerminanYNoTocaLasYaFinalizadas()
    {
        var e = new EscenarioMockups();
        var f = Derivada(e);
        var c = f.Cliente("Administrador");
        await c.PostAsync(Ruta(e.Diseno.Id), null);
        e.Reloj.Ahora = e.Reloj.Ahora.AddMinutes(31);

        await Polling(f).CicloAsync(default);
        var consultas = e.Proveedor.Consultas;
        await Polling(f).CicloAsync(default);

        var tarea = (await e.Repo.ListarPorDisenoAsync(e.Diseno.Id, default)).Single();
        Assert.Equal(EstadoTareaMockup.Fallida, tarea.Estado);
        Assert.Equal(consultas, e.Proveedor.Consultas);
    }

    [Fact]
    public async Task UnaTareaQueFallaNoDetieneAlResto()
    {
        var e = new EscenarioMockups();
        var f = Derivada(e);
        var c = f.Cliente("Administrador");
        await c.PostAsync(Ruta(e.Diseno.Id), null);
        e.Proveedor.Completar("task-1", (1, "front", EscenarioMockups.Png));
        // Segunda tarea en otro diseño, para que ambas estén pendientes.
        var otro = new EscenarioMockups();
        e.Disenos.Disenos.Add(otro.Diseno);
        e.Repo.Disenos.Add(otro.Diseno.Id);
        await c.PostAsync(Ruta(otro.Diseno.Id), null);
        e.Proveedor.Completar("task-2", (1, "front", EscenarioMockups.Png));
        e.Proveedor.FalloConsulta = id => id == "task-1" ? new ExcepcionCasoDeUso(ErrorUniforme.ProveedorFallo("c")) : null;
        e.Reloj.Ahora = e.Reloj.Ahora.AddMinutes(3);

        await Polling(f).CicloAsync(default);

        var t1 = (await e.Repo.ListarPorDisenoAsync(e.Diseno.Id, default)).Single();
        var t2 = (await e.Repo.ListarPorDisenoAsync(otro.Diseno.Id, default)).Single();
        Assert.Equal(EstadoTareaMockup.Pendiente, t1.Estado);
        Assert.Equal(EstadoTareaMockup.Completada, t2.Estado);
    }
}
