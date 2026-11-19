using BlackHellHeim.Api.Comun;
using BlackHellHeim.Api.Configuracion;
using BlackHellHeim.Api.Endpoints;
using BlackHellHeim.Api.Errores;
using BlackHellHeim.Api.OpenApi;
using BlackHellHeim.Aplicacion.Tiendas.ConsultarTienda;
using BlackHellHeim.Infraestructura.Identidad;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AgregarCasosDeUso();

// La API no conecta al arrancar: solo registra el contexto con la cadena validada.
builder.Services.AgregarPersistencia(CadenaConexion.Obtener(builder.Configuration));

// Los números se serializan como números estrictos (el contrato declara integer, no integer|string).
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);

builder.Services.AgregarIdentidad(builder.Configuration);
builder.Services.AgregarAutenticacionJwt();
builder.Services.AgregarPrintful(builder.Configuration);
builder.Services.AgregarArte(builder.Configuration);
builder.Services.AgregarPrecios(builder.Configuration);
builder.Services.AddHostedService<SembradoAdministrador>();

builder.Services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = OpcionesProblemDetails.Configurar);
builder.Services.AddExceptionHandler<ManejadorExcepcionesGlobal>();

builder.Services.AddOpenApi("v1", opciones =>
{
    // El contrato (specs/002b) llama "Tienda" al esquema que la API expone como TiendaDto.
    opciones.CreateSchemaReferenceId = tipo =>
        tipo.Type == typeof(TiendaDto) ? "Tienda" : OpenApiOptions.CreateDefaultSchemaReferenceId(tipo);
    opciones.AddDocumentTransformer<TransformadorDocumentoBlackHellHeim>();
});

var app = builder.Build();

app.UseMiddleware<MiddlewareCorrelacionYErrores>();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(opciones => opciones
    .WithTitle("BlackHellHeim API")
    .DisableAgent()
    .DisableMcp()
    .DisableTelemetry()
    .AddHeaderContent(AvisoDocumento.Html));
app.MapearSalud();
app.MapearTiendas();
app.MapearAuth();
app.MapearPrintfulAdmin();
app.MapearDisenosAdmin();
app.MapearArteAdmin();
app.MapearPreciosAdmin();
app.MapearCatalogo();
app.MapearSaludBaseDatos();

if (!app.Environment.IsProduction())
{
    // Endpoint de prueba de los errores inesperados (historia 004): nunca existe en Production
    // y no aparece en el documento OpenAPI.
    app.MapGet("/pruebas/error", () =>
        {
            throw new InvalidOperationException("secreto-interno en C:\\ruta\\Clase.cs");
        })
        .ExcludeFromDescription();

    // Recurso de prueba para verificar el 403 por rol (nunca existe en Production).
    app.MapGet("/pruebas/solo-administrador", () => Results.NoContent())
        .RequireAuthorization(politica => politica.RequireRole(BlackHellHeim.Dominio.Autenticacion.Roles.Administrador))
        .ExcludeFromDescription();
}

app.Run();

// Requerido por WebApplicationFactory<Program> en las pruebas en proceso.
public partial class Program { }
