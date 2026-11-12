using YamlDotNet.Serialization;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Carga los flujos de <c>.github/workflows</c> como árbol navegable (historia 006).</summary>
public static class FlujosCi
{
    public static readonly string[] Ramas = ["main", "release", "developer", "sprint/**"];

    public static string Carpeta => RaizRepositorio.Combinar(".github", "workflows");

    public static IReadOnlyList<string> Archivos() =>
        Directory.Exists(Carpeta) ? Directory.GetFiles(Carpeta, "*.yml").OrderBy(a => a).ToList() : [];

    public static string Texto(string archivo) => File.ReadAllText(Path.Combine(Carpeta, archivo));

    public static Dictionary<string, object?> Cargar(string archivo)
    {
        var ruta = Path.Combine(Carpeta, archivo);
        Xunit.Assert.True(File.Exists(ruta), $"No existe .github/workflows/{archivo}.");
        return Mapa(new DeserializerBuilder().Build().Deserialize<object>(File.ReadAllText(ruta)))!;
    }

    public static Dictionary<string, object?>? Mapa(object? nodo) =>
        nodo as Dictionary<string, object?> ??
        (nodo as IDictionary<object, object>)?.ToDictionary(p => p.Key.ToString()!, p => (object?)p.Value);

    public static Dictionary<string, object?>? Hijo(object? nodo, string clave) =>
        Mapa(nodo)?.GetValueOrDefault(clave) is { } v ? Mapa(v) : null;

    public static string? Cadena(object? nodo, string clave) => Mapa(nodo)?.GetValueOrDefault(clave)?.ToString();

    public static List<string> Lista(object? nodo) =>
        (nodo as IEnumerable<object>)?.Select(o => o.ToString()!).ToList() ?? [];

    public static Dictionary<string, object?> Job(Dictionary<string, object?> flujo, string id) =>
        Hijo(Hijo(flujo, "jobs"), id) ?? throw new Xunit.Sdk.XunitException($"Falta el job '{id}'.");

    public static List<Dictionary<string, object?>> Pasos(Dictionary<string, object?> job) =>
        ((job["steps"] as IEnumerable<object>) ?? []).Select(p => Mapa(p)!).ToList();

    public static Dictionary<string, object?> PasoUsa(Dictionary<string, object?> job, string accion) =>
        Pasos(job).FirstOrDefault(p => Cadena(p, "uses")?.StartsWith(accion + "@") == true)
        ?? throw new Xunit.Sdk.XunitException($"Falta el paso que usa {accion}.");

    public static List<string> Comandos(Dictionary<string, object?> job) =>
        Pasos(job).Select(p => Cadena(p, "run")?.Trim()).Where(r => r is not null).Select(r => r!).ToList();
}
