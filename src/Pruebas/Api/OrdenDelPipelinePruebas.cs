using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Comun.Comportamientos;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>
/// El primer comportamiento registrado es el más externo (plan.md, orden del pipeline):
/// correlación, errores, registro, validación y, al final, el manejador.
/// </summary>
public class OrdenDelPipelinePruebas
{
    [Fact]
    public void ElPipelineSeRegistraEnElOrdenDelPlan()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarCasosDeUso();

        var comportamientos = servicios
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToArray();

        Assert.Equal(
            [
                typeof(CorrelacionBehavior<,>),
                typeof(ErroresBehavior<,>),
                typeof(RegistroBehavior<,>),
                typeof(ValidacionBehavior<,>)
            ],
            comportamientos);
    }
}
