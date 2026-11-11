namespace BlackHellHeim.Aplicacion.Comun.Registro;

/// <summary>
/// Registro de una ejecución. No incluye la carga útil de la solicitud ni mensajes de
/// excepción, para no filtrar datos sensibles.
/// </summary>
public sealed record RegistroCasoDeUso(
    string TipoSolicitud,
    bool Exito,
    long DuracionMs,
    string IdCorrelacion,
    DateTimeOffset Momento,
    string? CodigoError,
    string? TipoExcepcion);
