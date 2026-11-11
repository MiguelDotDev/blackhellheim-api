namespace BlackHellHeim.Aplicacion.Comun.Errores;

/// <summary>Error de validación asociado a un campo de la solicitud.</summary>
public sealed record ErrorCampo(string Campo, string Mensaje);
