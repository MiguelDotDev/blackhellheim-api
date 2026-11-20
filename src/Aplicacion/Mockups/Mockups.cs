using System.Text.Json;
using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Aplicacion.Comun.CasosDeUso;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Disenos;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Dominio.Mockups;
using Mediator;

namespace BlackHellHeim.Aplicacion.Mockups;

/// <summary>Opciones <c>Mockups__*</c>, validadas al arrancar.</summary>
public sealed class OpcionesMockups
{
    public int IntervaloPollingSegundos { get; set; } = 30;
    public int EsperaMinimaSegundos { get; set; } = 120;
    public int TiempoMaximoMinutos { get; set; } = 30;
    public long MaxBytesImagen { get; set; } = 20L * 1024 * 1024;
    public string Posicion { get; set; } = "front";
    public string Tecnica { get; set; } = "dtg";

    /// <summary>Hosts https desde los que se descargan imágenes (anti-SSRF). Valor por omisión sin verificar ([MANUAL]).</summary>
    public string[] HostsDescargaPermitidos { get; set; } = ["files.cdn.printful.com"];

    public void Validar()
    {
        if (IntervaloPollingSegundos is < 1 or > 3600) throw new InvalidOperationException("Mockups__IntervaloPollingSegundos debe estar entre 1 y 3600.");
        if (EsperaMinimaSegundos < 0) throw new InvalidOperationException("Mockups__EsperaMinimaSegundos no puede ser negativa.");
        if (TiempoMaximoMinutos < 1) throw new InvalidOperationException("Mockups__TiempoMaximoMinutos debe ser positivo.");
        if (TimeSpan.FromSeconds(EsperaMinimaSegundos) >= TimeSpan.FromMinutes(TiempoMaximoMinutos))
            throw new InvalidOperationException("Mockups__EsperaMinimaSegundos debe ser menor que TiempoMaximoMinutos.");
        if (MaxBytesImagen < 1) throw new InvalidOperationException("Mockups__MaxBytesImagen debe ser positivo.");
        if (string.IsNullOrWhiteSpace(Posicion)) throw new InvalidOperationException("Mockups__Posicion es obligatoria.");
        if (!ValoresImpresion.Tecnicas.Contains(Tecnica)) throw new InvalidOperationException("Mockups__Tecnica no es una técnica admitida.");
        if (HostsDescargaPermitidos is not { Length: > 0 } || HostsDescargaPermitidos.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Mockups__HostsDescargaPermitidos debe tener al menos un host.");
    }
}

public sealed record ImagenMockupDto(Guid Id, string Color, string Posicion, string Url);

public sealed record TareaMockupDto(
    Guid Id, Guid DisenoId, string Estado, string? Motivo, DateTimeOffset CreadaUtc, DateTimeOffset? FinalizadaUtc,
    IReadOnlyList<ImagenMockupDto> Imagenes)
{
    public static TareaMockupDto De(TareaMockup t) => new(t.Id, t.DisenoId, t.Estado.ToString(), t.Motivo, t.CreadaUtc, t.FinalizadaUtc,
        [.. t.Imagenes.Select(i => new ImagenMockupDto(i.Id, i.Color, i.Posicion, i.Url))]);
}

public interface IRepositorioMockups
{
    Task<bool> ExisteDisenoAsync(Guid disenoId, CancellationToken cancelacion);

    Task<bool> ExistePendienteAsync(Guid disenoId, CancellationToken cancelacion);

    /// <summary>Inserta la tarea; false si choca con la restricción de una pendiente por diseño o de id repetido.</summary>
    Task<bool> AgregarAsync(TareaMockup tarea, CancellationToken cancelacion);

    Task<TareaMockup?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    Task<TareaMockup?> ObtenerPorPrintfulIdAsync(string printfulTaskId, CancellationToken cancelacion);

    Task<IReadOnlyList<TareaMockup>> ListarPorDisenoAsync(Guid disenoId, CancellationToken cancelacion);

    Task<IReadOnlyList<Guid>> ListarPendientesAntesDeAsync(DateTimeOffset limite, int maximo, CancellationToken cancelacion);

    /// <summary>Atómico: inserta las imágenes y pasa Pendiente→Completada en una transacción; false si otra finalización ganó.</summary>
    Task<bool> CompletarAsync(Guid tareaId, IReadOnlyList<ImagenMockup> imagenes, DateTimeOffset ahora, CancellationToken cancelacion);

    /// <summary>Condicional Pendiente→Fallida; false si ya no estaba pendiente.</summary>
    Task<bool> FallarAsync(Guid tareaId, string motivo, DateTimeOffset ahora, CancellationToken cancelacion);
}

// --- Pedir ---------------------------------------------------------------------------------

public sealed record PedirMockupsComando(Guid DisenoId) : ICommand<ResultadoCasoDeUso<TareaMockupDto>>;

public sealed class ManejadorPedirMockups(
    IRepositorioDisenos disenos, IRepositorioMockups repositorio, IProveedorImpresion proveedor,
    OpcionesMockups opciones, TimeProvider reloj, IContextoEjecucion contexto)
    : ICommandHandler<PedirMockupsComando, ResultadoCasoDeUso<TareaMockupDto>>
{
    public async ValueTask<ResultadoCasoDeUso<TareaMockupDto>> Handle(PedirMockupsComando c, CancellationToken cancelacion)
    {
        var id = contexto.IdCorrelacion;
        var diseno = await disenos.ObtenerAsync(c.DisenoId, cancelacion);
        if (diseno is null) return ResultadoCasoDeUso<TareaMockupDto>.Fallo(ErrorUniforme.NoEncontrado("El diseño no existe.", id));

        var activas = diseno.Variantes.Where(v => v.Activa).ToList();
        var errores = new List<ErrorCampo>();
        if (diseno.ArchivoArte is null) errores.Add(new("arte", "El diseño no tiene arte subido."));
        if (activas.Count == 0) errores.Add(new("variantes", "El diseño no tiene variantes activas."));
        if (errores.Count > 0) return ResultadoCasoDeUso<TareaMockupDto>.Fallo(ErrorUniforme.DeValidacion(errores, id));

        if (await repositorio.ExistePendienteAsync(c.DisenoId, cancelacion)) return Duplicada();

        var productos = activas.GroupBy(v => v.ProductoBase.PrintfulProductoId)
            .Select(g => new ProductoMockup(g.Key, [.. g.Select(v => v.PrintfulVarianteId).Distinct()])).ToList();
        var taskId = await proveedor.CrearTareaMockupAsync(
            new SolicitudMockup(productos, diseno.ArchivoArte!.Url, opciones.Posicion, opciones.Tecnica), cancelacion);

        var tarea = TareaMockup.Crear(c.DisenoId, taskId, reloj.GetUtcNow());
        // Si una carrera ganó el índice, la tarea creada en Printful queda huérfana (inofensiva).
        return await repositorio.AgregarAsync(tarea, cancelacion)
            ? ResultadoCasoDeUso<TareaMockupDto>.Exito(TareaMockupDto.De(tarea))
            : Duplicada();
    }

    private ResultadoCasoDeUso<TareaMockupDto> Duplicada() =>
        ResultadoCasoDeUso<TareaMockupDto>.Fallo(ErrorUniforme.Conflicto("Ya hay una tarea de mockups pendiente para este diseño.", contexto.IdCorrelacion));
}

// --- Listar --------------------------------------------------------------------------------

public sealed record ListarMockupsQuery(Guid DisenoId) : IQuery<ResultadoCasoDeUso<IReadOnlyList<TareaMockupDto>>>;

public sealed class ManejadorListarMockups(IRepositorioMockups repositorio, IContextoEjecucion contexto)
    : IQueryHandler<ListarMockupsQuery, ResultadoCasoDeUso<IReadOnlyList<TareaMockupDto>>>
{
    public async ValueTask<ResultadoCasoDeUso<IReadOnlyList<TareaMockupDto>>> Handle(ListarMockupsQuery q, CancellationToken cancelacion)
    {
        if (!await repositorio.ExisteDisenoAsync(q.DisenoId, cancelacion))
            return ResultadoCasoDeUso<IReadOnlyList<TareaMockupDto>>.Fallo(ErrorUniforme.NoEncontrado("El diseño no existe.", contexto.IdCorrelacion));
        var tareas = await repositorio.ListarPorDisenoAsync(q.DisenoId, cancelacion);
        return ResultadoCasoDeUso<IReadOnlyList<TareaMockupDto>>.Exito([.. tareas.Select(TareaMockupDto.De)]);
    }
}

// --- Finalizar (evento y polling) ----------------------------------------------------------

public sealed record FinalizarTareaMockupComando(Guid TareaId) : ICommand<ResultadoCasoDeUso<EstadoTareaMockup>>;

public sealed class ManejadorFinalizarTareaMockup(
    IRepositorioMockups repositorio, IRepositorioDisenos disenos, IProveedorImpresion proveedor, IAlmacenArchivos almacen,
    OpcionesMockups opciones, TimeProvider reloj, IContextoEjecucion contexto)
    : ICommandHandler<FinalizarTareaMockupComando, ResultadoCasoDeUso<EstadoTareaMockup>>
{
    public async ValueTask<ResultadoCasoDeUso<EstadoTareaMockup>> Handle(FinalizarTareaMockupComando c, CancellationToken cancelacion)
    {
        var tarea = await repositorio.ObtenerAsync(c.TareaId, cancelacion);
        if (tarea is null)
            return ResultadoCasoDeUso<EstadoTareaMockup>.Fallo(ErrorUniforme.NoEncontrado("La tarea de mockups no existe.", contexto.IdCorrelacion));
        if (tarea.Estado != EstadoTareaMockup.Pendiente) return Exito(tarea.Estado);

        var ahora = reloj.GetUtcNow();
        var remota = await proveedor.ConsultarTareaMockupAsync(tarea.PrintfulTaskId, cancelacion);
        if (remota is null || remota.Estado == EstadoMockupProveedor.Pendiente)
        {
            if (ahora - tarea.CreadaUtc < TimeSpan.FromMinutes(opciones.TiempoMaximoMinutos)) return Exito(EstadoTareaMockup.Pendiente);
            return await Fallar(tarea, "Tiempo agotado: Printful no entregó los mockups.", ahora, cancelacion);
        }

        if (remota.Estado == EstadoMockupProveedor.Fallida)
            return await Fallar(tarea, string.IsNullOrWhiteSpace(remota.Motivo) ? "Printful reportó un fallo." : remota.Motivo, ahora, cancelacion);

        var diseno = await disenos.ObtenerAsync(tarea.DisenoId, cancelacion);
        var colores = (diseno?.Variantes ?? []).GroupBy(v => v.PrintfulVarianteId).ToDictionary(g => g.Key, g => g.First().Color);
        var imagenes = new List<ImagenMockup>();
        var vistos = new HashSet<string>();
        foreach (var m in remota.Mockups)
        {
            if (!colores.TryGetValue(m.VarianteId, out var color)) continue; // variante desconocida: se descarta
            var clave = $"{Slug(color)}-{Slug(m.Posicion)}";
            if (vistos.Contains(clave)) continue; // Printful da un mockup por talla; basta uno por color y posición
            var bytes = await proveedor.DescargarImagenAsync(m.Url, opciones.MaxBytesImagen, cancelacion);
            if (bytes is null || TipoImagen(bytes) is not { } tipo) continue; // no permitida, demasiado grande o no PNG/JPEG
            vistos.Add(clave);
            var nombre = $"mockups/{tarea.Id:N}/{clave}.{tipo.Extension}";
            var url = await almacen.GuardarAsync(nombre, tipo.Mime, new MemoryStream(bytes), cancelacion);
            imagenes.Add(ImagenMockup.Crear(tarea.Id, color, m.Posicion, url.ToString(), nombre));
        }

        if (imagenes.Count == 0) return await Fallar(tarea, "Printful no devolvió imágenes válidas.", ahora, cancelacion);
        // false = otra finalización ganó la carrera; el resultado es el mismo.
        await repositorio.CompletarAsync(tarea.Id, imagenes, ahora, cancelacion);
        return Exito(EstadoTareaMockup.Completada);
    }

    private async Task<ResultadoCasoDeUso<EstadoTareaMockup>> Fallar(TareaMockup t, string motivo, DateTimeOffset ahora, CancellationToken c)
    {
        await repositorio.FallarAsync(t.Id, TareaMockup.RecortarMotivo(motivo), ahora, c);
        return Exito(EstadoTareaMockup.Fallida);
    }

    private static ResultadoCasoDeUso<EstadoTareaMockup> Exito(EstadoTareaMockup e) => ResultadoCasoDeUso<EstadoTareaMockup>.Exito(e);

    /// <summary>PNG o JPEG por su firma binaria (la cabecera Content-Type no es confiable).</summary>
    public static (string Mime, string Extension)? TipoImagen(byte[] b) =>
        b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 ? ("image/png", "png")
        : b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? ("image/jpeg", "jpg")
        : null;

    public static string Slug(string texto)
    {
        var s = new string([.. texto.ToLowerInvariant().Select(ch => char.IsAsciiLetterOrDigit(ch) ? ch : '-')]).Trim('-');
        while (s.Contains("--", StringComparison.Ordinal)) s = s.Replace("--", "-", StringComparison.Ordinal);
        return s.Length == 0 ? "x" : s.Length > 40 ? s[..40] : s;
    }
}

// --- Evento mockup_task_finished (HU-22) ---------------------------------------------------

/// <summary>
/// Consumidor del evento. El evento solo avisa: el resultado se vuelve a pedir a Printful, así no
/// depende de la forma exacta del payload (sin verificar, [MANUAL]); el polling cubre cualquier diferencia.
/// </summary>
public sealed class ManejadorMockupTaskFinished(ISender emisor, IRepositorioMockups repositorio) : IManejadorEventoProveedor
{
    public string Proveedor => "printful";

    public IReadOnlyCollection<string> Tipos { get; } = ["mockup_task_finished"];

    public async Task ManejarAsync(EventoRecibido evento, CancellationToken cancelacion)
    {
        if (ExtraerIdTarea(evento.Cuerpo) is not { } idTarea) return;
        var tarea = await repositorio.ObtenerPorPrintfulIdAsync(idTarea, cancelacion);
        if (tarea is null) return; // tarea de otro entorno o inexistente: se acepta sin hacer nada
        // Si lanza, HU-22 marca el evento Fallido y responde 500 para que Printful reintente.
        await emisor.Send(new FinalizarTareaMockupComando(tarea.Id), cancelacion);
    }

    public static string? ExtraerIdTarea(string cuerpo)
    {
        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            foreach (var ruta in new[] { new[] { "data", "mockup_task", "id" }, ["data", "task", "id"], ["data", "id"] })
            {
                var actual = doc.RootElement;
                var ok = true;
                foreach (var parte in ruta)
                {
                    if (actual.ValueKind != JsonValueKind.Object || !actual.TryGetProperty(parte, out actual)) { ok = false; break; }
                }

                if (ok && actual.ValueKind == JsonValueKind.String && actual.GetString() is { Length: > 0 } s) return s;
                if (ok && actual.ValueKind == JsonValueKind.Number) return actual.GetRawText();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
