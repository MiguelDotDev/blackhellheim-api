using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlackHellHeim.Dominio.Autenticacion;
using BlackHellHeim.Infraestructura.Identidad;
using BlackHellHeim.Infraestructura.Persistencia;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>Historia 013: registro, inicio de sesión, refresh rotativo, cierre y roles, con SQL Server real.</summary>
[Collection(ColeccionSqlServer.Nombre)]
public class AutenticacionEndpointsPruebas(ContenedorSqlServer sql)
{
    private const string Contrasena = "contrasena-segura-1";

    private async Task<FabricaApiPruebas> CrearFabricaAsync()
    {
        var cadena = await sql.CrearBaseAsync();
        await using (var contexto = new ContextoBlackHellHeim(
                         new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options))
        {
            await contexto.Database.MigrateAsync();
        }

        return FabricaApiPruebas.Con(cadena);
    }

    private static Task<HttpResponseMessage> Post(HttpClient cliente, string ruta, object? cuerpo = null, string? cookie = null)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, ruta);
        if (cuerpo is not null) peticion.Content = JsonContent.Create(cuerpo);
        if (cookie is not null) peticion.Headers.Add("Cookie", cookie);
        return cliente.SendAsync(peticion);
    }

    /// <summary>Extrae "refresh=valor" del Set-Cookie para reenviarlo (el cliente de pruebas usa HTTP).</summary>
    private static string? Refresh(HttpResponseMessage r) =>
        r.Headers.TryGetValues("Set-Cookie", out var v)
            ? v.Select(c => c.Split(';')[0]).FirstOrDefault(c => c.StartsWith("refresh=") && c.Length > "refresh=".Length)
            : null;

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task<(string Acceso, string Cookie)> Registrar(HttpClient c, string correo = "ana@correo.com")
    {
        var r = await Post(c, "/auth/registro", new { correo, contrasena = Contrasena });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return ((await Json(r)).GetProperty("tokenAcceso").GetString()!, Refresh(r)!);
    }

    [Fact]
    public async Task RegistroCreaClienteEntregaAccesoYCookieSegura()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();

        var r = await Post(cliente, "/auth/registro", new { correo = "Ana@Correo.com", contrasena = Contrasena, rol = "Administrador" });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var cuerpo = await Json(r);
        Assert.Equal("Cliente", cuerpo.GetProperty("rol").GetString());
        Assert.Equal(900, cuerpo.GetProperty("expiraEnSegundos").GetInt32());
        Assert.False(cuerpo.TryGetProperty("tokenRefresco", out _));
        var cookie = string.Join(";", r.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.Contains("path=/", cookie);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo.GetProperty("tokenAcceso").GetString());
        var yo = await Json(await cliente.GetAsync("/auth/yo"));
        Assert.Equal("ana@correo.com", yo.GetProperty("correo").GetString());
        Assert.Equal("Cliente", yo.GetProperty("rol").GetString());
    }

    [Theory]
    [InlineData("no-es-correo", Contrasena)]
    [InlineData("a@b.com", "corta1")]
    [InlineData("a@b.com", "sololetraslargasaqui")]
    public async Task RegistroInvalidoDa400ConCampos(string correo, string contrasena)
    {
        await using var fabrica = await CrearFabricaAsync();

        var r = await Post(fabrica.CreateClient(), "/auth/registro", new { correo, contrasena });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.True((await Json(r)).TryGetProperty("errores", out _));
    }

    [Fact]
    public async Task RegistroSinCuerpoDa400()
    {
        await using var fabrica = await CrearFabricaAsync();

        var r = await Post(fabrica.CreateClient(), "/auth/registro");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task RegistroConCorreoRepetidoDa409SinFiltrarDatos()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        await Registrar(cliente);

        var r = await Post(cliente, "/auth/registro", new { correo = "ANA@correo.com", contrasena = Contrasena });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.DoesNotContain(Contrasena, await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InicioDeSesionCorrectoDa200YErroresUniformes()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        await Registrar(cliente);

        var ok = await Post(cliente, "/auth/inicio-sesion", new { correo = "ana@correo.com", contrasena = Contrasena });
        var mala = await Post(cliente, "/auth/inicio-sesion", new { correo = "ana@correo.com", contrasena = "equivocada-123" });
        var inexistente = await Post(cliente, "/auth/inicio-sesion", new { correo = "nadie@correo.com", contrasena = "equivocada-123" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.NotNull(Refresh(ok));
        Assert.Equal(HttpStatusCode.Unauthorized, mala.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal((await Json(mala)).GetProperty("detail").GetString(), (await Json(inexistente)).GetProperty("detail").GetString());
        Assert.Null(Refresh(mala));
    }

    [Fact]
    public async Task CincoFallosBloqueanLaCuentaAunConLaContrasenaCorrecta()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        await Registrar(cliente);
        for (var i = 0; i < 5; i++)
            await Post(cliente, "/auth/inicio-sesion", new { correo = "ana@correo.com", contrasena = "equivocada-123" });

        var r = await Post(cliente, "/auth/inicio-sesion", new { correo = "ana@correo.com", contrasena = Contrasena });

        Assert.Equal(HttpStatusCode.Locked, r.StatusCode);
        Assert.Equal("cuenta_bloqueada", (await Json(r)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task RenovarRotaYReusarRevocaLaFamiliaYBorraLaCookie()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        var (_, primera) = await Registrar(cliente);

        var rotada = await Post(cliente, "/auth/renovar", cookie: primera);
        var segunda = Refresh(rotada)!;
        var reuso = await Post(cliente, "/auth/renovar", cookie: primera);
        var hijo = await Post(cliente, "/auth/renovar", cookie: segunda);

        Assert.Equal(HttpStatusCode.OK, rotada.StatusCode);
        Assert.NotEqual(primera, segunda);
        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
        Assert.Contains(reuso.Headers.GetValues("Set-Cookie"), c => c.StartsWith("refresh=;") || c.Contains("expires=Thu, 01 Jan 1970"));
        Assert.Equal(HttpStatusCode.Unauthorized, hijo.StatusCode);
    }

    [Fact]
    public async Task RenovarSinCookieOManipuladaDa401()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(cliente, "/auth/renovar")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(cliente, "/auth/renovar", cookie: "refresh=manipulada")).StatusCode);
    }

    [Fact]
    public async Task DosRenovacionesSimultaneasConElMismoRefreshSoloUnaTieneExito()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        var (_, cookie) = await Registrar(cliente);

        var respuestas = await Task.WhenAll(Post(cliente, "/auth/renovar", cookie: cookie), Post(cliente, "/auth/renovar", cookie: cookie));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CerrarSesionRevocaYSinCookieTambienDa204()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        var (_, cookie) = await Registrar(cliente);

        Assert.Equal(HttpStatusCode.NoContent, (await Post(cliente, "/auth/cerrar-sesion", cookie: cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(cliente, "/auth/cerrar-sesion")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(cliente, "/auth/renovar", cookie: cookie)).StatusCode);
    }

    [Fact]
    public async Task RolesSinTokenDa401ClienteDa403YAdministradorDa204()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        var (accesoCliente, _) = await Registrar(cliente);
        await CrearAdministradorAsync(fabrica, "admin@correo.com");
        var admin = await Json(await Post(cliente, "/auth/inicio-sesion", new { correo = "admin@correo.com", contrasena = Contrasena }));
        Assert.Equal("Administrador", admin.GetProperty("rol").GetString());

        var sinToken = await cliente.GetAsync("/pruebas/solo-administrador");
        var comoCliente = await Con(cliente, accesoCliente);
        var comoAdmin = await Con(cliente, admin.GetProperty("tokenAcceso").GetString()!);

        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal("application/problem+json", sinToken.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Forbidden, comoCliente.StatusCode);
        Assert.Equal("application/problem+json", comoCliente.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NoContent, comoAdmin.StatusCode);

        static Task<HttpResponseMessage> Con(HttpClient c, string token)
        {
            var p = new HttpRequestMessage(HttpMethod.Get, "/pruebas/solo-administrador");
            p.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return c.SendAsync(p);
        }
    }

    [Fact]
    public async Task UnTokenManipuladoDa401EnElPerfil()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        var (acceso, _) = await Registrar(cliente);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso[..^3] + "abc");

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task LasRespuestasNuncaContienenHashesNiContrasenas()
    {
        await using var fabrica = await CrearFabricaAsync();
        var cliente = fabrica.CreateClient();
        var r = await Post(cliente, "/auth/registro", new { correo = "ana@correo.com", contrasena = Contrasena });
        var texto = await r.Content.ReadAsStringAsync();

        Assert.DoesNotContain(Contrasena, texto);
        Assert.DoesNotContain("hash", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ElAdministradorInicialSeSiembraDesdeLaConfiguracion()
    {
        var cadena = await sql.CrearBaseAsync();
        await using (var contexto = new ContextoBlackHellHeim(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options))
            await contexto.Database.MigrateAsync();
        await using var fabrica = FabricaApiPruebas.Con(cadena).WithWebHostBuilder(b =>
        {
            b.UseSetting("Administrador:Correo", "root@correo.com");
            b.UseSetting("Administrador:Contrasena", Contrasena);
        });
        _ = fabrica.CreateClient();

        var rol = "";
        for (var i = 0; i < 50 && rol == ""; i++)
        {
            await Task.Delay(100);
            rol = await sql.ConsultarEscalarAsync<string>(cadena,
                "SELECT ISNULL(MAX(r.Name), '') FROM AspNetUsers u JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE u.Email='root@correo.com'");
        }

        Assert.Equal("Administrador", rol);
    }

    [Fact]
    public void LaApiNoArrancaSinClaveJwtValida()
    {
        using var fabrica = new FabricaApiPruebas().WithWebHostBuilder(b => b.UseSetting("Jwt:Clave", "corta"));

        var excepcion = Assert.Throws<InvalidOperationException>(() => fabrica.CreateClient());
        Assert.Contains("Jwt__Clave", excepcion.Message);
        Assert.DoesNotContain("corta", excepcion.Message);
    }

    private static async Task CrearAdministradorAsync(FabricaApiPruebas fabrica, string correo)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        var usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var admin = new Usuario { Id = Guid.NewGuid(), UserName = correo, Email = correo };
        Assert.True((await usuarios.CreateAsync(admin, Contrasena)).Succeeded);
        Assert.True((await usuarios.AddToRoleAsync(admin, Roles.Administrador)).Succeeded);
    }
}
