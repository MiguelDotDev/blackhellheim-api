using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BlackHellHeim.Api.OpenApi;

/// <summary>
/// Ajusta el documento generado: título y versión de la API, esquema canónico de
/// <c>ProblemDetails</c> (el de <c>specs/004-openapi-scalar/contratos/openapi.yaml</c>) y la
/// respuesta <c>500</c> uniforme en todas las operaciones.
/// </summary>
public sealed class TransformadorDocumentoBlackHellHeim : IOpenApiDocumentTransformer
{
    public const string TituloApi = "BlackHellHeim API";
    public const string VersionApi = "0.4.0";

    public Task TransformAsync(OpenApiDocument documento, OpenApiDocumentTransformerContext contexto, CancellationToken cancelacion)
    {
        documento.Info ??= new OpenApiInfo();
        documento.Info.Title = TituloApi;
        documento.Info.Version = VersionApi;

        documento.Components ??= new OpenApiComponents();
        documento.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        documento.Components.Schemas["ProblemDetails"] = EsquemaProblemDetails();

        foreach (var ruta in documento.Paths.Values)
        {
            foreach (var operacion in ruta.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
            {
                // El código de tienda siempre son tres letras mayúsculas (contrato de la historia 002b).
                foreach (var parametro in operacion.Parameters ?? [])
                {
                    if (parametro is OpenApiParameter { Name: "codigo", Schema: OpenApiSchema esquemaCodigo })
                    {
                        esquemaCodigo.Pattern = "^[A-Z]{3}$";
                    }
                }

                operacion.Responses ??= new OpenApiResponses();
                if (!operacion.Responses.ContainsKey("500"))
                {
                    operacion.Responses["500"] = RespuestaProblema(documento, "Error no controlado");
                }
            }
        }

        return Task.CompletedTask;
    }

    private static OpenApiResponse RespuestaProblema(OpenApiDocument documento, string descripcion) => new()
    {
        Description = descripcion,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/problem+json"] = new() { Schema = new OpenApiSchemaReference("ProblemDetails", documento) }
        }
    };

    private static OpenApiSchema EsquemaProblemDetails() => new()
    {
        Type = JsonSchemaType.Object,
        Required = new HashSet<string> { "type", "title", "status", "codigo", "idCorrelacion" },
        Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["type"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri", Description = "URI que identifica el tipo de problema." },
            ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
            ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" },
            ["codigo"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Código estable del error." },
            ["idCorrelacion"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Identificador de correlación de la ejecución." },
            ["errores"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "Errores por campo; solo en errores de validación.",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        }
    };
}
