namespace BlackHellHeim.Dominio.Mockups;

public enum EstadoTareaMockup { Pendiente, Completada, Fallida }

/// <summary>Tarea asíncrona de mockups en el proveedor de impresión. Pendiente → Completada | Fallida (final).</summary>
public sealed class TareaMockup
{
    public const int MotivoMaximo = 300;
    private readonly List<ImagenMockup> _imagenes = [];

    private TareaMockup()
    {
    }

    public Guid Id { get; private set; }
    public Guid DisenoId { get; private set; }
    public string PrintfulTaskId { get; private set; } = "";
    public EstadoTareaMockup Estado { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset CreadaUtc { get; private set; }
    public DateTimeOffset? FinalizadaUtc { get; private set; }
    public IReadOnlyList<ImagenMockup> Imagenes => _imagenes;

    public static TareaMockup Crear(Guid disenoId, string printfulTaskId, DateTimeOffset ahora)
    {
        if (string.IsNullOrWhiteSpace(printfulTaskId) || printfulTaskId.Length > 60)
            throw new ArgumentException("El id de tarea de Printful es obligatorio y de hasta 60 caracteres.", nameof(printfulTaskId));
        return new TareaMockup { Id = Guid.NewGuid(), DisenoId = disenoId, PrintfulTaskId = printfulTaskId, Estado = EstadoTareaMockup.Pendiente, CreadaUtc = ahora };
    }

    public static string RecortarMotivo(string motivo) =>
        motivo.Length <= MotivoMaximo ? motivo : motivo[..MotivoMaximo];
}

public sealed class ImagenMockup
{
    private ImagenMockup()
    {
    }

    public Guid Id { get; private set; }
    public Guid TareaId { get; private set; }
    public string Color { get; private set; } = "";
    public string Posicion { get; private set; } = "";
    public string Url { get; private set; } = "";
    public string NombreBlob { get; private set; } = "";

    public static ImagenMockup Crear(Guid tareaId, string color, string posicion, string url, string nombreBlob) =>
        new() { Id = Guid.NewGuid(), TareaId = tareaId, Color = color, Posicion = posicion, Url = url, NombreBlob = nombreBlob };
}
