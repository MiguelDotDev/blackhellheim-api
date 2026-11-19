using System.Net;

namespace BlackHellHeim.Infraestructura.Printful;

/// <summary>
/// 429 → espera <c>Retry-After</c> (segundos o fecha, tope 60 s); 5xx y fallos de red → espera creciente.
/// Máximo 3 reintentos; 4xx distintos de 429 no se reintentan. Cada intento tiene su propio tiempo
/// de espera; al agotarse lanza <see cref="TimeoutException"/> (sin reintento).
/// </summary>
public sealed class ManejadorReintentos : DelegatingHandler
{
    public const int MaximoReintentos = 3;
    public static readonly TimeSpan TopeEspera = TimeSpan.FromSeconds(60);

    private readonly TimeSpan _tiempoPorIntento;
    private readonly TimeProvider _reloj;
    private readonly Func<TimeSpan, CancellationToken, Task> _esperar;

    public ManejadorReintentos(TimeSpan tiempoPorIntento, TimeProvider reloj, Func<TimeSpan, CancellationToken, Task>? esperar = null)
    {
        _tiempoPorIntento = tiempoPorIntento;
        _reloj = reloj;
        _esperar = esperar ?? Task.Delay;
    }

    /// <summary>Segundos que sugiere Retry-After (con tope), o null si no viene.</summary>
    public static int? SegundosSugeridos(HttpResponseMessage respuesta, TimeProvider reloj)
    {
        var espera = respuesta.Headers.RetryAfter;
        if (espera is null) return null;
        var tiempo = espera.Delta ?? (espera.Date is { } fecha ? fecha - reloj.GetUtcNow() : (TimeSpan?)null);
        return tiempo is null ? null : (int)Math.Ceiling(Math.Clamp(tiempo.Value.TotalSeconds, 0, TopeEspera.TotalSeconds));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancelacion)
    {
        for (var intento = 0; ; intento++)
        {
            HttpResponseMessage? respuesta = null;
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
                limite.CancelAfter(_tiempoPorIntento);
                try
                {
                    respuesta = await base.SendAsync(solicitud, limite.Token);
                }
                catch (OperationCanceledException) when (!cancelacion.IsCancellationRequested)
                {
                    throw new TimeoutException("El proveedor no respondió a tiempo.");
                }
            }
            catch (HttpRequestException) when (intento < MaximoReintentos)
            {
                await _esperar(Creciente(intento), cancelacion);
                continue;
            }

            var reintentable = respuesta.StatusCode == HttpStatusCode.TooManyRequests || (int)respuesta.StatusCode >= 500;
            if (!reintentable || intento >= MaximoReintentos)
            {
                return respuesta;
            }

            var espera = respuesta.StatusCode == HttpStatusCode.TooManyRequests
                ? TimeSpan.FromSeconds(SegundosSugeridos(respuesta, _reloj) ?? Creciente(intento).TotalSeconds)
                : Creciente(intento);
            respuesta.Dispose();
            await _esperar(espera, cancelacion);
        }
    }

    // 0,5 · 2^n s con jitter de hasta 20 %.
    private static TimeSpan Creciente(int intento) =>
        TimeSpan.FromSeconds(0.5 * Math.Pow(2, intento) * (1 + Random.Shared.NextDouble() * 0.2));
}
