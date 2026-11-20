using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Impresion;
using BlackHellHeim.Aplicacion.Mockups;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlackHellHeim.Infraestructura.Printful;

/// <summary>Adaptador HTTP de la API v2 de catálogo de Printful (rutas verificadas el 2026-11-11, T002).</summary>
public sealed class ProveedorImpresionPrintful(
    HttpClient http, IOptions<OpcionesPrintful> opciones, IMemoryCache cache, TimeProvider reloj, IContextoEjecucion contexto,
    OpcionesMockups? mockups = null, ILogger<ProveedorImpresionPrintful>? log = null)
    : IProveedorImpresion
{
    private readonly OpcionesPrintful _opciones = opciones.Value;
    private readonly OpcionesMockups _mockups = mockups ?? new OpcionesMockups();

    // Mockups (HU-21): rutas y campos de Printful v2 SIN verificar con token real ([MANUAL]); ver specs/021-mockups-asincronos/research.md.
    public async Task<string> CrearTareaMockupAsync(SolicitudMockup solicitud, CancellationToken cancelacion)
    {
        var cuerpo = new
        {
            format = "png",
            products = solicitud.Productos.Select(p => new
            {
                source = "catalog",
                catalog_product_id = p.ProductoId,
                catalog_variant_ids = p.VarianteIds,
                placements = new[]
                {
                    new
                    {
                        placement = solicitud.Posicion,
                        technique = solicitud.Tecnica,
                        layers = new[] { new { type = "file", url = solicitud.ArteUrl } }
                    }
                }
            })
        };
        var raiz = await ObtenerAsync("/v2/mockup-tasks", cancelacion, HttpMethod.Post, System.Net.Http.Json.JsonContent.Create(cuerpo), deTienda: true);
        var primero = raiz is null ? default : Datos(raiz.Value).FirstOrDefault();
        var id = primero.ValueKind == JsonValueKind.Object && primero.TryGetProperty("id", out var i)
            ? i.ValueKind == JsonValueKind.String ? i.GetString() : i.ValueKind == JsonValueKind.Number ? i.GetRawText() : null
            : null;
        return string.IsNullOrWhiteSpace(id) || id.Length > 60 ? throw Fallo() : id;
    }

    public async Task<ResultadoTareaMockup?> ConsultarTareaMockupAsync(string tareaId, CancellationToken cancelacion)
    {
        var raiz = await ObtenerAsync($"/v2/mockup-tasks?id={Uri.EscapeDataString(tareaId)}", cancelacion, deTienda: true);
        var t = raiz is null ? default : Datos(raiz.Value).FirstOrDefault();
        if (t.ValueKind != JsonValueKind.Object) return null;
        var estado = Texto(t, "status").ToLowerInvariant() switch
        {
            "completed" => EstadoMockupProveedor.Completada,
            "failed" => EstadoMockupProveedor.Fallida,
            _ => EstadoMockupProveedor.Pendiente
        };
        var motivo = t.TryGetProperty("failure_reasons", out var f) && f.ValueKind is JsonValueKind.Array or JsonValueKind.Object
            ? f.GetRawText() : null;
        var mockups = new List<MockupGenerado>();
        if (t.TryGetProperty("catalog_variant_mockups", out var porVariante) && porVariante.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in porVariante.EnumerateArray())
            {
                if (!v.TryGetProperty("catalog_variant_id", out var vid) || vid.ValueKind != JsonValueKind.Number
                    || !v.TryGetProperty("mockups", out var lista) || lista.ValueKind != JsonValueKind.Array) continue;
                foreach (var m in lista.EnumerateArray())
                {
                    if (Uri.TryCreate(Texto(m, "mockup_url"), UriKind.Absolute, out var url))
                        mockups.Add(new MockupGenerado(vid.GetInt32(), Texto(m, "placement"), url));
                }
            }
        }

        return new ResultadoTareaMockup(tareaId, estado, motivo, mockups);
    }

    public async Task<byte[]?> DescargarImagenAsync(Uri url, long maxBytes, CancellationToken cancelacion)
    {
        if (url.Scheme != Uri.UriSchemeHttps || !_mockups.HostsDescargaPermitidos.Contains(url.Host, StringComparer.OrdinalIgnoreCase))
            return null;
        // Sin Authorization: el token de Printful nunca viaja al CDN.
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, url);
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.SendAsync(solicitud, HttpCompletionOption.ResponseHeadersRead, cancelacion);
        }
        catch (TimeoutException)
        {
            throw new ExcepcionCasoDeUso(ErrorUniforme.ProveedorTiempoAgotado(contexto.IdCorrelacion));
        }
        catch (HttpRequestException)
        {
            throw Fallo();
        }

        using (respuesta)
        {
            if (!respuesta.IsSuccessStatusCode) throw Fallo(); // p. ej. URL temporal caducada: transitorio
            if (respuesta.Content.Headers.ContentLength > maxBytes) return null;
            await using var flujo = await respuesta.Content.ReadAsStreamAsync(cancelacion);
            using var memoria = new MemoryStream();
            var buffer = new byte[81920];
            int n;
            while ((n = await flujo.ReadAsync(buffer, cancelacion)) > 0)
            {
                if (memoria.Length + n > maxBytes) return null;
                memoria.Write(buffer, 0, n);
            }

            return memoria.ToArray();
        }
    }

    public Task<IReadOnlyList<ProductoPrintfulDto>> ListarProductosAsync(
        string categoria, int pagina, int tamano, CancellationToken cancelacion) =>
        Cacheado($"productos|{categoria}|{pagina}|{tamano}", async () =>
        {
            // "playeras" y los ids numéricos se filtran en Printful (category_ids), antes de paginar.
            var ids = categoria.Equals("playeras", StringComparison.OrdinalIgnoreCase)
                ? string.Join(',', _opciones.CategoriasPlayeras)
                : categoria.All(c => char.IsAsciiDigit(c) || c == ',') ? categoria : null;
            var ruta = $"/v2/catalog-products?limit={tamano}&offset={(pagina - 1) * tamano}"
                + (ids is null ? "" : $"&category_ids={ids}");
            var raiz = await ObtenerAsync(ruta, cancelacion);
            if (raiz is null) return [];
            // shortcut: texto libre se filtra en memoria tras paginar (puede dar páginas vacías); usar ids de categoría.
            return (IReadOnlyList<ProductoPrintfulDto>)Datos(raiz.Value).Select(Producto)
                .Where(p => ids is not null || p.Tipo.Contains(categoria, StringComparison.OrdinalIgnoreCase))
                .ToList();
        });

    public async Task<ProductoPrintfulDto?> ObtenerProductoAsync(int id, CancellationToken cancelacion)
    {
        var clave = $"producto|{id}";
        if (cache.TryGetValue(clave, out ProductoPrintfulDto? guardado)) return guardado;
        var raiz = await ObtenerAsync($"/v2/catalog-products/{id}", cancelacion);
        if (raiz is null) return null;
        var producto = Producto(Contenido(raiz.Value));
        cache.Set(clave, producto, TimeSpan.FromMinutes(_opciones.MinutosCache));
        return producto;
    }

    public async Task<IReadOnlyList<VariantePrintfulDto>?> ListarVariantesAsync(
        int productoId, string tecnica, string pais, CancellationToken cancelacion)
    {
        var clave = $"variantes|{productoId}|{tecnica}|{pais}";
        if (cache.TryGetValue(clave, out IReadOnlyList<VariantePrintfulDto>? guardado)) return guardado;

        var variantes = await ObtenerAsync($"/v2/catalog-products/{productoId}/catalog-variants?limit=100", cancelacion);
        if (variantes is null) return null;
        var disponibilidad = await ObtenerAsync($"/v2/catalog-products/{productoId}/availability?limit=100", cancelacion);
        var precios = await ObtenerAsync($"/v2/catalog-products/{productoId}/prices", cancelacion);

        var regiones = _opciones.RegionesPorPais.TryGetValue(pais, out var r) ? r : ["worldwide"];
        var porVariante = Datos(disponibilidad ?? default).ToDictionary(
            d => d.GetProperty("catalog_variant_id").GetInt32(), d => d);
        var costos = CostosPorVariante(precios, tecnica);

        var resultado = new List<VariantePrintfulDto>();
        foreach (var v in Datos(variantes.Value))
        {
            var id = v.GetProperty("id").GetInt32();
            if (!porVariante.TryGetValue(id, out var disp) || !costos.TryGetValue(id, out var costo)
                || !DisponiblePara(disp, tecnica, regiones))
            {
                continue;
            }

            resultado.Add(new VariantePrintfulDto(id, productoId, Texto(v, "color"), Texto(v, "color_code"),
                Texto(v, "size"), costo, tecnica, true));
        }

        cache.Set(clave, (IReadOnlyList<VariantePrintfulDto>)resultado, TimeSpan.FromMinutes(_opciones.MinutosCache));
        return resultado;
    }

    private static bool DisponiblePara(JsonElement disponibilidad, string tecnica, string[] regiones) =>
        disponibilidad.TryGetProperty("techniques", out var tecnicas)
        && tecnicas.EnumerateArray().Any(t =>
            string.Equals(Texto(t, "technique"), tecnica, StringComparison.OrdinalIgnoreCase)
            && t.TryGetProperty("selling_regions", out var zonas)
            && zonas.EnumerateArray().Any(z => regiones.Contains(Texto(z, "name"))
                && !string.Equals(Texto(z, "availability"), "not_available", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(Texto(z, "availability"), "discontinued", StringComparison.OrdinalIgnoreCase)));

    private Dictionary<int, decimal> CostosPorVariante(JsonElement? precios, string tecnica)
    {
        var costos = new Dictionary<int, decimal>();
        if (precios is null) return costos;
        var contenido = Contenido(precios.Value);
        if (Texto(contenido, "currency") is { Length: > 0 } moneda && !moneda.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            throw Fallo();
        }

        if (!contenido.TryGetProperty("variants", out var variantes)) return costos;
        foreach (var v in variantes.EnumerateArray())
        {
            foreach (var t in v.GetProperty("techniques").EnumerateArray())
            {
                if (string.Equals(Texto(t, "technique_key"), tecnica, StringComparison.OrdinalIgnoreCase)
                    && decimal.TryParse(Texto(t, "price"), System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var precio))
                {
                    costos[v.GetProperty("id").GetInt32()] = precio;
                }
            }
        }

        return costos;
    }

    private async Task<IReadOnlyList<T>> Cacheado<T>(string clave, Func<Task<IReadOnlyList<T>>> obtener)
    {
        if (cache.TryGetValue(clave, out IReadOnlyList<T>? guardado) && guardado is not null) return guardado;
        var valor = await obtener();
        cache.Set(clave, valor, TimeSpan.FromMinutes(_opciones.MinutosCache));
        return valor;
    }

    /// <summary>GET autenticado. Null en 404; el resto de fallos se traducen a errores uniformes sin exponer el token.</summary>
    private async Task<JsonElement?> ObtenerAsync(string ruta, CancellationToken cancelacion, HttpMethod? metodo = null, HttpContent? cuerpo = null, bool deTienda = false)
    {
        if (string.IsNullOrWhiteSpace(_opciones.Token))
        {
            throw new ExcepcionCasoDeUso(ErrorUniforme.ProveedorFallo(contexto.IdCorrelacion));
        }

        using var solicitud = new HttpRequestMessage(metodo ?? HttpMethod.Get, new Uri(new Uri(_opciones.UrlBase), ruta)) { Content = cuerpo };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opciones.Token);
        if (deTienda && !string.IsNullOrWhiteSpace(_opciones.IdTienda)) solicitud.Headers.Add("X-PF-Store-Id", _opciones.IdTienda);
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.SendAsync(solicitud, cancelacion);
        }
        catch (TimeoutException)
        {
            throw new ExcepcionCasoDeUso(ErrorUniforme.ProveedorTiempoAgotado(contexto.IdCorrelacion));
        }
        catch (HttpRequestException)
        {
            throw Fallo();
        }

        using (respuesta)
        {
            if (deTienda && (int)respuesta.StatusCode >= 400) await RegistrarRechazoAsync(respuesta, cancelacion);
            if (respuesta.StatusCode == HttpStatusCode.NotFound) return null;
            if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var segundos = ManejadorReintentos.SegundosSugeridos(respuesta, reloj) ?? 60;
                throw new ExcepcionCasoDeUso(ErrorUniforme.ProveedorSaturado(segundos, contexto.IdCorrelacion));
            }

            if (!respuesta.IsSuccessStatusCode) throw Fallo();
            try
            {
                using var documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(cancelacion), cancellationToken: cancelacion);
                return documento.RootElement.Clone();
            }
            catch (JsonException)
            {
                throw Fallo();
            }
        }
    }

    /// <summary>Warning con código y primeros 500 caracteres del cuerpo; nunca cabeceras ni token.</summary>
    private async Task RegistrarRechazoAsync(HttpResponseMessage respuesta, CancellationToken cancelacion)
    {
        var texto = await respuesta.Content.ReadAsStringAsync(cancelacion);
        log?.LogWarning("Printful rechazó la llamada de tienda: HTTP {Codigo}, cuerpo: {Cuerpo}",
            (int)respuesta.StatusCode, texto.Length > 500 ? texto[..500] : texto);
    }

    private ExcepcionCasoDeUso Fallo() => new(ErrorUniforme.ProveedorFallo(contexto.IdCorrelacion));

    private static JsonElement Contenido(JsonElement raiz) => raiz.TryGetProperty("data", out var d) ? d : raiz;

    private static IEnumerable<JsonElement> Datos(JsonElement raiz) =>
        raiz.ValueKind == JsonValueKind.Undefined ? [] :
        raiz.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array ? d.EnumerateArray().ToList() : [];

    private static string Texto(JsonElement e, string nombre) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()! : "";

    private ProductoPrintfulDto Producto(JsonElement e)
    {
        try
        {
            var tecnicas = e.TryGetProperty("techniques", out var t) && t.ValueKind == JsonValueKind.Array
                ? t.EnumerateArray().Select(x => Texto(x, "key")).Where(x => x.Length > 0).ToList()
                : [];
            return new ProductoPrintfulDto(e.GetProperty("id").GetInt32(), Texto(e, "name"), Texto(e, "type"), tecnicas);
        }
        catch (Exception excepcion) when (excepcion is KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw Fallo();
        }
    }
}
