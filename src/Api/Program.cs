var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Comprobación de estado según specs/002-estructura-base-api/contratos/openapi.yaml
app.MapGet("/salud", () => TypedResults.Ok(new { estado = "ok" }));

app.Run();

// Requerido por WebApplicationFactory<Program> en las pruebas en proceso.
public partial class Program { }
