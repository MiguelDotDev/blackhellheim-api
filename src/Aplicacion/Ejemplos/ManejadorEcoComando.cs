using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using Mediator;

namespace BlackHellHeim.Aplicacion.Ejemplos;

public sealed class ManejadorEcoComando : ICommandHandler<EcoComando, ResultadoCasoDeUso<string>>
{
    public ValueTask<ResultadoCasoDeUso<string>> Handle(EcoComando comando, CancellationToken cancelacion) =>
        ValueTask.FromResult(ResultadoCasoDeUso<string>.Exito(comando.Texto));
}
