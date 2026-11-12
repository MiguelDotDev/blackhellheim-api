using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using BlackHellHeim.Pruebas.Comun;
using Xunit;

namespace BlackHellHeim.Pruebas.Api;

/// <summary>
/// Prueba en proceso de la comprobación de estado según
/// <c>specs/002-estructura-base-api/contratos/openapi.yaml</c>.
/// </summary>
public class SaludPruebas : IClassFixture<FabricaApiPruebas>
{
    private readonly HttpClient _cliente;

    public SaludPruebas(FabricaApiPruebas fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task LaComprobacionDeEstadoRespondeOk()
    {
        var respuesta = await _cliente.GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(cuerpo);
        Assert.Equal("ok", cuerpo!["estado"]);
    }
}
