using System.Security.Claims;
using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Precios;
using BlackHellHeim.Dominio.Autenticacion;
using Mediator;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Administración de precios (<c>specs/020-motor-precios/contratos/openapi.yaml</c>).</summary>
public static class PreciosAdminEndpoints
{
    public static IEndpointRouteBuilder MapearPreciosAdmin(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/admin/precios").RequireAuthorization(p => p.RequireRole(Roles.Administrador));

        grupo.MapGet("/parametros", async (ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new ObtenerParametrosPrecioQuery(), c), h))
            .WithName("obtenerParametrosPrecio").Produces<ParametrosPrecioDto>().Errores(401, 403);

        grupo.MapPut("/parametros", async (ParametrosPrecioSolicitud cuerpo, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new ActualizarParametrosPrecioComando(cuerpo, h.User.FindFirstValue("sub") ?? "desconocido"), c), h))
            .WithName("actualizarParametrosPrecio").Produces<ParametrosPrecioDto>().Errores(400, 401, 403);

        grupo.MapGet("/simular", async (Guid varianteId, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new SimularPrecioQuery(varianteId), c), h))
            .WithName("simularPrecio").Produces<DesglosePrecioDto>().Errores(400, 401, 403, 404, 503);

        return rutas;
    }

    private static IResult Responder<T>(ResultadoCasoDeUso<T> r, HttpContext h) =>
        r.EsExito ? Results.Json(r.Valor, statusCode: StatusCodes.Status200OK)
            : TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");

    private static RouteHandlerBuilder Errores(this RouteHandlerBuilder b, params int[] estados)
    {
        foreach (var estado in estados.Append(500)) b.ProducesProblem(estado);
        return b;
    }
}
