using System.Text.Json;
using BlackHellHeim.Api.Comun;
using BlackHellHeim.Aplicacion.Comun.Correlacion;
using BlackHellHeim.Aplicacion.Comun.Errores;
using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Infraestructura.Printful;
using BlackHellHeim.Infraestructura.Webhooks;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlackHellHeim.Api.Endpoints;

/// <summary>Webhooks de proveedores (<c>specs/022-webhooks-printful/contratos/openapi.yaml</c>). Públicos: los autentica la firma.</summary>
public static class WebhooksEndpoints
{
    public const int MaxBytes = 1024 * 1024;

    public static IEndpointRouteBuilder MapearWebhooks(this IEndpointRouteBuilder rutas)
    {
        rutas.MapPost("/webhooks/printful", async (HttpContext h, ISender e, IOptions<OpcionesPrintful> opciones,
                IContextoEjecucion ctx, ILogger<ManejadorRecibirWebhook> registro, CancellationToken c) =>
            {
                var o = opciones.Value;
                string Id() => ctx.IdCorrelacion;
                IResult Error(ErrorUniforme x) => TraductorProblemas.Traducir(x, h.Request.Path.Value ?? "/");

                if (string.IsNullOrEmpty(o.SecretoWebhook))
                    return Error(new(TipoError.NoDisponible, "webhook_no_configurado", "El webhook no está configurado.", null, Id()));

                // Printful entrega el secreto como hexadecimal: se decodifica a bytes antes del HMAC.
                byte[] secreto;
                try { secreto = Convert.FromHexString(o.SecretoWebhook.Trim()); }
                catch (FormatException)
                {
                    registro.LogError("Printful__SecretoWebhook no es hexadecimal válido; webhook deshabilitado.");
                    return Error(new(TipoError.NoDisponible, "webhook_no_configurado", "El webhook no está configurado.", null, Id()));
                }

                var cuerpo = await LeerAcotadoAsync(h.Request, c);
                if (cuerpo is null)
                    return Error(new(TipoError.ArchivoDemasiadoGrande, "webhook_demasiado_grande", "El cuerpo supera 1 MB.", null, Id()));

                // El nombre real de la cabecera de firma está sin verificar: se registran todas para confirmarlo.
                registro.LogInformation("Webhook printful: cabeceras recibidas [{Cabeceras}]; firma leída de «{Usada}».",
                    string.Join(", ", h.Request.Headers.Keys), o.CabeceraFirma);

                if (!VerificadorFirmaHmac.EsValida(secreto, cuerpo, h.Request.Headers[o.CabeceraFirma].ToString()))
                    return Error(ErrorUniforme.NoAutenticado(Id()));

                if (!TryLeerEvento(cuerpo, out var idEvento, out var tipo))
                    return Error(ErrorUniforme.DeValidacion([new("cuerpo", "Se esperaba un JSON objeto con «type».")], Id()));

                var r = await e.Send(new RecibirWebhookComando("printful", idEvento, tipo, System.Text.Encoding.UTF8.GetString(cuerpo)), c);
                if (r.Valor is { CuerpoDistinto: true })
                    registro.LogWarning("Evento printful/{Id} repetido con cuerpo distinto; se conserva el primero.", idEvento);
                return r.EsExito ? Results.Ok() : Error(r.Error!);
            })
            // El nombre real es configurable (Printful:CabeceraFirma); el documento publica el predeterminado.
            .AddOpenApiOperationTransformer((op, _, _) =>
            {
                op.Parameters ??= [];
                op.Parameters.Add(new Microsoft.OpenApi.OpenApiParameter
                {
                    Name = "X-Printful-Signature", In = Microsoft.OpenApi.ParameterLocation.Header, Required = true,
                    Schema = new Microsoft.OpenApi.OpenApiSchema { Type = Microsoft.OpenApi.JsonSchemaType.String }
                });
                return Task.CompletedTask;
            })
            .AllowAnonymous().WithName("recibirWebhookPrintful")
            .Accepts<Dictionary<string, object>>("application/json")
            .Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(413).ProducesProblem(503).ProducesProblem(500);
        return rutas;
    }

    /// <summary>Lee el cuerpo crudo (que es lo que se firma) sin pasar de <see cref="MaxBytes"/>; null si lo excede.</summary>
    private static async Task<byte[]?> LeerAcotadoAsync(HttpRequest peticion, CancellationToken c)
    {
        if (peticion.ContentLength > MaxBytes) return null;
        using var memoria = new MemoryStream();
        var buffer = new byte[8192];
        int n;
        while ((n = await peticion.Body.ReadAsync(buffer, c)) > 0)
        {
            if (memoria.Length + n > MaxBytes) return null;
            memoria.Write(buffer, 0, n);
        }

        return memoria.ToArray();
    }

    /// <summary>
    /// Printful no envía id en la raíz: se deriva de forma determinista (type|id de la tarea u objeto en data|occurred_at);
    /// sin id en data, SHA-256 del cuerpo crudo. shortcut: en el respaldo, un reintento con «retries» distinto cambia el hash.
    /// </summary>
    public static bool TryLeerEvento(byte[] cuerpo, out string id, out string tipo)
    {
        id = tipo = "";
        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            var raiz = doc.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object) return false;
            tipo = Texto(raiz, "type");
            if (tipo.Length is 0 or > 80) return false;

            var objeto = "";
            if (raiz.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object)
            {
                foreach (var nodo in new[] { Hijo(d, "mockup_task"), Hijo(d, "task"), d })
                {
                    objeto = Texto(nodo, "id");
                    if (objeto.Length > 0) break;
                }
            }

            id = objeto.Length > 0 ? $"{tipo}|{objeto}|{Texto(raiz, "occurred_at")}" : "";
            if (id.Length is 0 or > 100) id = "sha256:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(cuerpo));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonElement Hijo(JsonElement e, string n) => e.TryGetProperty(n, out var v) ? v : default;

    private static string Texto(JsonElement e, string n) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v)
            ? v.ValueKind switch { JsonValueKind.String => v.GetString() ?? "", JsonValueKind.Number => v.GetRawText(), _ => "" }
            : "";
}
