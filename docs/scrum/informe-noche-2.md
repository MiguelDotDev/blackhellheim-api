# Informe de la noche 2

Fecha: 2026-11-12. Nadie hizo `git push`. Sin `--amend`, sin tocar `sprint/01` a `sprint/05` (solo se leyó `sprint/05` para fusionarla en `sprint/06`).

## Parte A · Implementadas (rama `sprint/06`)
| HU | Commit | Pruebas | Notas |
|---|---|---|---|
| (fusión `sprint/05` → `sprint/06`, sin conflictos) | `0acc029` | — | merge commit |
| 22 Webhooks de Printful | `afe92f7` | 507 en verde | `POST /webhooks/printful`, HMAC-SHA256 sobre el cuerpo crudo, tabla `EventosWebhook` idempotente, migración `AgregarEventosWebhook`, 34 pruebas nuevas (incluye 20 entregas paralelas en SQL Server real) |
| 21 Ajustes tras análisis | `18cfaf4` | — | `/speckit.plan`, `/speckit.tasks` y `/speckit.analyze` ejecutados con los skills; hallazgos medios y bajos corregidos (RF-008 anti-SSRF, reintentos, variante inexistente, riesgo del id de evento) |
| 21 Mockups asíncronos | `27af8a0` | 573 en verde | `POST/GET /admin/disenos/{id}/mockups`, finalización idempotente compartida por el evento y el polling, copia a Blob con nombres deterministas, descarga por puerto con lista de hosts, migración `AgregarMockups`, 66 pruebas nuevas (incluye 10 finalizaciones concurrentes en SQL Server real) |

## Parte B · Specs solo API (sin implementar), cada una con specify, plan, tasks y analyze ejecutados con los skills
| Sprint | Rama | HU | Commit |
|---|---|---|---|
| 7 | `sprint/07` | 24 Carrito (`specs/024-carrito-servidor`) | `21b29f3` |
| 7 | `sprint/07` | 26 Cotizar envío (`specs/026-cotizar-envio`) | `76e579e` |
| 8 | `sprint/08` | 28 Pedidos (`specs/028-pedidos`) | `c84afd7` |
| 8 | `sprint/08` | 29 Pago con Stripe (`specs/029-pago-stripe`) | `c383bda` |
| 8 | `sprint/08` | 30 Borrador en Printful (`specs/030-borrador-printful`) | `1a12d6d` |

**No hechas (me quedé sin capacidad, no por bloqueo):** sprint 9 (31, 32, 34, 38), sprint 10 (36, 35, 37, en ese orden y al final), sprint 11 (40, 42, 43, 44, 45). Las ramas `sprint/09` a `sprint/11` no existen. Quedan por fuera por ser solo web: 27, 33, 39 y 41. La 44 ya tiene carpetas (`specs/900-aviso-privacidad-diferido`, `specs/901-cfdi-diferido`): al retomarla basta verificar sus plantillas. Cada HU exige cuatro ejecuciones de skills con instrucciones largas; por eso el avance fue de cinco HU.

## [MANUAL] (lo haces tú)
- Token de Printful, `Printful__SecretoWebhook` y registro del webhook (requiere URL pública, HU-25/HU-40, no implementadas); verificar con cuenta real los formatos que quedaron como suposición: firma y `id` de evento (HU-22), rutas y forma de mockups y evento (HU-21), `shipping-rates` (HU-26), `POST /v2/orders` (HU-30).
- Definir `BHH_JWT_CLAVE` (el compose la exige; yo usé una temporal solo en la terminal). No hay `.env` con valores reales en el repo.
- Cuenta de Stripe, claves, activar tarjeta y OXXO (SPEI solo tras verificarlo), registrar el webhook, cargar secretos en Azure y GitHub (HU-29).
- Política de aviso aduanal y de margen sobre el envío (HU-26); datos mínimos del domicilio (HU-28).
- Coordinar con `blackhellheim-web` el carrito local y la fusión (HU-24).
- Encender `Printful__ConfirmarPedidosReales` solo cuando lo decidas (HU-30; apagada por omisión).

## Bloqueos y dependencias
- HU-29 y HU-30 dependen de HU-28; HU-28 de HU-24 y HU-26 (las tres con spec lista, sin implementar). Orden de implementación sugerido: 24 → 26 → 28 → 29 → 30.
- HU-21 en uso real depende de HU-22 y de una URL pública del arte; HU-22 de HU-25/HU-40.
- Con `Printful__ConfirmarPedidosReales` apagada el pedido se queda en `Pagado` y la tabla de estados de HU-28 no permite `Pagado → Enviado`: HU-32, HU-38 y HU-42 quedan bloqueadas por esa decisión (anotado en la spec de HU-30; no se inventaron transiciones).

## Hallazgos
- Constitución: ninguna historia la contradice. HU-30 no necesita contrato porque no agrega endpoints.
- HU-22: Printful v1 podría no enviar `id` de evento ni firma HMAC; si no lo hace, un evento real recibiría 400. El polling de HU-21 es la garantía. Detalle en `specs/022-webhooks-printful/investigacion.md`.
- HU-21: el polling vive en `src/Api/Comun/RegistroMockups.cs` (no en Infraestructura, que no referencia Mediator) y el cliente HTTP de Printful dejó de seguir redirecciones (`AllowAutoRedirect = false`) para que la descarga no salga de la lista de hosts.
- HU-24: el catálogo de HU-19 no expone `varianteId`; la spec de HU-24 incluye ese cambio compatible al contrato 019.
- HU-28: la regla «congelar al pagar» se aclaró (un único refresco permitido antes de la sesión de pago); `ProximoIntentoUtc` quedó en el modelo de HU-28.
- HU-29: SPEI en Stripe puede exigir un cliente de Stripe; los métodos son configurables y SPEI queda fuera hasta verificarlo.
- `.specify/feature.json` cambia con cada `specify` (lo escriben los skills); está en los commits.
- compose.yaml: se añadieron `Printful__SecretoWebhook` y `Mockups__*` sin valores reales.

## Licencias
No se agregó ninguna dependencia nueva en esta noche (solo biblioteca estándar de .NET y paquetes ya existentes: Azure.Storage.Blobs, SkiaSharp y Testcontainers, todos MIT). Las specs 024 a 030 tampoco requieren dependencias nuevas (Stripe por HTTP directo, sin SDK).

## Push (cuando lo apruebes)
```bash
git push origin sprint/06
git push origin sprint/07
git push origin sprint/08
```
