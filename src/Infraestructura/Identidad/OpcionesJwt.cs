using System.Text;

namespace BlackHellHeim.Infraestructura.Identidad;

/// <summary>Configuración del JWT (sección <c>Jwt</c>; variables <c>Jwt__*</c>). La clave nunca se versiona.</summary>
public sealed class OpcionesJwt
{
    public const int LongitudMinimaClave = 32;

    public string Clave { get; set; } = "";
    public string Emisor { get; set; } = "blackhellheim-api";
    public string Audiencia { get; set; } = "blackhellheim-web";
    public int MinutosAcceso { get; set; } = 15;

    public byte[] BytesClave => Encoding.UTF8.GetBytes(Clave);

    /// <summary>Falla al arrancar si la clave falta o es corta (no se revela su valor).</summary>
    public void Validar()
    {
        if (BytesClave.Length < LongitudMinimaClave)
        {
            throw new InvalidOperationException(
                $"Jwt__Clave es obligatoria y debe tener al menos {LongitudMinimaClave} bytes.");
        }
    }
}
