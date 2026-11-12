# Especificación 029: Pagar con Stripe (parte API)

**Historia**: HU-29 (Sprint 8) · **Repos**: api + web (**este documento cubre solo la parte API**; la etiqueta CI se cubre con las tareas `[MANUAL]` de secretos) · **Estado**: Spec lista · **Creada**: 2026-11-12

**Entrada**: «Como cliente, quiero pagar con Stripe. Criterio: crea la sesión de Stripe Checkout, redirige, valida el webhook y marca el pedido pagado; los pagos diferidos (OXXO, SPEI) quedan pendientes.»

**Bloqueado por**: HU-28 (pedidos, spec lista, no implementada: aporta `Pedido`, `MarcarPedidoPagado` y `RecalcularPedidoPendiente`), HU-22 (tabla de eventos e idempotencia, implementada). Cuenta de Stripe, claves y registro del webhook son `[MANUAL]` (y la URL pública depende de HU-25/HU-40). **Parte faltante en `blackhellheim-web`**: el botón Pagar, la redirección a Stripe y las páginas de éxito y cancelación. Alimenta a HU-30 (borrador en Printful, consume `pedido.pagado`).

## Escenarios de uso y pruebas

### HU 1 — Iniciar el pago (P1)
1. **Dado** una clienta con sesión y un pedido suyo en `PendientePago`, **cuando** pide pagar, **entonces** el servidor refresca los montos (RF-008 de HU-28), crea una sesión de Stripe Checkout por el total del pedido en MXN con los métodos configurados (por omisión tarjeta y OXXO; SPEI se agrega al confirmarlo, ver supuestos), guarda el intento de pago y devuelve la URL de Stripe.
2. Si ya hay una sesión vigente del mismo pedido, se devuelve la misma URL (no se crea otra); si la anterior expiró o falló, se crea una nueva.
3. Pedido ajeno o inexistente → `404`; pedido que no está en `PendientePago` → `409`; sin sesión → `401`; Stripe caído → `502`/`504`.

### HU 2 — Recibir y validar el webhook (P1)
1. **Dado** un evento con firma válida de Stripe, **cuando** llega a `POST /webhooks/stripe`, **entonces** se registra una sola vez por su id de evento y se procesa.
2. Firma ausente, mal formada, incorrecta o con marca de tiempo fuera de 5 minutos → `401` sin registrar nada; cuerpo no JSON o sin id/tipo → `400`; más de 1 MB → `413`; sin secreto configurado → `503`.
3. Un evento repetido responde `200` sin reprocesar; si el procesamiento falla, el evento queda `Fallido` y se responde `500` para que Stripe reintente (misma política que HU-22).

### HU 3 — Marcar el pedido pagado, con pagos diferidos (P1)
1. `checkout.session.completed` con pago realizado → el pedido pasa a `Pagado` (montos congelados, mensaje `pedido.pagado` en el outbox), solo si el monto cobrado coincide con el total del pedido; si no coincide, no se marca y se registra la discrepancia.
2. `checkout.session.completed` con pago aún no realizado (OXXO/SPEI: ficha emitida) → el intento queda `Pendiente` y el pedido sigue en `PendientePago`.
3. `checkout.session.async_payment_succeeded` → `Pagado`; `checkout.session.async_payment_failed` → el intento queda `Fallido` y la clienta puede iniciar otro; `checkout.session.expired` → el intento queda `Expirado`.
4. El evento de una sesión que no corresponde a ningún intento se acepta (200) sin efectos.

### Casos límite
Pago de un pedido que ya está `Pagado` (idempotente); dos sesiones del mismo pedido pagadas (la segunda se registra como duplicada para reembolso manual, `[MANUAL]`); montos con centavos (se envían en centavos enteros); tipo de cambio cambió entre la cotización y el pago (por eso se refresca antes de crear la sesión).

## Requisitos funcionales
- **RF-001** `POST /pedidos/{id}/pago` (sesión de la dueña) → `{ url }`; `POST /webhooks/stripe` (público, solo firma).
- **RF-002** Pasarela por el puerto `IPasarelaPago` (crear sesión); Stripe nunca se llama en pruebas.
- **RF-003** Verificación de la firma `Stripe-Signature` (HMAC-SHA256 sobre `marca.cuerpo`, tolerancia 5 min, tiempo constante) con el secreto del webhook.
- **RF-004** Idempotencia y reintentos con la tabla de eventos de HU-22 (proveedor `stripe`).
- **RF-005** Intentos de pago con estados `Creado`, `Pendiente`, `Pagado`, `Fallido`, `Expirado`.
- **RF-006** El monto de la sesión sale del pedido en el servidor; el cliente no envía montos (principio V).
- **RF-007** Pruebas de éxito, error y validación con dobles; claves y secretos solo por configuración, nunca en el repositorio.

## Criterios de éxito
- **CE-001** El 100 % de las firmas inválidas de la tabla de pruebas se rechaza sin efectos.
- **CE-002** 20 entregas paralelas del mismo evento de pago dejan un solo pedido pagado y un solo mensaje `pedido.pagado`.
- **CE-003** Ningún pedido pasa a `Pagado` con un monto cobrado distinto de su total.

## Entidades
Intento de pago (pedido, sesión de Stripe, estado, método, monto) y evento recibido (de HU-22).

## Supuestos y fuera de alcance
Moneda MXN; métodos configurables con `Stripe__MetodosPago` (por omisión `card` y `oxxo`). SPEI en Stripe puede requerir un cliente de Stripe y otro tipo de método: se activa solo tras verificarlo con la cuenta (`[MANUAL]`); el manejo de pagos diferidos es el mismo. Habilitar los métodos en el panel de Stripe también es `[MANUAL]`. Reembolsos, disputas y facturación (CFDI diferido) quedan fuera. Los nombres de campos y eventos de Stripe se confirman contra su documentación con una cuenta de prueba (`[MANUAL]`). Las URLs de éxito y cancelación las define la web (configuración `Stripe__UrlExito` y `Stripe__UrlCancelacion`).
