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

                var cuerpo = await LeerAcotadoAsync(h.Request, c);
                if (cuerpo is null)
                    return Error(new(TipoError.ArchivoDemasiadoGrande, "webhook_demasiado_grande", "El cuerpo supera 1 MB.", null, Id()));

                if (!VerificadorFirmaHmac.EsValida(o.SecretoWebhook, cuerpo, h.Request.Headers[o.CabeceraFirma].ToString()))
                    return Error(ErrorUniforme.NoAutenticado(Id()));

                if (!TryLeerEvento(cuerpo, out var idEvento, out var tipo))
                    return Error(ErrorUniforme.DeValidacion([new("cuerpo", "Se esperaba un JSON con «id» y «type».")], Id()));

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

    private static bool TryLeerEvento(byte[] cuerpo, out string id, out string tipo)
    {
        id = tipo = "";
        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
            var raiz = doc.RootElement;
            id = raiz.TryGetProperty("id", out var i) ? i.ValueKind switch
            {
                JsonValueKind.String => i.GetString() ?? "",
                JsonValueKind.Number => i.GetRawText(),
                _ => ""
            } : "";
            tipo = raiz.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? "" : "";
            return id.Length is > 0 and <= 100 && tipo.Length is > 0 and <= 80;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
