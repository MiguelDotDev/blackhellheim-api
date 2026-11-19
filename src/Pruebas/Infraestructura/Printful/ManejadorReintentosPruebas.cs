using System.Net;
using System.Net.Http.Headers;
using BlackHellHeim.Infraestructura.Printful;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Printful;

internal sealed class RespuestasGuionadas(params Func<HttpResponseMessage>[] guion) : HttpMessageHandler
{
    private int _n;
    public int Llamadas => _n;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c) =>
        Task.FromResult(guion[Math.Min(_n++, guion.Length - 1)]());
}

public class ManejadorReintentosPruebas
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private static (HttpClient Cliente, List<TimeSpan> Esperas) Crear(HttpMessageHandler interno, TimeSpan? tiempo = null)
    {
        var esperas = new List<TimeSpan>();
        var manejador = new ManejadorReintentos(tiempo ?? TimeSpan.FromSeconds(10), new FakeTime(Ahora),
            (t, _) => { esperas.Add(t); return Task.CompletedTask; }) { InnerHandler = interno };
        return (new HttpClient(manejador), esperas);
    }

    private static HttpResponseMessage R(HttpStatusCode c, Action<HttpResponseMessage>? a = null)
    {
        var r = new HttpResponseMessage(c);
        a?.Invoke(r);
        return r;
    }

    [Fact]
    public async Task Un429ConRetryAfterEnSegundosSeReintentaYTieneExito()
    {
        var interno = new RespuestasGuionadas(
            () => R(HttpStatusCode.TooManyRequests, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2))),
            () => R(HttpStatusCode.OK));
        var (cliente, esperas) = Crear(interno);

        var r = await cliente.GetAsync("http://x/");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(2, interno.Llamadas);
        Assert.Equal([TimeSpan.FromSeconds(2)], esperas);
    }

    [Fact]
    public async Task RetryAfterComoFechaSeConvierteEnEspera()
    {
        var interno = new RespuestasGuionadas(
            () => R(HttpStatusCode.TooManyRequests, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(Ahora.AddSeconds(5))),
            () => R(HttpStatusCode.OK));
        var (cliente, esperas) = Crear(interno);

        await cliente.GetAsync("http://x/");

        Assert.Equal([TimeSpan.FromSeconds(5)], esperas);
    }

    [Fact]
    public async Task UnRetryAfterAbsurdoSeRecortaA60Segundos()
    {
        var interno = new RespuestasGuionadas(
            () => R(HttpStatusCode.TooManyRequests, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(5))),
            () => R(HttpStatusCode.OK));
        var (cliente, esperas) = Crear(interno);

        await cliente.GetAsync("http://x/");

        Assert.Equal([TimeSpan.FromSeconds(60)], esperas);
    }

    [Fact]
    public async Task SiElLimiteDuraSeAgotanLosTresReintentosYDevuelveElUltimo429()
    {
        var interno = new RespuestasGuionadas(() => R(HttpStatusCode.TooManyRequests));
        var (cliente, esperas) = Crear(interno);

        var r = await cliente.GetAsync("http://x/");

        Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode);
        Assert.Equal(4, interno.Llamadas);
        Assert.Equal(3, esperas.Count);
    }

    [Fact]
    public async Task Los5xxSeReintentanConEsperaCreciente()
    {
        var interno = new RespuestasGuionadas(() => R(HttpStatusCode.BadGateway), () => R(HttpStatusCode.ServiceUnavailable), () => R(HttpStatusCode.OK));
        var (cliente, esperas) = Crear(interno);

        var r = await cliente.GetAsync("http://x/");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(2, esperas.Count);
        Assert.True(esperas[1] > esperas[0]);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Los4xxDistintosDe429NoSeReintentan(HttpStatusCode codigo)
    {
        var interno = new RespuestasGuionadas(() => R(codigo));
        var (cliente, esperas) = Crear(interno);

        await cliente.GetAsync("http://x/");

        Assert.Equal(1, interno.Llamadas);
        Assert.Empty(esperas);
    }

    [Fact]
    public async Task UnIntentoQueSuperaElTiempoLanzaTimeoutSinReintentar()
    {
        var (cliente, esperas) = Crear(new Lento(), TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<TimeoutException>(() => cliente.GetAsync("http://x/"));
        Assert.Empty(esperas);
    }

    [Fact]
    public async Task LaCancelacionDelLlamadorNoSeConvierteEnTimeout()
    {
        var (cliente, _) = Crear(new Lento());
        using var fuente = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cliente.GetAsync("http://x/", fuente.Token));
    }

    private sealed class Lento : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), c);
            return new HttpResponseMessage();
        }
    }

    private sealed class FakeTime(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
