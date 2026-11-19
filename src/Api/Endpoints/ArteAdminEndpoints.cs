using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Dominio.Autenticacion;
using Mediator;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Subida de arte (<c>specs/017-subida-arte/contratos/openapi.yaml</c>).</summary>
public static class ArteAdminEndpoints
{
    public static IEndpointRouteBuilder MapearArteAdmin(this IEndpointRouteBuilder rutas)
    {
        rutas.MapPost("/admin/arte", async (HttpContext h, ISender e, Aplicacion.Arte.OpcionesArte opciones, CancellationToken c) =>
            {
                // Solo esta ruta admite cuerpos grandes; el tope real lo aplica el manejador mientras copia el flujo.
                h.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize = opciones.MaxBytes + (1024 * 1024);
                if (!MediaTypeHeaderValue.TryParse(h.Request.ContentType, out var tipo)
                    || !tipo.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)
                    || HeaderUtilities.RemoveQuotes(tipo.Boundary).Value is not { Length: > 0 } limite)
                    return Problema(h, 400, "arte-ilegible", "Se esperaba multipart/form-data con el campo «archivo».");

                var lector = new MultipartReader(limite, h.Request.Body);
                MultipartSection? seccion;
                while ((seccion = await lector.ReadNextSectionAsync(c)) is not null)
                {
                    if (!ContentDispositionHeaderValue.TryParse(seccion.ContentDisposition, out var cd)
                        || cd.Name.Value != "archivo" || cd.FileName.Value is null) continue;
                    // El nombre original solo sirve para validar la extensión; nunca forma una ruta.
                    var r = await e.Send(new SubirArteComando(seccion.Body, cd.FileName.Value, seccion.ContentType), c);
                    if (!r.EsExito) return TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");
                    return Results.Json(r.Valor, statusCode: StatusCodes.Status201Created);
                }

                return Problema(h, 400, "arte-ilegible", "Falta el campo «archivo».");
            })
            .RequireAuthorization(p => p.RequireRole(Roles.Administrador))
            .DisableAntiforgery().WithName("subirArte")
            .Produces<ArteDto>(StatusCodes.Status201Created)
            .Accepts<ArchivoSubida>("multipart/form-data")
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(413)
            .ProducesProblem(415).ProducesProblem(503).ProducesProblem(500);
        return rutas;
    }

    private static IResult Problema(HttpContext h, int estado, string codigo, string detalle) =>
        Results.Json(new Dictionary<string, object?>
        {
            ["type"] = $"https://blackhellheim.mx/errores/{codigo}", ["title"] = "Arte rechazado", ["status"] = estado,
            ["detail"] = detalle, ["instance"] = h.Request.Path.Value, ["codigo"] = codigo
        }, statusCode: estado, contentType: "application/problem+json");

    /// <summary>Solo describe el cuerpo multipart para OpenAPI.</summary>
    public sealed class ArchivoSubida { public required IFormFile Archivo { get; init; } }
}
