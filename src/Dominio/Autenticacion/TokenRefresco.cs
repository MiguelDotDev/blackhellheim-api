namespace BlackHellHeim.Dominio.Autenticacion;

/// <summary>
/// Token de refresco opaco. Solo se guarda su hash. Cada renovación lo marca como usado y crea
/// otro en la misma familia; presentar uno ya usado o revocado revoca la familia completa.
/// </summary>
public sealed class TokenRefresco
{
    private TokenRefresco()
    {
    }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid FamiliaId { get; private set; }
    public string HashToken { get; private set; } = "";
    public DateTimeOffset CreadoUtc { get; private set; }
    public DateTimeOffset ExpiraUtc { get; private set; }
    public DateTimeOffset? UsadoUtc { get; private set; }
    public DateTimeOffset? RevocadoUtc { get; private set; }

    public static TokenRefresco Crear(
        Guid usuarioId, Guid familiaId, string hashToken, DateTimeOffset ahora, TimeSpan vida)
    {
        if (string.IsNullOrWhiteSpace(hashToken))
        {
            throw new ArgumentException("El hash del token es obligatorio.", nameof(hashToken));
        }

        if (vida <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(vida), "La vida debe ser positiva.");
        }

        return new TokenRefresco
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            FamiliaId = familiaId,
            HashToken = hashToken,
            CreadoUtc = ahora,
            ExpiraUtc = ahora + vida
        };
    }

    /// <summary>Activo = sin usar, sin revocar y sin caducar.</summary>
    public bool EstaActivo(DateTimeOffset ahora) =>
        UsadoUtc is null && RevocadoUtc is null && ExpiraUtc > ahora;

    /// <summary>Ya se rotó o se revocó: presentarlo de nuevo es un reuso sospechoso.</summary>
    public bool FueConsumido => UsadoUtc is not null || RevocadoUtc is not null;
}
