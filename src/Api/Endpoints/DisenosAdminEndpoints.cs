using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Disenos;
using BlackHellHeim.Dominio.Autenticacion;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Administración de diseños (<c>specs/016-dominio-diseno/contratos/openapi.yaml</c>).</summary>
public static class DisenosAdminEndpoints
{
    public static IEndpointRouteBuilder MapearDisenosAdmin(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/admin/disenos").RequireAuthorization(p => p.RequireRole(Roles.Administrador));

        grupo.MapGet("", async (string? estado, int? pagina, int? tamano, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new ListarDisenosQuery(estado, pagina ?? 1, tamano ?? 20), c), h, StatusCodes.Status200OK))
            .WithName("listarDisenos").Produces<PaginaDisenosDto>().Errores(400, 401, 403);

        grupo.MapPost("", async (DisenoSolicitud cuerpo, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new CrearDisenoComando(cuerpo), c), h, StatusCodes.Status201Created))
            .WithName("crearDiseno").Produces<DisenoDto>(StatusCodes.Status201Created).Errores(400, 401, 403, 409);

        grupo.MapGet("/{id:guid}", async (Guid id, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new ObtenerDisenoQuery(id), c), h, StatusCodes.Status200OK))
            .WithName("obtenerDiseno").Produces<DisenoDto>().Errores(401, 403, 404);

        grupo.MapPut("/{id:guid}", async (Guid id, [FromHeader(Name = "If-Match")] string? ifMatch, DisenoSolicitud cuerpo,
                ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new EditarDisenoComando(id, ifMatch, cuerpo), c), h, StatusCodes.Status200OK))
            .WithName("editarDiseno").Produces<DisenoDto>().Errores(400, 401, 403, 404, 409, 412);

        return rutas;
    }

    private static IResult Responder(ResultadoCasoDeUso<DisenoDto> r, HttpContext h, int estado) =>
        Resultado(r, h, estado, d => d.Version);

    private static IResult Responder(ResultadoCasoDeUso<PaginaDisenosDto> r, HttpContext h, int estado) =>
        Resultado(r, h, estado, _ => null);

    private static IResult Resultado<T>(ResultadoCasoDeUso<T> r, HttpContext h, int estado, Func<T, string?> version)
    {
        if (!r.EsExito) return TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");
        if (version(r.Valor!) is { } v) h.Response.Headers.ETag = $"\"{v}\"";
        return Results.Json(r.Valor, statusCode: estado);
    }

    private static RouteHandlerBuilder Errores(this RouteHandlerBuilder b, params int[] estados)
    {
        foreach (var estado in estados.Append(500)) b.ProducesProblem(estado);
        return b;
    }
}
