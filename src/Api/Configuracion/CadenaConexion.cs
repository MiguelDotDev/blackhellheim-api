namespace BlackHellHeim.Api.Configuracion;

/// <summary>Lectura y validación de la cadena de conexión. Nunca imprime su valor.</summary>
public static class CadenaConexion
{
    public const string NombreVariable = "ConnectionStrings__BlackHellHeim";

    public static string Obtener(IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("BlackHellHeim");
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                $"Falta la variable de entorno {NombreVariable}. " +
                "Copia .env.example a .env, define la contraseña y cárgalo antes de iniciar la API.");
        }

        return cadena;
    }
}
