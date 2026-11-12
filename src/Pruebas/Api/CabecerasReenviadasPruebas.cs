using BlackHellHeim.Pruebas.Comun;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>RF-009: con ASPNETCORE_FORWARDEDHEADERS_ENABLED la API respeta X-Forwarded-Proto del proxy.</summary>
public class CabecerasReenviadasPruebas
{
    // Filtro de arranque registrado después del de cabeceras reenviadas: expone el esquema visto.
    private sealed class FiltroEsquema : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => app =>
        {
            app.Use((ctx, sig) =>
            {
                ctx.Response.OnStarting(() => { ctx.Response.Headers["X-Esquema"] = ctx.Request.Scheme; return Task.CompletedTask; });
                return sig();
            });
            siguiente(app);
        };
    }

    private static async Task<string?> EsquemaVisto(bool habilitado)
    {
        using var fabrica = new FabricaApiPruebas().WithWebHostBuilder(b =>
        {
            if (habilitado) b.UseSetting("FORWARDEDHEADERS_ENABLED", "true");
            b.ConfigureServices(s => s.AddTransient<IStartupFilter, FiltroEsquema>());
        });
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/salud");
        peticion.Headers.Add("X-Forwarded-Proto", "https");
        var respuesta = await fabrica.CreateClient().SendAsync(peticion);
        return respuesta.Headers.TryGetValues("X-Esquema", out var v) ? v.Single() : null;
    }

    [Fact]
    public async Task HabilitadoRespetaHttpsDelProxy() => Assert.Equal("https", await EsquemaVisto(true));

    [Fact]
    public async Task DeshabilitadoIgnoraLaCabecera() => Assert.Equal("http", await EsquemaVisto(false));
}
