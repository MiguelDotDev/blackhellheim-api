namespace BlackHellHeim.Aplicacion.Comun.Errores;

/// <summary>Clasificación del error, independiente del transporte (HTTP).</summary>
public enum TipoError
{
    Validacion,
    NoEncontrado,
    Conflicto,
    Cancelado,
    NoDisponible,
    NoAutenticado,
    Bloqueado,
    Prohibido,
    Inesperado
}

/// <summary>
/// Error con formato uniforme. No contiene trazas, rutas internas ni datos sensibles.
/// </summary>
/// <param name="Tipo">Clasificación del error.</param>
/// <param name="Codigo">Código estable en español.</param>
/// <param name="Mensaje">Mensaje apto para el cliente.</param>
/// <param name="Campos">Errores por campo; solo en validación.</param>
/// <param name="IdCorrelacion">Identificador de correlación de la ejecución.</param>
public sealed record ErrorUniforme(
    TipoError Tipo,
    string Codigo,
    string Mensaje,
    IReadOnlyDictionary<string, string[]>? Campos,
    string IdCorrelacion)
{
    public static ErrorUniforme NoEncontrado(string mensaje, string idCorrelacion) =>
        new(TipoError.NoEncontrado, "recurso_no_encontrado", mensaje, null, idCorrelacion);

    public static ErrorUniforme Conflicto(string mensaje, string idCorrelacion) =>
        new(TipoError.Conflicto, "conflicto", mensaje, null, idCorrelacion);

    /// <summary>Mensaje único: no distingue correo inexistente de contraseña errónea.</summary>
    public static ErrorUniforme NoAutenticado(string idCorrelacion) =>
        new(TipoError.NoAutenticado, "no_autenticado", "Credenciales inválidas o sesión no válida.", null, idCorrelacion);

    public static ErrorUniforme Prohibido(string idCorrelacion) =>
        new(TipoError.Prohibido, "acceso_prohibido", "No tienes permiso para este recurso.", null, idCorrelacion);

    public static ErrorUniforme Bloqueado(string idCorrelacion) =>
        new(TipoError.Bloqueado, "cuenta_bloqueada",
            "La cuenta está bloqueada temporalmente por demasiados intentos. Intenta más tarde.", null, idCorrelacion);

    public static ErrorUniforme Inesperado(string idCorrelacion) =>
        new(TipoError.Inesperado, "error_inesperado",
            "Ocurrió un error inesperado. Usa el identificador de correlación para dar seguimiento.",
            null, idCorrelacion);

    public static ErrorUniforme BaseNoDisponible(string idCorrelacion) =>
        new(TipoError.NoDisponible, "base_no_disponible",
            "La base de datos no respondió. Verifica que el servicio local esté en marcha.", null, idCorrelacion);

    public static ErrorUniforme Cancelado(string idCorrelacion) =>
        new(TipoError.Cancelado, "operacion_cancelada", "La operación fue cancelada.", null, idCorrelacion);

    public static ErrorUniforme DeValidacion(IEnumerable<ErrorCampo> errores, string idCorrelacion)
    {
        var campos = errores
            .GroupBy(e => e.Campo, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Mensaje).ToArray(), StringComparer.Ordinal);
        return new(TipoError.Validacion, "solicitud_invalida", "La solicitud no es válida", campos, idCorrelacion);
    }
}
