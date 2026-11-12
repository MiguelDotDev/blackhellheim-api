using System.Security.Claims;
using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using Mediator;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Cuerpo de registro e inicio de sesión (el rol nunca se acepta del cliente).</summary>
public sealed class CredencialesSolicitud
{
    public string? Correo { get; init; }

    public string? Contrasena { get; init; }
}

/// <summary>Endpoints de autenticación según <c>specs/013-autenticacion-jwt/contratos/openapi.yaml</c>.</summary>
public static class AuthEndpoints
{
    public const string CookieRefresco = "refresh";

    public static IEndpointRouteBuilder MapearAuth(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/auth");

        grupo.MapPost("/registro", async (CredencialesSolicitud cuerpo, ISender emisor, HttpContext http, CancellationToken c) =>
                Sesion(await emisor.Send(new RegistrarComando(cuerpo.Correo ?? "", cuerpo.Contrasena ?? ""), c), http, StatusCodes.Status201Created))
            .WithName("registrar")
            .Produces<SesionDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPost("/inicio-sesion", async (CredencialesSolicitud cuerpo, ISender emisor, HttpContext http, CancellationToken c) =>
                Sesion(await emisor.Send(new IniciarSesionComando(cuerpo.Correo ?? "", cuerpo.Contrasena ?? ""), c), http, StatusCodes.Status200OK))
            .WithName("iniciarSesion")
            .Produces<SesionDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status423Locked)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPost("/renovar", async (ISender emisor, HttpContext http, CancellationToken c) =>
                Sesion(await emisor.Send(new RenovarComando(http.Request.Cookies[CookieRefresco]), c), http, StatusCodes.Status200OK))
            .WithName("renovarSesion")
            .Produces<SesionDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPost("/cerrar-sesion", async (ISender emisor, HttpContext http, CancellationToken c) =>
            {
                await emisor.Send(new CerrarSesionComando(http.Request.Cookies[CookieRefresco]), c);
                BorrarCookie(http);
                return Results.NoContent();
            })
            .WithName("cerrarSesion")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/yo", async (ClaimsPrincipal usuario, ISender emisor, HttpContext http, CancellationToken c) =>
            {
                if (!Guid.TryParse(usuario.FindFirstValue("sub"), out var id))
                {
                    return Results.Unauthorized();
                }

                var resultado = await emisor.Send(new ObtenerPerfilQuery(id), c);
                return resultado.EsExito
                    ? Results.Ok(resultado.Valor)
                    : TraductorProblemas.Traducir(resultado.Error!, http.Request.Path.Value ?? "/");
            })
            .RequireAuthorization()
            .WithName("obtenerPerfil")
            .Produces<PerfilDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return rutas;
    }

    private static IResult Sesion(ResultadoCasoDeUso<SesionEmitida> resultado, HttpContext http, int estado)
    {
        if (!resultado.EsExito)
        {
            // Un fallo de renovación (reuso, caducado, inexistente) también borra la cookie.
            BorrarCookie(http);
            return TraductorProblemas.Traducir(resultado.Error!, http.Request.Path.Value ?? "/");
        }

        var emitida = resultado.Valor!;
        http.Response.Cookies.Append(CookieRefresco, emitida.TokenRefresco, Opciones(http, emitida.RefrescoExpiraUtc));
        http.Response.Headers.CacheControl = "no-store";
        return Results.Json(emitida.Sesion, statusCode: estado);
    }

    private static void BorrarCookie(HttpContext http) =>
        http.Response.Cookies.Delete(CookieRefresco, Opciones(http, null));

    // Secure siempre activo salvo en Development (HTTP local); HttpOnly y SameSite=Lax, Path=/.
    private static CookieOptions Opciones(HttpContext http, DateTimeOffset? expira) => new()
    {
        HttpOnly = true,
        Secure = !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expira
    };
}
