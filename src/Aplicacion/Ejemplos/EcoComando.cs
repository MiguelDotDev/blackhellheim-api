using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using Mediator;

namespace BlackHellHeim.Aplicacion.Ejemplos;

/// <summary>
/// Comando mínimo de ejemplo (RF-001): devuelve el mismo texto. Sin estado, sin persistencia
/// y sin endpoint; solo demuestra que los comandos usan el mismo mediador que las consultas.
/// </summary>
public sealed record EcoComando(string Texto) : ICommand<ResultadoCasoDeUso<string>>;
