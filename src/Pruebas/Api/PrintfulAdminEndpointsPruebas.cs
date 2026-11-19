using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Pruebas.Comun;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Historia 015: endpoints de administración con un proveedor doble (sin red).</summary>
public class PrintfulAdminEndpointsPruebas : IClassFixture<FabricaApiPruebas>
{
    private sealed class ProveedorDoble : IProveedorImpresion
    {
        public Func<ExcepcionCasoDeUso?> Fallo { get; set; } = () => null;
        public Task<IReadOnlyList<ProductoPrintfulDto>> ListarProductosAsync(string c, int p, int t, CancellationToken x) =>
            Fallar<IReadOnlyList<ProductoPrintfulDto>>() ?? Task.FromResult<IReadOnlyList<ProductoPrintfulDto>>([new(71, "Playera", "T-SHIRT", ["dtg"])]);
        public Task<ProductoPrintfulDto?> ObtenerProductoAsync(int id, CancellationToken x) =>
            Fallar<ProductoPrintfulDto?>() ?? Task.FromResult<ProductoPrintfulDto?>(id == 404 ? null : new(id, "Playera", "T-SHIRT", ["dtg"]));
        public Task<IReadOnlyList<VariantePrintfulDto>?> ListarVariantesAsync(int id, string t, string p, CancellationToken x) =>
            Fallar<IReadOnlyList<VariantePrintfulDto>?>() ?? Task.FromResult<IReadOnlyList<VariantePrintfulDto>?>(
                id == 404 ? null : [new(1, id, "Black", "#000000", "S", 12.5m, t, true)]);
        private Task<T>? Fallar<T>() => Fallo() is { } e ? Task.FromException<T>(e) : null;
    }

    private readonly ProveedorDoble _proveedor = new();
    private readonly FabricaApiPruebas _base;

    public PrintfulAdminEndpointsPruebas(FabricaApiPruebas fabrica) => _base = fabrica;

    private HttpClient Cliente(string? rol)
    {
        // La clave JWT es aleatoria por fábrica: el token se firma con la fábrica derivada que atiende la petición.
        var derivada = _base.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddScoped<IProveedorImpresion>(_ => _proveedor)));
        var cliente = derivada.CreateClient();
        if (rol is not null)
        {
            using var ambito = derivada.Services.CreateScope();
            var token = ambito.ServiceProvider.GetRequiredService<IEmisorTokens>()
                .CrearTokenAcceso(new UsuarioIdentidad(Guid.NewGuid(), "x@x.com", rol));
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return cliente;
    }

    [Fact]
    public async Task ElAdministradorListaProductosYVariantesFiltradas()
    {
        var cliente = Cliente("Administrador");

        var productos = await cliente.GetAsync("/admin/printful/productos");
        var variantes = await cliente.GetAsync("/admin/printful/productos/71/variantes?tecnica=dtg&pais=MX");
        var detalle = await cliente.GetAsync("/admin/printful/productos/71");

        Assert.Equal(HttpStatusCode.OK, productos.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        var cuerpo = JsonDocument.Parse(await variantes.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("S", cuerpo[0].GetProperty("talla").GetString());
        Assert.Equal(12.5m, cuerpo[0].GetProperty("costoBaseUsd").GetDecimal());
    }

    [Theory]
    [InlineData("?tecnica=tinta&pais=MX")]
    [InlineData("?tecnica=dtg&pais=mexico")]
    [InlineData("")]
    public async Task UnaTecnicaOPaisInvalidosDan400ConLosValoresAdmitidos(string consulta)
    {
        var r = await Cliente("Administrador").GetAsync("/admin/printful/productos/71/variantes" + consulta);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnProductoInexistenteDa404()
    {
        var cliente = Cliente("Administrador");

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/admin/printful/productos/404")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/admin/printful/productos/404/variantes?tecnica=dtg&pais=MX")).StatusCode);
    }

    [Fact]
    public async Task SinSesionDa401YComoClienteDa403()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Cliente(null).GetAsync("/admin/printful/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Cliente("Cliente").GetAsync("/admin/printful/productos")).StatusCode);
    }

    [Theory]
    [InlineData(TipoError.ProveedorSaturado, 503)]
    [InlineData(TipoError.ProveedorFallo, 502)]
    [InlineData(TipoError.ProveedorTiempoAgotado, 504)]
    public async Task LosErroresDelProveedorSeTraducenConSuEstado(TipoError tipo, int estado)
    {
        _proveedor.Fallo = () => new ExcepcionCasoDeUso(tipo switch
        {
            TipoError.ProveedorSaturado => ErrorUniforme.ProveedorSaturado(7, "c"),
            TipoError.ProveedorFallo => ErrorUniforme.ProveedorFallo("c"),
            _ => ErrorUniforme.ProveedorTiempoAgotado("c")
        });

        var r = await Cliente("Administrador").GetAsync("/admin/printful/productos/71");

        Assert.Equal(estado, (int)r.StatusCode);
        var cuerpo = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(tipo == TipoError.ProveedorSaturado, cuerpo.TryGetProperty("retryAfter", out var ra) && ra.GetInt32() == 7);
    }
}
