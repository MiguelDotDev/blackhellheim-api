using BlackHellHeim.Aplicacion.Comun.Errores;

namespace BlackHellHeim.Api.Comun;

/// <summary>
/// Traducción única de <see cref="ErrorUniforme"/> a ProblemDetails (RFC 9457). La usan el
/// middleware y los endpoints. No incluye <c>traceId</c>, trazas ni detalles internos.
/// </summary>
public static class TraductorProblemas
{
    public static int EstadoHttp(TipoError tipo) => tipo switch
    {
        TipoError.Validacion => StatusCodes.Status400BadRequest,
        TipoError.NoEncontrado => StatusCodes.Status404NotFound,
        TipoError.Conflicto => StatusCodes.Status409Conflict,
        TipoError.Cancelado => StatusCodes.Status408RequestTimeout,
        TipoError.NoDisponible => StatusCodes.Status503ServiceUnavailable,
        TipoError.NoAutenticado => StatusCodes.Status401Unauthorized,
        TipoError.Bloqueado => StatusCodes.Status423Locked,
        TipoError.Prohibido => StatusCodes.Status403Forbidden,
        TipoError.ProveedorFallo => StatusCodes.Status502BadGateway,
        TipoError.ProveedorSaturado => StatusCodes.Status503ServiceUnavailable,
        TipoError.ProveedorTiempoAgotado => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IResult Traducir(ErrorUniforme error, string instancia)
    {
        var estado = EstadoHttp(error.Tipo);
        var (titulo, detalle) = error.Tipo switch
        {
            TipoError.Validacion => ("La solicitud no es válida", "Revisa los campos indicados."),
            TipoError.NoEncontrado => ("Recurso no encontrado", error.Mensaje),
            TipoError.Conflicto => ("Conflicto", error.Mensaje),
            TipoError.Cancelado => ("Operación cancelada", error.Mensaje),
            TipoError.NoAutenticado => ("No autenticado", error.Mensaje),
            TipoError.Bloqueado => ("Cuenta bloqueada", error.Mensaje),
            TipoError.Prohibido => ("Acceso prohibido", error.Mensaje),
            TipoError.ProveedorFallo => ("Proveedor de impresión con fallo", error.Mensaje),
            TipoError.ProveedorSaturado => ("Proveedor de impresión saturado", error.Mensaje),
            TipoError.ProveedorTiempoAgotado => ("Proveedor de impresión sin respuesta", error.Mensaje),
            TipoError.NoDisponible => ("Base de datos no disponible", error.Mensaje),
            _ => ("Error interno", error.Mensaje)
        };

        var cuerpo = new Dictionary<string, object?>
        {
            ["type"] = $"https://blackhellheim.mx/errores/{error.Codigo}",
            ["title"] = titulo,
            ["status"] = estado,
            ["detail"] = detalle,
            ["instance"] = instancia,
            ["codigo"] = error.Codigo,
            ["idCorrelacion"] = error.IdCorrelacion
        };

        if (error.ReintentarEnSegundos is { } segundos)
        {
            cuerpo["retryAfter"] = segundos;
        }

        if (error.Campos is { Count: > 0 })
        {
            cuerpo["errores"] = error.Campos;
        }

        return Results.Json(cuerpo, statusCode: estado, contentType: "application/problem+json");
    }
}
