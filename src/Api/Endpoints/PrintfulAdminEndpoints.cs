using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Dominio.Autenticacion;
using Mediator;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Consulta del catálogo de Printful para administradores (<c>specs/015-cliente-printful/contratos</c>).</summary>
public static class PrintfulAdminEndpoints
{
    public static IEndpointRouteBuilder MapearPrintfulAdmin(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/admin/printful").RequireAuthorization(p => p.RequireRole(Roles.Administrador));

        grupo.MapGet("/productos", async (string? categoria, int? pagina, int? tamano, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new ListarProductosQuery(categoria, pagina ?? 1, tamano ?? 20), c), h))
            .WithName("listarProductosPrintful").Produces<IReadOnlyList<ProductoPrintfulDto>>().Errores(400, 401, 403, 502, 503, 504);

        grupo.MapGet("/productos/{id:int}", async (int id, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new ObtenerProductoQuery(id), c), h))
            .WithName("obtenerProductoPrintful").Produces<ProductoPrintfulDto>().Errores(401, 403, 404, 502, 503, 504);

        grupo.MapGet("/productos/{id:int}/variantes", async (int id, string? tecnica, string? pais, ISender e, HttpContext h, CancellationToken c) =>
                Responder(await e.Send(new ListarVariantesQuery(id, tecnica, pais), c), h))
            .WithName("listarVariantesPrintful").Produces<IReadOnlyList<VariantePrintfulDto>>().Errores(400, 401, 403, 404, 502, 503, 504);

        return rutas;
    }

    private static IResult Responder<T>(BlackHellHeim.Aplicacion.Comun.CasosDeUso.ResultadoCasoDeUso<T> r, HttpContext h) =>
        r.EsExito ? Results.Ok(r.Valor) : TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");

    private static RouteHandlerBuilder Errores(this RouteHandlerBuilder b, params int[] estados)
    {
        foreach (var estado in estados.Append(500)) b.ProducesProblem(estado);
        return b;
    }
}
