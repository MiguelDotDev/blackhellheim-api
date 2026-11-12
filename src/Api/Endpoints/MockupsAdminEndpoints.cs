using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Dominio.Autenticacion;
using Mediator;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Mockups asíncronos de un diseño (<c>specs/021-mockups-asincronos/contratos/openapi.yaml</c>).</summary>
public static class MockupsAdminEndpoints
{
    public static IEndpointRouteBuilder MapearMockupsAdmin(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/admin/disenos/{id:guid}/mockups").RequireAuthorization(p => p.RequireRole(Roles.Administrador));

        grupo.MapPost("", async (Guid id, ISender e, HttpContext h, CancellationToken c) =>
            {
                var r = await e.Send(new PedirMockupsComando(id), c);
                return r.EsExito ? Results.Json(r.Valor, statusCode: StatusCodes.Status202Accepted)
                    : TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");
            })
            .WithName("pedirMockups").Produces<TareaMockupDto>(StatusCodes.Status202Accepted)
            .Errores(400, 401, 403, 404, 409, 502, 503, 504);

        grupo.MapGet("", async (Guid id, ISender e, HttpContext h, CancellationToken c) =>
            {
                var r = await e.Send(new ListarMockupsQuery(id), c);
                return r.EsExito ? Results.Ok(r.Valor) : TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");
            })
            .WithName("listarMockups").Produces<IReadOnlyList<TareaMockupDto>>().Errores(401, 403, 404);

        return rutas;
    }

    private static RouteHandlerBuilder Errores(this RouteHandlerBuilder b, params int[] estados)
    {
        foreach (var estado in estados.Append(500)) b.ProducesProblem(estado);
        return b;
    }
}
