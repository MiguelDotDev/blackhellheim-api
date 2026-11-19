using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Infraestructura.Persistencia;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BlackHellHeim.Pruebas.Arte;

/// <summary>Historia 017: POST /admin/arte contra SQL Server real y un almacén en memoria.</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class ArteAdminEndpointsPruebas(ContenedorSqlServer sql)
{
    private async Task<(IAsyncDisposable Fabrica, HttpClient Cliente, AlmacenEnMemoria Almacen)> PrepararAsync(string? rol = "Administrador")
    {
        var cadena = await sql.CrearBaseAsync();
        await using (var c = new ContextoBlackHellHeim(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options))
            await c.Database.MigrateAsync();
        var almacen = new AlmacenEnMemoria();
        var fabrica = FabricaApiPruebas.Con(cadena).WithWebHostBuilder(b =>
        {
            b.UseSetting("Arte:MinAncho", "40").UseSetting("Arte:MinAlto", "50").UseSetting("Arte:MaxMegapixeles", "1")
                .UseSetting("Arte:MaxBytes", "1000000");
            b.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IAlmacenArchivos>(almacen)));
        });
        var cliente = fabrica.CreateClient();
        if (rol is not null)
        {
            using var ambito = fabrica.Services.CreateScope();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                ambito.ServiceProvider.GetRequiredService<IEmisorTokens>().CrearTokenAcceso(new UsuarioIdentidad(Guid.NewGuid(), "a@a.com", rol)));
        }

        return (fabrica, cliente, almacen);
    }

    private static MultipartFormDataContent Formulario(byte[] bytes, string nombre = "arte.png", string tipo = "image/png", string campo = "archivo")
    {
        var contenido = new ByteArrayContent(bytes);
        contenido.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        return new MultipartFormDataContent { { contenido, campo, nombre } };
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();

    [Fact]
    public async Task UnArteValidoDa201ConUrlPublicaYSeDeduplica()
    {
        var (fabrica, cliente, almacen) = await PrepararAsync();
        await using var _ = fabrica;
        var png = GeneradorPng.Crear(40, 50);

        var r = await cliente.PostAsync("/admin/arte", Formulario(png));
        var otra = await cliente.PostAsync("/admin/arte", Formulario(png));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var cuerpo = await Json(r);
        Assert.StartsWith("https://almacen.test/arte/", cuerpo.GetProperty("url").GetString());
        Assert.Equal(40, cuerpo.GetProperty("ancho").GetInt32());
        Assert.False(cuerpo.GetProperty("reutilizado").GetBoolean());
        var segunda = await Json(otra);
        Assert.True(segunda.GetProperty("reutilizado").GetBoolean());
        Assert.Equal(cuerpo.GetProperty("id").GetGuid(), segunda.GetProperty("id").GetGuid());
        Assert.Single(almacen.Blobs);
    }

    public static IEnumerable<object[]> Rechazos()
    {
        yield return [GeneradorPng.Crear(40, 50, transparente: false), "a.png", "image/png", HttpStatusCode.BadRequest, "arte-sin-transparencia"];
        yield return [GeneradorPng.Crear(20, 20), "a.png", "image/png", HttpStatusCode.BadRequest, "arte-resolucion-insuficiente"];
        yield return [new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "a.png", "image/png", HttpStatusCode.UnsupportedMediaType, "arte-no-png"];
        yield return [GeneradorPng.Crear(40, 50), "a.jpg", "image/jpeg", HttpStatusCode.UnsupportedMediaType, "arte-no-png"];
        yield return [Array.Empty<byte>(), "a.png", "image/png", HttpStatusCode.BadRequest, "arte-ilegible"];
        yield return [new byte[1_000_001], "a.png", "image/png", HttpStatusCode.RequestEntityTooLarge, "arte-demasiado-grande"];
    }

    [Theory]
    [MemberData(nameof(Rechazos))]
    public async Task LosRechazosTienenCodigoEstable(byte[] bytes, string nombre, string tipo, HttpStatusCode estado, string codigo)
    {
        var (fabrica, cliente, almacen) = await PrepararAsync();
        await using var _ = fabrica;

        var r = await cliente.PostAsync("/admin/arte", Formulario(bytes, nombre, tipo));

        Assert.Equal(estado, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType!.MediaType);
        Assert.Equal(codigo, (await Json(r)).GetProperty("codigo").GetString());
        Assert.Empty(almacen.Blobs);
    }

    [Fact]
    public async Task SinElCampoArchivoDa400YSinMultipartDa415()
    {
        var (fabrica, cliente, _) = await PrepararAsync();
        await using var _ = fabrica;

        var sinCampo = await cliente.PostAsync("/admin/arte", Formulario([1], campo: "otro"));
        var sinMultipart = await cliente.PostAsync("/admin/arte", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, sinCampo.StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, sinMultipart.StatusCode); // lo rechaza el enrutado: el contrato solo acepta multipart
    }

    [Fact]
    public async Task SiElAlmacenFallaDa503()
    {
        var (fabrica, cliente, almacen) = await PrepararAsync();
        await using var _ = fabrica;
        almacen.Falla = true;

        var r = await cliente.PostAsync("/admin/arte", Formulario(GeneradorPng.Crear(40, 50)));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal("almacen_no_disponible", (await Json(r)).GetProperty("codigo").GetString());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Cliente", HttpStatusCode.Forbidden)]
    public async Task SinSesionOSinRolAdministradorDa401O403(string? rol, HttpStatusCode esperado)
    {
        var (fabrica, cliente, _) = await PrepararAsync(rol);
        await using var _ = fabrica;

        var r = await cliente.PostAsync("/admin/arte", Formulario(GeneradorPng.Crear(40, 50)));

        Assert.Equal(esperado, r.StatusCode);
    }
}
