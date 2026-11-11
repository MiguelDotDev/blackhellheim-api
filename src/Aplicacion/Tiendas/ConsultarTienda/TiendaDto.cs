using System.ComponentModel.DataAnnotations;

namespace BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;

/// <summary>Modelo de lectura que devuelve la API; nunca se expone la entidad.</summary>
public sealed record TiendaDto(
    [property: RegularExpression("^[A-Z]{3}$")] string Codigo,
    string Nombre,
    [property: RegularExpression("^[A-Z]{3}$")] string Moneda);
