using BlackHellHeim.Api.Comun;
using BlackHellHeim.Api.Configuracion;
using BlackHellHeim.Api.Endpoints;
using BlackHellHeim.Infraestructura.Persistencia;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AgregarCasosDeUso();

// La API no conecta al arrancar: solo registra el contexto con la cadena validada.
builder.Services.AgregarPersistencia(CadenaConexion.Obtener(builder.Configuration));

var app = builder.Build();

app.UseMiddleware<MiddlewareCorrelacionYErrores>();

// Comprobación de estado según specs/002-estructura-base-api/contratos/openapi.yaml
app.MapGet("/salud", () => TypedResults.Ok(new { estado = "ok" }));
app.MapearTiendas();
app.MapearSaludBaseDatos();

app.Run();

// Requerido por WebApplicationFactory<Program> en las pruebas en proceso.
public partial class Program { }
