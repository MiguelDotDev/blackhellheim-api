using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;
using Mediator;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Endpoints de tiendas según <c>specs/002b-casos-de-uso-mediator/contratos/openapi.yaml</c>.</summary>
public static class TiendasEndpoints
{
    public static IEndpointRouteBuilder MapearTiendas(this IEndpointRouteBuilder rutas)
    {
        rutas.MapGet("/tiendas/{codigo}", async (string codigo, ISender emisor, HttpContext contexto, CancellationToken cancelacion) =>
        {
            var resultado = await emisor.Send(new ConsultarTiendaQuery(codigo), cancelacion);
            return resultado.EsExito
                ? Results.Ok(resultado.Valor)
                : TraductorProblemas.Traducir(resultado.Error!, contexto.Request.Path.Value ?? "/");
        })
        .WithName("consultarTienda")
        .Produces<TiendaDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status408RequestTimeout)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return rutas;
    }
}
