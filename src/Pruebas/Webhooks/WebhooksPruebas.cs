using System.Net;
using System.Security.Cryptography;
using System.Text;
using BlackHellHeim.Aplicacion.Webhooks;
using BlackHellHeim.Dominio.Webhooks;
using BlackHellHeim.Infraestructura.Webhooks;
using BlackHellHeim.Pruebas.Comun;
using BlackHellHeim.Pruebas.Dobles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Webhooks;

/// <summary>Historia 022: firma, caso de uso y endpoint con dobles (Printful nunca se llama).</summary>
public class WebhooksPruebas : IClassFixture<FabricaApiPruebas>
{
    private static readonly string Secreto = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    private const string Cuerpo = """{"type":"mockup_task_finished","occurred_at":"t1","data":{"n":1}}""";

    private static readonly string IdDerivado = "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Cuerpo)));

    private static string Firma(string cuerpo, string? secreto = null) =>
        Convert.ToHexString(HMACSHA256.HashData(Convert.FromHexString(secreto ?? Secreto), Encoding.UTF8.GetBytes(cuerpo))).ToLowerInvariant();

    // --- Verificador (CE-001) ---
    [Fact]
    public void LaFirmaValidaSeAceptaEnMinusculasYMayusculas()
    {
        var bytes = Encoding.UTF8.GetBytes(Cuerpo);
        Assert.True(VerificadorFirmaHmac.EsValida(Convert.FromHexString(Secreto), bytes, Firma(Cuerpo)));
        Assert.True(VerificadorFirmaHmac.EsValida(Convert.FromHexString(Secreto), bytes, Firma(Cuerpo).ToUpperInvariant()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("zz")]
    public void LasFirmasAusentesOMalFormadasSeRechazan(string? firma) =>
        Assert.False(VerificadorFirmaHmac.EsValida(Convert.FromHexString(Secreto), Encoding.UTF8.GetBytes(Cuerpo), firma));

    [Fact]
    public void UnaFirmaConHexInvalidoDe64CaracteresSeRechaza() =>
        Assert.False(VerificadorFirmaHmac.EsValida(Convert.FromHexString(Secreto), Encoding.UTF8.GetBytes(Cuerpo), new string('g', 64)));

    [Fact]
    public void LaFirmaDeOtroCuerpoOSecretoOSinSecretoSeRechaza()
    {
        var bytes = Encoding.UTF8.GetBytes(Cuerpo);
        Assert.False(VerificadorFirmaHmac.EsValida(Convert.FromHexString(Secreto), bytes, Firma(Cuerpo + " ")));
        Assert.False(VerificadorFirmaHmac.EsValida(Convert.FromHexString(Secreto), bytes, Firma(Cuerpo, "ABCD")));
        Assert.False(VerificadorFirmaHmac.EsValida([], bytes, Firma(Cuerpo, "")));
    }

    // --- Caso de uso ---
    private static (ManejadorRecibirWebhook M, RepositorioEventosEnMemoria R, RelojFalso T) Manejador(params ConsumidorFalso[] consumidores)
    {
        var repo = new RepositorioEventosEnMemoria();
        var reloj = new RelojFalso(new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        return (new ManejadorRecibirWebhook(repo, consumidores, reloj, new ContextoEjecucionFalso { IdCorrelacion = "c" }), repo, reloj);
    }

    private static RecibirWebhookComando Cmd(string id = "ev-1", string tipo = "mockup_task_finished", string cuerpo = Cuerpo) =>
        new("printful", id, tipo, cuerpo);

    [Fact]
    public async Task UnEventoNuevoLlegaAlConsumidorDeSuTipoYQuedaProcesado()
    {
        var mockup = new ConsumidorFalso("printful", "mockup_task_finished");
        var otro = new ConsumidorFalso("printful", "package_shipped");
        var (m, repo, _) = Manejador(mockup, otro);

        var r = await m.Handle(Cmd(), default);

        Assert.True(r.EsExito);
        Assert.False(r.Valor!.Repetido);
        Assert.Equal(1, mockup.Llamadas);
        Assert.Equal(0, otro.Llamadas);
        Assert.Equal(EstadoEvento.Procesado, repo.Estado("ev-1"));
    }

    [Fact]
    public async Task UnTipoSinConsumidorSeRegistraYResponde200()
    {
        var (m, repo, _) = Manejador(new ConsumidorFalso("printful", "otro_tipo"));

        var r = await m.Handle(Cmd(tipo: "desconocido"), default);

        Assert.True(r.EsExito);
        Assert.Equal(EstadoEvento.Procesado, repo.Estado("ev-1"));
    }

    [Fact]
    public async Task UnEventoRepetidoNoSeReprocesa()
    {
        var c = new ConsumidorFalso("printful", "mockup_task_finished");
        var (m, _, _) = Manejador(c);

        await m.Handle(Cmd(), default);
        var segunda = await m.Handle(Cmd(), default);

        Assert.True(segunda.EsExito);
        Assert.True(segunda.Valor!.Repetido);
        Assert.Equal(1, c.Llamadas);
    }

    [Fact]
    public async Task ElMismoIdConCuerpoDistintoConservaElPrimeroYLoMarca()
    {
        var c = new ConsumidorFalso("printful", "mockup_task_finished");
        var (m, repo, _) = Manejador(c);
        await m.Handle(Cmd(), default);

        var r = await m.Handle(Cmd(cuerpo: """{"id":"ev-1","type":"mockup_task_finished","data":{"x":1}}"""), default);

        Assert.True(r.Valor!.CuerpoDistinto);
        Assert.Equal(1, c.Llamadas);
        Assert.Equal(1, repo.Total);
    }

    [Fact]
    public async Task UnConsumidorQueFallaDejaElEventoFallidoYElReintentoLoReprocesa()
    {
        var c = new ConsumidorFalso("printful", "mockup_task_finished") { Fallo = () => new InvalidOperationException("boom") };
        var (m, repo, _) = Manejador(c);

        var primera = await m.Handle(Cmd(), default);
        Assert.False(primera.EsExito);
        Assert.Equal(EstadoEvento.Fallido, repo.Estado("ev-1"));

        c.Fallo = () => null;
        var reintento = await m.Handle(Cmd(), default);
        Assert.True(reintento.EsExito);
        Assert.False(reintento.Valor!.Repetido);
        Assert.Equal(2, c.Llamadas);
        Assert.Equal(EstadoEvento.Procesado, repo.Estado("ev-1"));
    }

    [Fact]
    public async Task UnEventoEnCursoNoSeReprocesaPeroUnoHuerfanoSi()
    {
        var c = new ConsumidorFalso("printful", "mockup_task_finished");
        var (m, repo, reloj) = Manejador(c);
        // Simula un proceso caído: el evento quedó Recibido sin terminar.
        await repo.AgregarAsync(EventoWebhook.Crear("printful", "ev-1", "mockup_task_finished", Cuerpo, reloj.Ahora), default);

        var enCurso = await m.Handle(Cmd(), default);
        Assert.True(enCurso.Valor!.Repetido);
        Assert.Equal(0, c.Llamadas);

        reloj.Ahora = reloj.Ahora.AddMinutes(6);
        var huerfano = await m.Handle(Cmd(), default);
        Assert.False(huerfano.Valor!.Repetido);
        Assert.Equal(1, c.Llamadas);
    }

    [Fact]
    public async Task VeinteEntregasParalelasConDobleProcesanUnaVez()
    {
        var c = new ConsumidorFalso("printful", "mockup_task_finished");
        var (m, _, _) = Manejador(c);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(async () => await m.Handle(Cmd(), default))));

        Assert.Equal(1, c.Llamadas);
    }

    // --- Endpoint ---
    private (HttpClient Cliente, RepositorioEventosEnMemoria Repo, ConsumidorFalso Consumidor) Api(string? secreto = null)
    {
        var repo = new RepositorioEventosEnMemoria();
        var consumidor = new ConsumidorFalso("printful", "mockup_task_finished");
        var derivada = _fabrica.WithWebHostBuilder(b =>
        {
            if (secreto is not null) b.UseSetting("Printful:SecretoWebhook", secreto);
            b.ConfigureServices(s =>
            {
                s.AddSingleton<IRepositorioEventos>(repo);
                s.AddSingleton<IManejadorEventoProveedor>(consumidor);
            });
        });
        return (derivada.CreateClient(), repo, consumidor);
    }

    private readonly FabricaApiPruebas _fabrica;

    public WebhooksPruebas(FabricaApiPruebas fabrica) => _fabrica = fabrica;

    private static Task<HttpResponseMessage> Enviar(HttpClient cliente, string cuerpo, string? firma, string cabecera = "X-Printful-Signature")
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/webhooks/printful")
        { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };
        if (firma is not null) peticion.Headers.Add(cabecera, firma);
        return cliente.SendAsync(peticion);
    }

    [Fact]
    public async Task UnEventoFirmadoSeAceptaYUnaRepeticionTambienPeroSeProcesaUnaVez()
    {
        var (cliente, repo, consumidor) = Api(Secreto);

        var primera = await Enviar(cliente, Cuerpo, Firma(Cuerpo));
        var segunda = await Enviar(cliente, Cuerpo, Firma(Cuerpo));

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(1, consumidor.Llamadas);
        Assert.Equal(1, repo.Total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-hex")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public async Task UnaFirmaAusenteOInvalidaDa401SinRegistrarNada(string? firma)
    {
        var (cliente, repo, consumidor) = Api(Secreto);

        var r = await Enviar(cliente, Cuerpo, firma);

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, repo.Total);
        Assert.Equal(0, consumidor.Llamadas);
    }

    [Fact]
    public async Task UnCuerpoAlteradoDespuesDeFirmarDa401()
    {
        var (cliente, _, _) = Api(Secreto);

        var r = await Enviar(cliente, Cuerpo.Replace("\"n\":1", "\"n\":2"), Firma(Cuerpo));

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Theory]
    [InlineData("no es json")]
    [InlineData("[]")]
    [InlineData("""{"id":"x"}""")]
    [InlineData("""{"type":""}""")]
    [InlineData("""{"type":{"a":1}}""")]
    public async Task UnCuerpoFirmadoSinJsonOSinIdOTipoDa400(string cuerpo)
    {
        var (cliente, repo, _) = Api(Secreto);

        var r = await Enviar(cliente, cuerpo, Firma(cuerpo));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(0, repo.Total);
    }

    [Fact]
    public async Task UnSecretoNoHexadecimalDa503()
    {
        var (cliente, repo, _) = Api("no-es-hex");

        var r = await Enviar(cliente, Cuerpo, Firma(Cuerpo));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.DoesNotContain("no-es-hex", await r.Content.ReadAsStringAsync());
        Assert.Equal(0, repo.Total);
    }

    // --- Id derivado (Printful no manda id en la raíz) ---
    private static string IdDe(string json)
    {
        Assert.True(BlackHellHeim.Api.Endpoints.WebhooksEndpoints.TryLeerEvento(Encoding.UTF8.GetBytes(json), out var id, out _));
        return id;
    }

    [Fact]
    public void ElIdSeDerivaDeTipoObjetoYOccurredAtYEsDeterminista()
    {
        const string a = """{"type":"mockup_task_finished","occurred_at":"2026-11-01T10:00:00Z","retries":0,"data":{"mockup_task":{"id":77}}}""";
        var reintento = a.Replace("\"retries\":0", "\"retries\":2");

        Assert.Equal("mockup_task_finished|77|2026-11-01T10:00:00Z", IdDe(a));
        Assert.Equal(IdDe(a), IdDe(reintento));
        Assert.NotEqual(IdDe(a), IdDe(a.Replace("77", "78")));
    }

    [Fact]
    public void SinIdEnDataElIdEsElHashDelCuerpo()
    {
        const string a = """{"type":"package_shipped","data":{"x":1}}""";

        Assert.StartsWith("sha256:", IdDe(a));
        Assert.Equal(IdDe(a), IdDe(a));
        Assert.NotEqual(IdDe(a), IdDe(a.Replace("1", "2")));
    }

    [Fact]
    public async Task UnCuerpoMayorA1MbDa413()
    {
        var (cliente, _, _) = Api(Secreto);
        var grande = "{\"id\":\"g\",\"type\":\"t\",\"data\":\"" + new string('a', 1024 * 1024) + "\"}";

        var r = await Enviar(cliente, grande, Firma(grande));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, r.StatusCode);
    }

    [Fact]
    public async Task SinSecretoConfiguradoElEndpointDa503()
    {
        var (cliente, repo, _) = Api();

        var r = await Enviar(cliente, Cuerpo, Firma(Cuerpo, ""));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal(0, repo.Total);
    }

    [Fact]
    public async Task UnConsumidorQueFallaDa500YElReintentoDelProveedorFunciona()
    {
        var (cliente, repo, consumidor) = Api(Secreto);
        consumidor.Fallo = () => new InvalidOperationException("boom");

        var falla = await Enviar(cliente, Cuerpo, Firma(Cuerpo));
        consumidor.Fallo = () => null;
        var reintento = await Enviar(cliente, Cuerpo, Firma(Cuerpo));

        Assert.Equal(HttpStatusCode.InternalServerError, falla.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reintento.StatusCode);
        Assert.Equal(EstadoEvento.Procesado, repo.Estado(IdDerivado));
    }

    [Fact]
    public async Task LaCabeceraDeFirmaEsConfigurable()
    {
        var repo = new RepositorioEventosEnMemoria();
        var cliente = _fabrica.WithWebHostBuilder(b =>
        {
            b.UseSetting("Printful:SecretoWebhook", Secreto);
            b.UseSetting("Printful:CabeceraFirma", "X-Otra");
            b.ConfigureServices(s => s.AddSingleton<IRepositorioEventos>(repo));
        }).CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(cliente, Cuerpo, Firma(Cuerpo))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Enviar(cliente, Cuerpo, Firma(Cuerpo), "X-Otra")).StatusCode);
    }
}
