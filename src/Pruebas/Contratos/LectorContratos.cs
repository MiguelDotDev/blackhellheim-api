using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace BlackHellHeim.Pruebas.Contratos;

/// <summary>Localiza y lee los contratos <c>specs/*/contratos/openapi.yaml</c> como JSON.</summary>
public static class LectorContratos
{
    /// <summary>Sube desde <paramref name="inicio"/> hasta encontrar la raíz (BlackHellHeim.sln y specs).</summary>
    public static string BuscarRaiz(string inicio)
    {
        var directorio = new DirectoryInfo(inicio);
        while (directorio is not null &&
               !(File.Exists(Path.Combine(directorio.FullName, "BlackHellHeim.sln")) &&
                 Directory.Exists(Path.Combine(directorio.FullName, "specs"))))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new InvalidOperationException(
            $"No se encontró la raíz del repositorio (BlackHellHeim.sln y specs) hacia arriba desde '{inicio}'.");
    }

    /// <summary>Lee todos los contratos; la clave es la carpeta de la spec (por ejemplo, <c>003-sql-server-efcore</c>).</summary>
    public static IReadOnlyDictionary<string, JsonObject> LeerTodos(string raiz)
    {
        var resultado = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var ruta in Directory.EnumerateFiles(Path.Combine(raiz, "specs"), "openapi.yaml", SearchOption.AllDirectories))
        {
            var carpeta = new DirectoryInfo(Path.GetDirectoryName(ruta)!);
            if (carpeta.Name != "contratos")
            {
                continue;
            }

            var contrato = ConvertirYaml(File.ReadAllText(ruta));
            // Un contrato con `x-pendiente: true` aún no está implementado: se omite hasta que su historia lo esté.
            if (contrato["x-pendiente"]?.GetValue<bool>() == true)
            {
                continue;
            }

            resultado[carpeta.Parent!.Name] = contrato;
        }

        return resultado;
    }

    public static JsonObject ConvertirYaml(string yaml)
    {
        var flujo = new YamlStream();
        flujo.Load(new StringReader(yaml));
        return (JsonObject)Convertir(flujo.Documents[0].RootNode)!;
    }

    private static JsonNode? Convertir(YamlNode nodo) => nodo switch
    {
        YamlMappingNode mapa => new JsonObject(mapa.Children.Select(par =>
            new KeyValuePair<string, JsonNode?>(((YamlScalarNode)par.Key).Value ?? string.Empty, Convertir(par.Value)))),
        YamlSequenceNode secuencia => new JsonArray(secuencia.Children.Select(Convertir).ToArray()),
        YamlScalarNode escalar => ConvertirEscalar(escalar),
        _ => null
    };

    private static JsonNode? ConvertirEscalar(YamlScalarNode escalar)
    {
        var valor = escalar.Value;
        if (escalar.Style != ScalarStyle.Plain || valor is null)
        {
            return JsonValue.Create(valor);
        }

        if (valor is "true" or "false")
        {
            return JsonValue.Create(valor == "true");
        }

        return long.TryParse(valor, out var entero) ? JsonValue.Create(entero) : JsonValue.Create(valor);
    }
}
