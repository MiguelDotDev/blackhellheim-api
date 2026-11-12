using System.Text.Json.Nodes;

namespace BlackHellHeim.Pruebas.Contratos;

/// <summary>
/// Compone los contratos y compara su forma con el documento publicado. Ignora lo que no
/// afecta al contrato: <c>info</c>, <c>servers</c>, <c>tags</c>, descripciones, resúmenes,
/// ejemplos, <c>const</c>, extensiones <c>x-*</c> y la cabecera <c>X-Correlation-ID</c>.
/// </summary>
public static class ComparadorContratos
{
    private static readonly string[] Metodos = ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

    public sealed record Composicion(JsonObject Paths, JsonObject Esquemas, IReadOnlyList<string> Errores);

    public static Composicion Componer(IEnumerable<KeyValuePair<string, JsonObject>> contratos)
    {
        var rutas = new JsonObject();
        var esquemas = new JsonObject();
        var errores = new List<string>();

        foreach (var (origen, contrato) in contratos)
        {
            foreach (var (ruta, item) in contrato["paths"] as JsonObject ?? [])
            {
                foreach (var (metodo, operacion) in (item as JsonObject) ?? [])
                {
                    if (!Metodos.Contains(metodo))
                    {
                        continue;
                    }

                    var clave = $"{metodo.ToUpperInvariant()} {ruta}";
                    var existente = rutas[clave];
                    if (existente is null)
                    {
                        rutas[clave] = operacion?.DeepClone();
                    }
                    else if (!Proyectar(existente).SequenceEqual(Proyectar(operacion)))
                    {
                        errores.Add($"Operación duplicada con contenido distinto: {clave} (también en {origen}).");
                    }
                }
            }

            foreach (var (nombre, esquema) in contrato["components"]?["schemas"] as JsonObject ?? [])
            {
                var existente = esquemas[nombre];
                if (existente is null)
                {
                    esquemas[nombre] = esquema?.DeepClone();
                }
                else if (!AplanarEsquema(existente, nombre).SequenceEqual(AplanarEsquema(esquema, nombre)))
                {
                    errores.Add($"Esquema duplicado con contenido distinto: {nombre} (también en {origen}).");
                }
            }
        }

        return new Composicion(rutas, esquemas, errores);
    }

    /// <summary>Proyección de un documento OpenAPI completo o de una composición, como líneas ordenadas.</summary>
    public static SortedDictionary<string, string> Proyectar(JsonObject documento)
    {
        var lineas = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (ruta, item) in documento["paths"] as JsonObject ?? [])
        {
            foreach (var (metodo, operacion) in (item as JsonObject) ?? [])
            {
                if (Metodos.Contains(metodo))
                {
                    Unir(lineas, ProyectarOperacion($"{metodo.ToUpperInvariant()} {ruta}", operacion));
                }
            }
        }

        foreach (var (nombre, esquema) in documento["components"]?["schemas"] as JsonObject ?? [])
        {
            Unir(lineas, AplanarEsquema(esquema, $"esquema {nombre}"));
        }

        return lineas;
    }

    /// <summary>Proyección de una composición (rutas por clave "MÉTODO /ruta" y esquemas).</summary>
    public static SortedDictionary<string, string> Proyectar(Composicion composicion)
    {
        var lineas = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (clave, operacion) in composicion.Paths)
        {
            Unir(lineas, ProyectarOperacion(clave, operacion));
        }

        foreach (var (nombre, esquema) in composicion.Esquemas)
        {
            Unir(lineas, AplanarEsquema(esquema, $"esquema {nombre}"));
        }

        return lineas;
    }

    private static SortedDictionary<string, string> Proyectar(JsonNode? operacion) =>
        ProyectarOperacion("op", operacion);

    /// <summary>Compara y devuelve diferencias legibles (vacío si coinciden).</summary>
    public static IReadOnlyList<string> Comparar(
        SortedDictionary<string, string> contrato, SortedDictionary<string, string> publicado)
    {
        var diferencias = new List<string>();
        foreach (var (clave, valor) in contrato)
        {
            if (!publicado.TryGetValue(clave, out var otro))
            {
                diferencias.Add($"Falta en el documento publicado: {clave} = {valor}");
            }
            else if (otro != valor)
            {
                diferencias.Add($"Distinto: {clave} — contrato: {valor}; documento: {otro}");
            }
        }

        diferencias.AddRange(publicado.Where(p => !contrato.ContainsKey(p.Key))
            .Select(p => $"Sobra en el documento publicado: {p.Key} = {p.Value}"));
        return diferencias;
    }

    private static SortedDictionary<string, string> ProyectarOperacion(string clave, JsonNode? operacion)
    {
        var lineas = new SortedDictionary<string, string>(StringComparer.Ordinal);
        lineas[$"{clave}"] = "operación";

        foreach (var parametro in operacion?["parameters"] as JsonArray ?? [])
        {
            var nombre = parametro?["name"]?.ToString();
            var en = parametro?["in"]?.ToString();
            if (en == "header" && string.Equals(nombre, "X-Correlation-ID", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var prefijo = $"{clave} parámetro {en}:{nombre}";
            lineas[prefijo] = $"required={Normalizar(parametro?["required"]) ?? "false"}";
            Unir(lineas, AplanarEsquema(parametro?["schema"], $"{prefijo} esquema"));
        }

        ProyectarContenido(lineas, $"{clave} solicitud", operacion?["requestBody"]?["content"] as JsonObject);
        foreach (var (codigo, respuesta) in operacion?["responses"] as JsonObject ?? [])
        {
            lineas[$"{clave} respuesta {codigo}"] = "declarada";
            ProyectarContenido(lineas, $"{clave} respuesta {codigo}", respuesta?["content"] as JsonObject);
        }

        return lineas;
    }

    private static void ProyectarContenido(SortedDictionary<string, string> lineas, string prefijo, JsonObject? contenido)
    {
        foreach (var (tipo, medio) in contenido ?? [])
        {
            lineas[$"{prefijo} contenido {tipo}"] = "declarado";
            Unir(lineas, AplanarEsquema(medio?["schema"], $"{prefijo} contenido {tipo} esquema"));
        }
    }

    private static SortedDictionary<string, string> AplanarEsquema(JsonNode? esquema, string prefijo)
    {
        var lineas = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (esquema is not JsonObject objeto)
        {
            return lineas;
        }

        lineas[prefijo] = "esquema";
        foreach (var clave in new[] { "$ref", "type", "format", "pattern", "minLength", "maxLength", "minimum", "maximum" })
        {
            var valor = Normalizar(objeto[clave]);
            if (valor is not null)
            {
                lineas[$"{prefijo}.{clave}"] = valor;
            }
        }

        if (objeto["required"] is JsonArray requeridos)
        {
            lineas[$"{prefijo}.required"] = string.Join(",", requeridos.Select(r => r?.ToString()).OrderBy(r => r, StringComparer.Ordinal));
        }

        if (objeto["enum"] is JsonArray valores)
        {
            lineas[$"{prefijo}.enum"] = string.Join(",", valores.Select(r => r?.ToString()).OrderBy(r => r, StringComparer.Ordinal));
        }

        foreach (var (nombre, propiedad) in objeto["properties"] as JsonObject ?? [])
        {
            Unir(lineas, AplanarEsquema(propiedad, $"{prefijo}.properties.{nombre}"));
        }

        Unir(lineas, AplanarEsquema(objeto["items"], $"{prefijo}.items"));
        if (objeto["additionalProperties"] is JsonObject adicionales)
        {
            Unir(lineas, AplanarEsquema(adicionales, $"{prefijo}.additionalProperties"));
        }
        else if (objeto["additionalProperties"] is not null)
        {
            lineas[$"{prefijo}.additionalProperties"] = Normalizar(objeto["additionalProperties"])!;
        }

        return lineas;
    }

    private static string? Normalizar(JsonNode? nodo)
    {
        if (nodo is null)
        {
            return null;
        }

        if (nodo is JsonArray arreglo)
        {
            return string.Join(",", arreglo.Select(Normalizar).OrderBy(x => x, StringComparer.Ordinal));
        }

        var texto = nodo.ToString();
        return texto switch
        {
            "True" => "true",
            "False" => "false",
            _ => texto
        };
    }

    private static void Unir(SortedDictionary<string, string> destino, SortedDictionary<string, string> origen)
    {
        foreach (var (k, v) in origen)
        {
            destino[k] = v;
        }
    }
}
