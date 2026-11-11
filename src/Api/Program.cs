using BlackHellHeim.Api.Comun;
using BlackHellHeim.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AgregarCasosDeUso();

var app = builder.Build();

app.UseMiddleware<MiddlewareCorrelacionYErrores>();

// Comprobación de estado según specs/002-estructura-base-api/contratos/openapi.yaml
app.MapGet("/salud", () => TypedResults.Ok(new { estado = "ok" }));
app.MapearTiendas();

app.Run();

// Requerido por WebApplicationFactory<Program> en las pruebas en proceso.
public partial class Program { }
