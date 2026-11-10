using Xunit;

namespace BlackHellHeim.Pruebas.Arquitectura;

/// <summary>
/// Prueba de arquitectura sobre el grafo real de referencias de la solución.
/// Vigila la matriz de dependencias hacia adentro definida en el plan.
/// </summary>
public class MatrizReferenciasPruebas
{
    private static readonly string[] ProyectosEsperados =
    [
        "BlackHellHeim.Dominio",
        "BlackHellHeim.Aplicacion",
        "BlackHellHeim.Infraestructura",
        "BlackHellHeim.Api",
        "BlackHellHeim.Pruebas"
    ];

    [Fact]
    public void ElGrafoRealCumpleLaMatrizDeReferencias()
    {
        var grafo = LectorGrafoProyectos.LeerGrafo();

        var violaciones = MatrizReferencias.Validar(grafo);

        Assert.Empty(violaciones);
    }

    [Fact]
    public void ElGrafoRealContieneExactamenteLosCincoProyectos()
    {
        var grafo = LectorGrafoProyectos.LeerGrafo();

        Assert.Equal(
            ProyectosEsperados.OrderBy(nombre => nombre, StringComparer.Ordinal),
            grafo.Keys.OrderBy(nombre => nombre, StringComparer.Ordinal));
    }
}
