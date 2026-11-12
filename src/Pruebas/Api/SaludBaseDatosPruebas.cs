using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Contrato <c>GET /salud/base</c> de la historia 003 (base disponible y base cerrada).</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class SaludBaseDatosPruebas(ContenedorSqlServer sql)
{
    [Fact]
    public async Task ConLaBaseDisponibleRespondeDisponible()
    {
        await using var fabrica = FabricaApiPruebas.Con(await sql.CrearBaseAsync());
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud/base");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("disponible", cuerpo!["estado"]);
    }

    [Fact]
    public async Task ConUnPuertoCerradoRespondeProblemaYLaSaludDeVidaSigueEn200()
    {
        await using var fabrica = new FabricaApiPruebas();
        var cliente = fabrica.CreateClient();

        var cronometro = Stopwatch.StartNew();
        var respuesta = await cliente.GetAsync("/salud/base");
        cronometro.Stop();
        var vida = await cliente.GetAsync("/salud");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var texto = await respuesta.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(texto);
        Assert.Equal("base_no_disponible", doc.RootElement.GetProperty("codigo").GetString());
        Assert.Equal(respuesta.Headers.GetValues("X-Correlation-ID").Single(),
            doc.RootElement.GetProperty("idCorrelacion").GetString());
        Assert.DoesNotContain("127.0.0.1", texto);
        Assert.DoesNotContain("BlackHellHeimPruebas", texto);
        Assert.True(cronometro.Elapsed < TimeSpan.FromSeconds(5), $"Tardó {cronometro.Elapsed}.");
        Assert.Equal(HttpStatusCode.OK, vida.StatusCode);
    }

    [Fact]
    public async Task LaCabeceraDeCorrelacionSeConservaEnElFallo()
    {
        await using var fabrica = new FabricaApiPruebas();
        var cliente = fabrica.CreateClient();
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/salud/base");
        peticion.Headers.Add("X-Correlation-ID", "corr-base-1");

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("corr-base-1", respuesta.Headers.GetValues("X-Correlation-ID").Single());
    }
}

/// <summary>
/// Base real inaccesible: el contenedor se pausa (acepta TCP pero no responde). Usa su propio
/// contenedor para no afectar a las demás pruebas.
/// </summary>
public class SaludBaseDatosPausadaPruebas(ContenedorSqlServer sql) : IClassFixture<ContenedorSqlServer>
{
    [Fact]
    public async Task ConLaBasePausadaRespondeProblemaEntre25Y5SegundosYSeRecupera()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var fabrica = FabricaApiPruebas.Con(cadena);
        var cliente = fabrica.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/salud/base")).StatusCode);

        await sql.PausarAsync();
        try
        {
            var cronometro = Stopwatch.StartNew();
            var respuesta = await cliente.GetAsync("/salud/base");
            cronometro.Stop();
            var vida = await cliente.GetAsync("/salud");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
            Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
            using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            Assert.Equal("base_no_disponible", doc.RootElement.GetProperty("codigo").GetString());
            Assert.InRange(cronometro.Elapsed, TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(5));
            Assert.Equal(HttpStatusCode.OK, vida.StatusCode);
        }
        finally
        {
            await sql.ReanudarAsync();
        }

        HttpStatusCode ultimo = default;
        for (var intento = 0; intento < 15 && ultimo != HttpStatusCode.OK; intento++)
        {
            ultimo = (await cliente.GetAsync("/salud/base")).StatusCode;
            if (ultimo != HttpStatusCode.OK)
            {
                await Task.Delay(1000);
            }
        }

        Assert.Equal(HttpStatusCode.OK, ultimo);
    }

    [Fact]
    public async Task LaCadenaNoApareceEnLaRespuestaYLasSolicitudesConsecutivasNoSeAcumulan()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var fabrica = FabricaApiPruebas.Con(cadena);
        var cliente = fabrica.CreateClient();
        await cliente.GetAsync("/salud/base");

        await sql.PausarAsync();
        try
        {
            for (var i = 0; i < 2; i++)
            {
                var cronometro = Stopwatch.StartNew();
                var respuesta = await cliente.GetAsync("/salud/base");
                cronometro.Stop();

                Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
                Assert.True(cronometro.Elapsed < TimeSpan.FromSeconds(5), $"Solicitud {i + 1}: {cronometro.Elapsed}.");
                var visible = await respuesta.Content.ReadAsStringAsync()
                              + string.Join(";", respuesta.Headers.Select(h => string.Concat(h.Key, h.Value)));
                Assert.DoesNotContain(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cadena).Password, visible);
                Assert.DoesNotContain("Password", visible, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            await sql.ReanudarAsync();
        }
    }
}
