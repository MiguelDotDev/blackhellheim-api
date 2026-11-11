namespace BlackHellHeim.Api.Endpoints;

/// <summary>Cuerpo de <c>GET /salud</c> (contrato de la historia 002).</summary>
public sealed record EstadoSalud(string Estado);

public static class SaludEndpoints
{
    public static IEndpointRouteBuilder MapearSalud(this IEndpointRouteBuilder rutas)
    {
        // Comprobación de estado según specs/002-estructura-base-api/contratos/openapi.yaml
        rutas.MapGet("/salud", () => TypedResults.Ok(new EstadoSalud("ok")))
            .WithName("obtenerSalud")
            .Produces<EstadoSalud>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return rutas;
    }
}
