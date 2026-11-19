using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Aplicacion.Precios;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Dominio.Precios;
using BlackHellHeim.Infraestructura.Persistencia;
using BlackHellHeim.Infraestructura.Precios;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BlackHellHeim.Pruebas.Precios;

/// <summary>Historia 020 contra SQL Server real: unicidad por día, concurrencia, semilla y endpoints.</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class PreciosSqlPruebas(ContenedorSqlServer sql)
{
    private static readonly DateOnly Hoy = new(2026, 11, 11);

    private async Task<string> BaseMigradaAsync()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var c = Nuevo(cadena);
        await c.Database.MigrateAsync();
        return cadena;
    }

    private static ContextoBlackHellHeim Nuevo(string cadena) => new(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);

    [Fact]
    public async Task LaMigracionSiembraLosParametrosIniciales()
    {
        var cadena = await BaseMigradaAsync();
        await using var c = Nuevo(cadena);

        var p = await new RepositorioPrecios(c).ObtenerParametrosVigentesAsync(default);

        Assert.Equal((0.03m, 0.40m, 0.036m, 0m), (p.ColchonCambiario, p.Margen, p.ComisionPasarela, p.ImpresionExtraUsd));
    }

    [Fact]
    public async Task UnaSegundaFilaDeLaMismaFechaNoSeGuarda()
    {
        var cadena = await BaseMigradaAsync();
        await using var c1 = Nuevo(cadena);
        await using var c2 = Nuevo(cadena);

        var primera = await new RepositorioPrecios(c1).GuardarTipoCambioAsync(TipoCambioDiario.Crear(Hoy, 18.1m, "a", DateTimeOffset.UtcNow), default);
        var segunda = await new RepositorioPrecios(c2).GuardarTipoCambioAsync(TipoCambioDiario.Crear(Hoy, 18.2m, "b", DateTimeOffset.UtcNow), default);

        Assert.True(primera);
        Assert.False(segunda);
        Assert.Equal(1, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM dbo.TiposCambioDiarios"));
    }

    [Fact]
    public async Task DiezPeticionesConcurrentesDejanUnaSolaFilaPorDia()
    {
        var cadena = await BaseMigradaAsync();
        var reloj = new RelojFijo(new DateTimeOffset(2026, 11, 11, 15, 0, 0, TimeSpan.Zero));
        var proveedor = new ProveedorTipoCambioFalso { Tasa = new TasaProveedor(Hoy, 18.5m) };

        var resultados = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var c = Nuevo(cadena);
            return await new ServicioTipoCambio(proveedor, new RepositorioPrecios(c), reloj, new AdvertenciasFalsas()).ObtenerDelDiaAsync(default);
        }));

        Assert.All(resultados, r => Assert.Equal(18.5m, r!.Valor));
        Assert.Equal(1, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM dbo.TiposCambioDiarios"));
    }

    private async Task<(IAsyncDisposable Fabrica, HttpClient Cliente, ProveedorTipoCambioFalso Proveedor, Guid Variante)> PrepararAsync(string? rol = "Administrador")
    {
        var cadena = await BaseMigradaAsync();
        Guid variante;
        await using (var c = Nuevo(cadena))
        {
            var producto = ProductoBase.Crear(71, "Playera", "T-SHIRT");
            var v = Variante.Crear(producto, 4011, "Black", "#000000", "M", 10m);
            c.Variantes.Add(v);
            await c.SaveChangesAsync();
            variante = v.Id;
        }

        var proveedor = new ProveedorTipoCambioFalso { Tasa = new TasaProveedor(ServicioTipoCambio.Hoy(TimeProvider.System), 18m) };
        var fabrica = FabricaApiPruebas.Con(cadena).WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IProveedorTipoCambio>(proveedor))));
        var cliente = fabrica.CreateClient();
        if (rol is not null)
        {
            using var ambito = fabrica.Services.CreateScope();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                ambito.ServiceProvider.GetRequiredService<IEmisorTokens>().CrearTokenAcceso(new UsuarioIdentidad(Guid.NewGuid(), "a@a.com", rol)));
        }

        return (fabrica, cliente, proveedor, variante);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();

    [Fact]
    public async Task SimularDevuelveElDesgloseDelEjemploDeLaSpec()
    {
        var (fabrica, cliente, _, variante) = await PrepararAsync();
        await using var _ = fabrica;

        var r = await cliente.GetAsync($"/admin/precios/simular?varianteId={variante}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var d = await Json(r);
        Assert.Equal(329m, d.GetProperty("precioFinalMxn").GetDecimal());
        Assert.Equal(185.4m, d.GetProperty("costoMxn").GetDecimal());
        Assert.False(d.GetProperty("tipoCambioObsoleto").GetBoolean());
    }

    [Fact]
    public async Task SimularSinTipoCambioDa503YVarianteInexistenteDa404()
    {
        var (fabrica, cliente, proveedor, variante) = await PrepararAsync();
        await using var _ = fabrica;
        proveedor.Falla = true;

        var sinTasa = await cliente.GetAsync($"/admin/precios/simular?varianteId={variante}");
        var inexistente = await cliente.GetAsync($"/admin/precios/simular?varianteId={Guid.NewGuid()}");
        var malId = await cliente.GetAsync("/admin/precios/simular?varianteId=abc");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, sinTasa.StatusCode);
        Assert.Equal("tipo_cambio_no_disponible", (await Json(sinTasa)).GetProperty("codigo").GetString());
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malId.StatusCode);
    }

    [Fact]
    public async Task ActualizarParametrosCambiaElPrecioSimulado()
    {
        var (fabrica, cliente, _, variante) = await PrepararAsync();
        await using var _ = fabrica;

        var put = await cliente.PutAsJsonAsync("/admin/precios/parametros",
            new { colchonCambiario = 0.0, margen = 0.5, comisionPasarela = 0.0, impresionExtraUsd = 0.0 });
        var leidos = await Json(await cliente.GetAsync("/admin/precios/parametros"));
        var simulado = await Json(await cliente.GetAsync($"/admin/precios/simular?varianteId={variante}"));

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(0.5m, leidos.GetProperty("margen").GetDecimal());
        Assert.Equal(369m, simulado.GetProperty("precioFinalMxn").GetDecimal()); // 10 × 18 ÷ 0.5 = 360 → 369
    }

    [Theory]
    [InlineData("{\"colchonCambiario\":0.03,\"margen\":0.6,\"comisionPasarela\":0.4,\"impresionExtraUsd\":0}")]
    [InlineData("{\"colchonCambiario\":0.03,\"margen\":0.4}")]
    [InlineData("{\"colchonCambiario\":2,\"margen\":0.4,\"comisionPasarela\":0.03,\"impresionExtraUsd\":0}")]
    public async Task UnosParametrosInvalidosDan400YNoCambianNada(string cuerpo)
    {
        var (fabrica, cliente, _, _) = await PrepararAsync();
        await using var _ = fabrica;

        var r = await cliente.PutAsync("/admin/precios/parametros", new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(0.4m, (await Json(await cliente.GetAsync("/admin/precios/parametros"))).GetProperty("margen").GetDecimal());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Cliente", HttpStatusCode.Forbidden)]
    public async Task SinSesionOSinRolAdministradorDa401O403(string? rol, HttpStatusCode esperado)
    {
        var (fabrica, cliente, _, variante) = await PrepararAsync(rol);
        await using var _ = fabrica;

        foreach (var ruta in new[] { "/admin/precios/parametros", $"/admin/precios/simular?varianteId={variante}" })
            Assert.Equal(esperado, (await cliente.GetAsync(ruta)).StatusCode);
        Assert.Equal(esperado, (await cliente.PutAsJsonAsync("/admin/precios/parametros", new { })).StatusCode);
    }
}
