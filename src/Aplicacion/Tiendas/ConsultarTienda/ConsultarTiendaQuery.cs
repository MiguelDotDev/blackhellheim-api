using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using Mediator;

namespace BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;

/// <summary>Consulta de solo lectura de una tienda por su código.</summary>
/// <param name="Codigo">Obligatorio; exactamente tres letras mayúsculas (<c>^[A-Z]{3}$</c>).</param>
public sealed record ConsultarTiendaQuery(string Codigo) : IQuery<ResultadoCasoDeUso<TiendaDto>>;
