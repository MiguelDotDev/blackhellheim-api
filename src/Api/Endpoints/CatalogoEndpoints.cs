using System.Security.Cryptography;
using System.Text.Json;
using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Catalogo;
using Mediator;
using Microsoft.Net.Http.Headers;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Catálogo público (<c>specs/019-api-catalogo/contratos/openapi.yaml</c>): sin sesión, con ETag y caché corto.</summary>
public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapearCatalogo(this IEndpointRouteBuilder rutas)
    {
        rutas.MapGet("/catalogo/playeras", async (int? pagina, int? tamano, string? texto, string? color, string? talla,
                decimal? precioMin, decimal? precioMax, string? orden, ISender e, HttpContext h, CancellationToken c) =>
            {
                var r = await e.Send(new ListarCatalogoQuery(pagina, tamano, texto, color, talla, precioMin, precioMax, orden), c);
                return r.EsExito ? Cacheable(h, r.Valor!) : TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");
            })
            .AllowAnonymous().WithName("listarPlayeras").Produces<PaginaPlayerasDto>().Produces(304)
            .ProducesProblem(400).ProducesProblem(500);

        rutas.MapGet("/catalogo/playeras/{slug}", async (string slug, ISender e, HttpContext h, CancellationToken c) =>
            {
                var r = await e.Send(new ObtenerPlayeraQuery(slug), c);
                return r.EsExito ? Cacheable(h, r.Valor!) : TraductorProblemas.Traducir(r.Error!, h.Request.Path.Value ?? "/");
            })
            .AllowAnonymous().WithName("obtenerPlayera").Produces<PlayeraDetalleDto>().Produces(304)
            .ProducesProblem(404).ProducesProblem(500);
        return rutas;
    }

    private static IResult Cacheable<T>(HttpContext h, T valor)
    {
        var cuerpo = JsonSerializer.SerializeToUtf8Bytes(valor, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var etag = $"\"{Convert.ToHexString(SHA256.HashData(cuerpo))[..32].ToLowerInvariant()}\"";
        h.Response.Headers.ETag = etag;
        h.Response.Headers.CacheControl = "public, max-age=60";
        if (h.Request.Headers.IfNoneMatch.Contains(etag)) return Results.StatusCode(StatusCodes.Status304NotModified);
        return Results.Bytes(cuerpo, "application/json");
    }
}
