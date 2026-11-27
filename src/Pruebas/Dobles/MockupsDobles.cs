using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Dominio.Mockups;
using BlackHellHeim.Pruebas.Aplicacion.Disenos;
using BlackHellHeim.Pruebas.Arte;

namespace BlackHellHeim.Pruebas.Dobles;

/// <summary>Repositorio en memoria con las mismas garantías atómicas que el de SQL Server.</summary>
public sealed class RepositorioMockupsEnMemoria : IRepositorioMockups
{
    private readonly object _candado = new();
    private readonly List<TareaMockup> _tareas = [];
    public List<Guid> Disenos { get; } = [];

    public Task<bool> ExisteDisenoAsync(Guid id, CancellationToken c) => Task.FromResult(Disenos.Contains(id));

    public Task<bool> ExistePendienteAsync(Guid id, CancellationToken c)
    {
        lock (_candado) return Task.FromResult(_tareas.Any(t => t.DisenoId == id && t.Estado == EstadoTareaMockup.Pendiente));
    }

    public Task<bool> AgregarAsync(TareaMockup tarea, CancellationToken c)
    {
        lock (_candado)
        {
            if (_tareas.Any(t => t.PrintfulTaskId == tarea.PrintfulTaskId
                    || (t.DisenoId == tarea.DisenoId && t.Estado == EstadoTareaMockup.Pendiente && tarea.Estado == EstadoTareaMockup.Pendiente)))
                return Task.FromResult(false);
            _tareas.Add(tarea);
            return Task.FromResult(true);
        }
    }

    public Task<TareaMockup?> ObtenerAsync(Guid id, CancellationToken c)
    {
        lock (_candado) return Task.FromResult(_tareas.FirstOrDefault(t => t.Id == id));
    }

    public Task<TareaMockup?> ObtenerPorPrintfulIdAsync(string idPrintful, CancellationToken c)
    {
        lock (_candado) return Task.FromResult(_tareas.FirstOrDefault(t => t.PrintfulTaskId == idPrintful));
    }

    public Task<IReadOnlyList<TareaMockup>> ListarPorDisenoAsync(Guid id, CancellationToken c)
    {
        lock (_candado) return Task.FromResult<IReadOnlyList<TareaMockup>>([.. _tareas.Where(t => t.DisenoId == id).OrderByDescending(t => t.CreadaUtc)]);
    }

    public Task<IReadOnlyList<Guid>> ListarPendientesAntesDeAsync(DateTimeOffset limite, int maximo, CancellationToken c)
    {
        lock (_candado)
            return Task.FromResult<IReadOnlyList<Guid>>([.. _tareas.Where(t => t.Estado == EstadoTareaMockup.Pendiente && t.CreadaUtc <= limite)
                .OrderBy(t => t.CreadaUtc).Select(t => t.Id).Take(maximo)]);
    }

    public Task<bool> CompletarAsync(Guid id, IReadOnlyList<ImagenMockup> imagenes, DateTimeOffset ahora, CancellationToken c)
    {
        lock (_candado)
        {
            var t = _tareas.First(x => x.Id == id);
            if (t.Estado != EstadoTareaMockup.Pendiente) return Task.FromResult(false);
            Poner(t, EstadoTareaMockup.Completada, null, ahora);
            ((List<ImagenMockup>)typeof(TareaMockup).GetField("_imagenes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(t)!).AddRange(imagenes);
            return Task.FromResult(true);
        }
    }

    public Task<bool> FallarAsync(Guid id, string motivo, DateTimeOffset ahora, CancellationToken c)
    {
        lock (_candado)
        {
            var t = _tareas.First(x => x.Id == id);
            if (t.Estado != EstadoTareaMockup.Pendiente) return Task.FromResult(false);
            Poner(t, EstadoTareaMockup.Fallida, motivo, ahora);
            return Task.FromResult(true);
        }
    }

    private static void Poner(TareaMockup t, EstadoTareaMockup estado, string? motivo, DateTimeOffset ahora)
    {
        typeof(TareaMockup).GetProperty(nameof(TareaMockup.Estado))!.SetValue(t, estado);
        typeof(TareaMockup).GetProperty(nameof(TareaMockup.Motivo))!.SetValue(t, motivo);
        typeof(TareaMockup).GetProperty(nameof(TareaMockup.FinalizadaUtc))!.SetValue(t, ahora);
    }
}

/// <summary>Printful falso: nunca hay red. Las tareas creadas arrancan Pendientes hasta que la prueba las resuelve.</summary>
public sealed class ProveedorMockupsFalso : IProveedorImpresion
{
    private int _creadas;
    private int _consultas;

    public Dictionary<string, ResultadoTareaMockup> Remotas { get; } = [];
    public Dictionary<Uri, byte[]?> Imagenes { get; } = [];
    public SolicitudMockup? UltimaSolicitud { get; private set; }
    public Func<Exception?> FalloCrear { get; set; } = () => null;
    public Func<string, Exception?> FalloConsulta { get; set; } = _ => null;
    public Func<Exception?> FalloDescarga { get; set; } = () => null;
    public int Creadas => _creadas;
    public int Consultas => _consultas;

    public Task<string> CrearTareaMockupAsync(SolicitudMockup s, CancellationToken c)
    {
        if (FalloCrear() is { } e) return Task.FromException<string>(e);
        UltimaSolicitud = s;
        var id = $"task-{Interlocked.Increment(ref _creadas)}";
        Remotas[id] = new(id, EstadoMockupProveedor.Pendiente, null, []);
        return Task.FromResult(id);
    }

    public Task<ResultadoTareaMockup?> ConsultarTareaMockupAsync(string id, CancellationToken c)
    {
        Interlocked.Increment(ref _consultas);
        if (FalloConsulta(id) is { } e) return Task.FromException<ResultadoTareaMockup?>(e);
        return Task.FromResult(Remotas.GetValueOrDefault(id));
    }

    public Task<byte[]?> DescargarImagenAsync(Uri url, long max, CancellationToken c)
    {
        if (FalloDescarga() is { } e) return Task.FromException<byte[]?>(e);
        return Task.FromResult(Imagenes.GetValueOrDefault(url));
    }

    /// <summary>Resuelve la tarea con una imagen por (variante, posición); registra los bytes para cada URL.</summary>
    public void Completar(string id, params (int Variante, string Posicion, byte[]? Bytes)[] mockups)
    {
        var lista = new List<MockupGenerado>();
        foreach (var (variante, posicion, bytes) in mockups)
        {
            var url = new Uri($"https://files.cdn.printful.com/{id}/{variante}-{posicion}.png");
            Imagenes[url] = bytes;
            lista.Add(new MockupGenerado(variante, posicion, url));
        }

        Remotas[id] = new(id, EstadoMockupProveedor.Completada, null, lista);
    }

    public Task<IReadOnlyList<ProductoPrintfulDto>> ListarProductosAsync(string c, int p, int t, CancellationToken x) => throw new NotSupportedException();
    public Task<ProductoPrintfulDto?> ObtenerProductoAsync(int id, CancellationToken x) => throw new NotSupportedException();
    public Task<IReadOnlyList<VariantePrintfulDto>?> ListarVariantesAsync(int id, string t, string p, CancellationToken x) => throw new NotSupportedException();
}

public sealed class AdvertenciasMockupsFalsas : BlackHellHeim.Aplicacion.Precios.IAdvertencias
{
    public List<string> Mensajes { get; } = [];
    public void Advertir(string mensaje) => Mensajes.Add(mensaje);
}

/// <summary>Diseño con arte y variantes (S y M negras, L blanca) listo para pedir mockups.</summary>
public sealed class EscenarioMockups
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    public static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    public RelojFalso Reloj { get; } = new(DateTimeOffset.UtcNow); // real: los JWT de las pruebas se validan con el reloj del sistema
    internal RepositorioDisenosFalso Disenos { get; } = new();
    public RepositorioMockupsEnMemoria Repo { get; } = new();
    public ProveedorMockupsFalso Proveedor { get; } = new();
    internal AlmacenEnMemoria Almacen { get; } = new();
    public OpcionesMockups Opciones { get; } = new();
    public ContextoEjecucionFalso Contexto { get; } = new() { IdCorrelacion = "c" };

    public Diseno Diseno { get; } = Diseno.Crear("Diseño de prueba", null, new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    public AdvertenciasMockupsFalsas Advertencias { get; } = new();

    public EscenarioMockups(bool conArte = true, bool conVariantes = true)
    {
        if (conArte)
            Diseno.AsignarArte(ArchivoArte.Crear("https://almacen.test/arte/a.png", "a.png", "image/png", 10, 4500, 5400, new string('a', 64), Reloj.Ahora), Reloj.Ahora);
        if (conVariantes)
        {
            var producto = ProductoBase.Crear(71, "Playera", "T-SHIRT");
            Diseno.ReemplazarVariantes(
            [
                Variante.Crear(producto, 1, "Black", "#000000", "S", 12.5m),
                Variante.Crear(producto, 2, "Black", "#000000", "M", 12.5m),
                Variante.Crear(producto, 3, "White", "#ffffff", "L", 12.5m)
            ], Reloj.Ahora);
        }

        Disenos.Disenos.Add(Diseno);
        Repo.Disenos.Add(Diseno.Id);
    }

    public ManejadorPedirMockups Pedir => new(Disenos, Repo, Proveedor, Opciones, Reloj, Contexto);

    public ManejadorFinalizarTareaMockup Finalizar => new(Repo, Disenos, Proveedor, Almacen, Opciones, Reloj, Contexto, Advertencias);

    public async Task<TareaMockupDto> PedirOkAsync()
    {
        var r = await Pedir.Handle(new PedirMockupsComando(Diseno.Id), default);
        return r.Valor ?? throw new InvalidOperationException(r.Error!.Codigo);
    }
}
