namespace BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;

/// <summary>Modelo de lectura que devuelve la API; nunca se expone la entidad.</summary>
public sealed record TiendaDto(string Codigo, string Nombre, string Moneda);
