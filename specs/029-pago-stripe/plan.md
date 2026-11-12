# Plan 029: Pagar con Stripe (parte API)

**Spec**: [spec.md](spec.md) · **Investigación**: [research.md](research.md) · **Modelo**: [modelo-datos.md](modelo-datos.md) · **Contrato**: [contratos/openapi.yaml](contratos/openapi.yaml) · **Validación**: [quickstart.md](quickstart.md)

## Contexto técnico
.NET 10, EF Core, Mediator, `HttpClient`, xUnit; reutiliza HU-22 (eventos) y HU-28 (pedidos). **Dependencias nuevas: ninguna.**

## Revisión de la constitución
| Principio | Resultado |
|---|---|
| I | Cumple: `Dominio/Pagos`, `Aplicacion/Pagos`, adaptador en Infraestructura, DTO en la API. |
| II | Cumple: contrato completo antes del código. |
| III | Cumple (ver Pruebas). |
| IV | Cumple: HTTP directo, sin SDK. |
| V | Cumple: el monto sale del pedido; se compara con lo cobrado antes de marcar pagado. |
| VI | Cumple: firma verificada, evento idempotente, outbox al pagar. |
| VII | Cumple: Stripe solo por `IPasarelaPago`; pruebas con doble. |
| VIII | Cumple: clave y secreto solo por configuración (`Stripe__ClaveSecreta`, `Stripe__SecretoWebhook`); pago solo con sesión. |
| IX–XI | No aplica / cumple. |

## Decisiones
1. `IPasarelaPago.CrearSesionAsync(SolicitudSesionPago)` → `SesionPago(Id, Url, ExpiraUtc)`; adaptador `PasarelaPagoStripe` con `HttpClient` y manejo de errores como el de Printful (502/503/504).
2. `IniciarPago`: valida dueña y estado, llama a `RecalcularPedidoPendiente` (HU-28), reutiliza el intento vigente si existe o crea sesión e intento.
3. Endpoint `POST /webhooks/stripe` con su `VerificadorFirmaStripe`; reutiliza `RecibirWebhook` y la tabla de eventos de HU-22.
4. Consumidores `ManejadorPagoStripe` por tipo de evento: marcan el intento y, si procede y el monto coincide, llaman a `MarcarPedidoPagado` (HU-28).
5. Opciones `Stripe__ClaveSecreta`, `Stripe__SecretoWebhook`, `Stripe__UrlExito`, `Stripe__UrlCancelacion`, `Stripe__MetodosPago` (`card`,`oxxo`), `Stripe__DiasVigenciaOxxo` (3), `Stripe__HorasExpiracion` (24) validadas al arrancar; sin clave el pago responde 503, sin secreto el webhook responde 503.

## Estructura
`Dominio/Pagos`, `Aplicacion/Pagos`, `Infraestructura/Pagos` (adaptador, verificador, repositorio), `Api/Endpoints/PagosEndpoints.cs`, migración `AgregarIntentosPago`.

## Pruebas
Verificador (válida, alterada, ausente, mal formada, marca de tiempo vieja o futura), `IniciarPago` con pasarela doble (éxito, sesión reutilizada, pedido ajeno/inexistente/no pendiente, Stripe caído), consumidores por evento (pagado, diferido, async éxito/fallo, expirado, monto distinto, sesión desconocida), endpoint del webhook (200/400/401/413/500/503), SQL Server (20 entregas paralelas, CE-002), contrato.

## Bloqueos y [MANUAL]
Bloqueado por HU-28 (no implementada: sin ella no se puede construir `IniciarPago` ni `MarcarPedidoPagado`). `[MANUAL]`: cuenta de Stripe, claves (de prueba y reales), activar tarjeta/OXXO/SPEI, registrar el webhook con URL pública (HU-25/HU-40), secretos en Azure y en los flujos de CI (etiqueta CI de la historia).
