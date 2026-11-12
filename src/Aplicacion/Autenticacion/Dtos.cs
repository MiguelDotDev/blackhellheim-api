namespace BlackHellHeim.Aplicacion.Autenticacion;

/// <summary>Respuesta de sesión; el refresh nunca viaja en el cuerpo (va en cookie).</summary>
public sealed record SesionDto(string TokenAcceso, int ExpiraEnSegundos, string Rol);

public sealed record PerfilDto(Guid Id, string Correo, string Rol);

/// <summary>Sesión recién emitida: el DTO público y el refresh que la API coloca en la cookie.</summary>
public sealed record SesionEmitida(SesionDto Sesion, string TokenRefresco, DateTimeOffset RefrescoExpiraUtc);

public sealed record UsuarioIdentidad(Guid Id, string Correo, string Rol);
