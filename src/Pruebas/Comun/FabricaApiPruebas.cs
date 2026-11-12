using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlackHellHeim.Pruebas.Comun;

/// <summary>
/// Servidor de pruebas de la API. Es la única forma permitida de arrancar la API en pruebas:
/// fija el entorno <c>Pruebas</c> e inyecta una cadena de conexión ficticia y sin credenciales,
/// de modo que la API arranca sin depender de variables de entorno ni de una base real.
/// </summary>
public class FabricaApiPruebas : WebApplicationFactory<Program>
{
    /// <summary>Cadena ficticia a un puerto cerrado; sin usuario ni contraseña.</summary>
    public const string CadenaPredeterminada =
        "Server=127.0.0.1,1;Database=BlackHellHeimPruebas;Integrated Security=true;Encrypt=False;Connect Timeout=1";

    private readonly string _cadena;

    /// <summary>Constructor público único y sin parámetros, como exige xUnit para fixtures de clase.</summary>
    public FabricaApiPruebas() : this(CadenaPredeterminada)
    {
    }

    private FabricaApiPruebas(string cadenaConexion)
    {
        _cadena = cadenaConexion;
    }

    /// <summary>Crea una fábrica con una cadena propia (contenedor real o puerto cerrado).</summary>
    public static FabricaApiPruebas Con(string cadenaConexion) => new(cadenaConexion);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Pruebas");
        builder.UseSetting("ConnectionStrings:BlackHellHeim", _cadena);
        // Clave de firma aleatoria por fábrica: nunca hay un valor fijo versionado.
        builder.UseSetting("Jwt:Clave", Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)));
    }
}
