using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Infraestructura.Persistencia;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Historia 016: endpoints de administración de diseños contra SQL Server real.</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class DisenosAdminEndpointsPruebas(ContenedorSqlServer sql)
{
    private async Task<(FabricaApiPruebas Fabrica, HttpClient Cliente, string Cadena)> PrepararAsync(string? rol = "Administrador", string? termino = null)
    {
        var cadena = await sql.CrearBaseAsync();
        await using (var c = new ContextoBlackHellHeim(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options))
            await c.Database.MigrateAsync();
        var fabrica = FabricaApiPruebas.Con(cadena);
        var factoria = termino is null ? fabrica : fabrica.WithWebHostBuilder(b => b.UseSetting("Disenos:TerminosBloqueados:0", termino));
        var cliente = factoria.CreateClient();
        if (rol is not null)
        {
            using var ambito = factoria.Services.CreateScope();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                ambito.ServiceProvider.GetRequiredService<IEmisorTokens>().CrearTokenAcceso(new UsuarioIdentidad(Guid.NewGuid(), "a@a.com", rol)));
        }

        return (fabrica, cliente, cadena);
    }

    private static object Var(int id, string talla = "S") => new
    {
        printfulVarianteId = id, printfulProductoId = 71, nombreProducto = "Playera", color = "Black", codigoColor = "#000000", talla, costoBaseUsd = 12.5
    };

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();

    private static HttpRequestMessage Put(Guid id, object cuerpo, string? version)
    {
        var p = new HttpRequestMessage(HttpMethod.Put, $"/admin/disenos/{id}") { Content = JsonContent.Create(cuerpo) };
        if (version is not null) p.Headers.TryAddWithoutValidation("If-Match", version);
        return p;
    }

    [Fact]
    public async Task CrearDevuelve201ConEtagYSePuedeConsultar()
    {
        var (fabrica, cliente, _) = await PrepararAsync();
        await using var _ = fabrica;

        var r = await cliente.PostAsJsonAsync("/admin/disenos", new { nombre = "Calavera épica", descripcion = "d", variantes = new[] { Var(1), Var(2, "M") } });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var d = await Json(r);
        Assert.Equal("Borrador", d.GetProperty("estado").GetString());
        Assert.Equal("calavera-epica", d.GetProperty("slug").GetString());
        Assert.Equal(2, d.GetProperty("variantes").GetArrayLength());
        Assert.Equal($"\"{d.GetProperty("version").GetString()}\"", r.Headers.ETag!.Tag);
        var leido = await Json(await cliente.GetAsync($"/admin/disenos/{d.GetProperty("id").GetGuid()}"));
        Assert.Equal(d.GetProperty("version").GetString(), leido.GetProperty("version").GetString());
    }

    [Theory]
    [InlineData("{\"nombre\":\"ab\"}")]
    [InlineData("{\"nombre\":\"Diseño\",\"estado\":\"Raro\"}")]
    [InlineData("{\"nombre\":\"Diseño\",\"variantes\":[{\"printfulVarianteId\":0,\"printfulProductoId\":71,\"color\":\"a\",\"codigoColor\":\"b\",\"talla\":\"S\",\"costoBaseUsd\":1}]}")]
    public async Task UnaSolicitudInvalidaDa400(string json)
    {
        var (fabrica, cliente, _) = await PrepararAsync();
        await using var _ = fabrica;

        var r = await cliente.PostAsync("/admin/disenos", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task NombreRepetidoYPublicarSinArteDan409()
    {
        var (fabrica, cliente, _) = await PrepararAsync();
        await using var _ = fabrica;
        await cliente.PostAsJsonAsync("/admin/disenos", new { nombre = "Calavera" });

        var repetido = await cliente.PostAsJsonAsync("/admin/disenos", new { nombre = "calavera" });
        var sinArte = await cliente.PostAsJsonAsync("/admin/disenos", new { nombre = "Otro", estado = "Publicado", variantes = new[] { Var(1) } });

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, sinArte.StatusCode);
    }

    [Fact]
    public async Task EditarConIfMatchCorrectoFuncionaYConUnaVersionVencidaDa412()
    {
        var (fabrica, cliente, _) = await PrepararAsync();
        await using var _ = fabrica;
        var creado = await Json(await cliente.PostAsJsonAsync("/admin/disenos", new { nombre = "Calavera" }));
        var id = creado.GetProperty("id").GetGuid();
        var v1 = creado.GetProperty("version").GetString()!;

        var ok = await cliente.SendAsync(Put(id, new { nombre = "Calavera dos", variantes = new[] { Var(1) } }, $"\"{v1}\""));
        var vencido = await cliente.SendAsync(Put(id, new { nombre = "Calavera tres" }, $"\"{v1}\""));
        var sinCabecera = await cliente.SendAsync(Put(id, new { nombre = "Calavera tres" }, null));

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.NotEqual(v1, (await Json(ok)).GetProperty("version").GetString());
        Assert.Equal(HttpStatusCode.PreconditionFailed, vencido.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sinCabecera.StatusCode);
    }

    [Fact]
    public async Task ConArteYVariantesSePublicaYSeListaPorEstado()
    {
        var (fabrica, cliente, cadena) = await PrepararAsync();
        await using var _ = fabrica;
        var arteId = Guid.NewGuid();
        await sql.ConsultarEscalarAsync<int>(cadena,
            $"INSERT INTO ArchivosArte (Id,Url,NombreBlob,TipoContenido,TamanoBytes,Ancho,Alto,HashSha256,CreadoUtc) VALUES ('{arteId}','http://x/a.png','a.png','image/png',10,4500,5400,'{new string('b', 64)}',SYSDATETIMEOFFSET()); SELECT 1");

        var r = await cliente.PostAsJsonAsync("/admin/disenos",
            new { nombre = "Calavera", archivoArteId = arteId, estado = "Publicado", variantes = new[] { Var(1), Var(2, "M"), Var(3, "L") } });
        var publicados = await Json(await cliente.GetAsync("/admin/disenos?estado=Publicado"));
        var borradores = await Json(await cliente.GetAsync("/admin/disenos?estado=Borrador"));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal("http://x/a.png", (await Json(r)).GetProperty("archivoArteUrl").GetString());
        Assert.Equal(1, publicados.GetProperty("total").GetInt32());
        Assert.Equal(0, borradores.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task UnArteInexistenteDa400()
    {
        var (fabrica, cliente, _) = await PrepararAsync();
        await using var _ = fabrica;

        var r = await cliente.PostAsJsonAsync("/admin/disenos", new { nombre = "Calavera", archivoArteId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task ObtenerInexistenteDa404()
    {
        var (fabrica, cliente, _) = await PrepararAsync();
        await using var _ = fabrica;

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/admin/disenos/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task SinSesionDa401YComoClienteDa403()
    {
        var (f1, sinSesion, _) = await PrepararAsync(rol: null);
        var (f2, comoCliente, _) = await PrepararAsync(rol: "Cliente");
        await using var _1 = f1;
        await using var _2 = f2;

        Assert.Equal(HttpStatusCode.Unauthorized, (await sinSesion.GetAsync("/admin/disenos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await comoCliente.PostAsJsonAsync("/admin/disenos", new { nombre = "Calavera" })).StatusCode);
    }

    [Fact]
    public async Task UnTerminoBloqueadoSoloAvisa()
    {
        var (fabrica, cliente, _) = await PrepararAsync(termino: "metallica");
        await using var _ = fabrica;

        var r = await cliente.PostAsJsonAsync("/admin/disenos", new { nombre = "Estilo Metallica" });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(1, (await Json(r)).GetProperty("avisos").GetArrayLength());
    }
}
