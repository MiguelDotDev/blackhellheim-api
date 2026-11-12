using BlackHellHeim.Api.Configuracion;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Persistencia;

public class CadenaConexionPruebas
{
    private static IConfiguration Configuracion(string? valor)
    {
        var datos = new Dictionary<string, string?>();
        if (valor is not null)
        {
            datos["ConnectionStrings:BlackHellHeim"] = valor;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(datos).Build();
    }

    [Fact]
    public void LeeLaCadenaValida()
    {
        const string cadena = "Server=s;Database=d;Integrated Security=true";

        Assert.Equal(cadena, CadenaConexion.Obtener(Configuracion(cadena)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FallaNombrandoLaVariableSiFaltaOEstaVacia(string? valor)
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() => CadenaConexion.Obtener(Configuracion(valor)));

        Assert.Contains("ConnectionStrings__BlackHellHeim", excepcion.Message);
        Assert.Contains(".env.example", excepcion.Message);
    }

    [Fact]
    public void ElMensajeNoContieneElValorDeLaCadena()
    {
        // Una cadena con espacios solamente es inválida; el mensaje no debe repetir ningún valor.
        var excepcion = Assert.Throws<InvalidOperationException>(() => CadenaConexion.Obtener(Configuracion("   ")));

        Assert.DoesNotContain("Password", excepcion.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }
}
