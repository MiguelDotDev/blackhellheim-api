using Microsoft.AspNetCore.Http;

namespace BlackHellHeim.Api.Errores;

/// <summary>Configuración de ProblemDetails: el formato uniforme no incluye <c>traceId</c>.</summary>
public static class OpcionesProblemDetails
{
    public static void Configurar(ProblemDetailsContext contexto) =>
        contexto.ProblemDetails.Extensions.Remove("traceId");
}
