namespace BlackHellHeim.Api.OpenApi;

/// <summary>
/// Aviso que la página de Scalar muestra cuando no puede cargar el documento OpenAPI (RF-009).
/// El aviso está oculto; un script lo muestra si <c>/openapi/v1.json</c> no responde 2xx o falla.
/// </summary>
public static class AvisoDocumento
{
    public const string Mensaje =
        "No se pudo cargar el contrato OpenAPI de BlackHellHeim API. Verifica que /openapi/v1.json responda e inténtalo de nuevo.";

    public static readonly string Html =
        $$"""
        <div id="aviso-documento" role="alert" hidden style="margin:1rem;padding:1rem;border:1px solid #b91c1c;color:#b91c1c;font-family:sans-serif">{{Mensaje}}</div>
        <script>
        (function () {
            var aviso = document.getElementById('aviso-documento');
            function mostrar() { aviso.removeAttribute('hidden'); }
            fetch('/openapi/v1.json').then(function (r) { if (!r.ok) { mostrar(); } }).catch(mostrar);
        })();
        </script>
        """;
}
