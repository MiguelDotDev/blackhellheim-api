# Investigación 029 (Fase 0)
Sin llamadas reales; los nombres de campos y eventos de Stripe son **suposiciones a verificar** con una cuenta de prueba (`[MANUAL]`).

| Tema | Decisión | Motivo / alternativas |
|---|---|---|
| Cliente de Stripe | HTTP directo (`POST /v1/checkout/sessions`, formulario `application/x-www-form-urlencoded`, `Authorization: Bearer <clave>`), sin SDK | Sin dependencia nueva (principio IV); el SDK `Stripe.net` (MIT) quedaría como alternativa si crece el uso. |
| Sesión | `mode=payment`, `currency=mxn`, una sola línea con el total del pedido (`unit_amount` en centavos), `payment_method_types[]` tomados de `Stripe__MetodosPago` (por omisión `card` y `oxxo`; SPEI/`customer_balance` queda fuera hasta verificarlo: puede exigir un cliente de Stripe), `client_reference_id` = id del pedido, `metadata[pedido_id]`, `expires_at` a `Stripe__HorasExpiracion` (24 h) y `payment_method_options[oxxo][expires_after_days]` = `Stripe__DiasVigenciaOxxo` (3, a verificar), `success_url`/`cancel_url` de configuración | OXXO y SPEI son pagos diferidos. Una sola línea evita discrepancias de redondeo; el detalle vive en el pedido. |
| Firma del webhook | Cabecera `Stripe-Signature: t=<marca>,v1=<hex>`; HMAC-SHA256 de `"{t}.{cuerpo}"` con el secreto `whsec`; tolerancia 5 min; comparación en tiempo constante; se acepta cualquier `v1` válido | Esquema documentado de Stripe; verificación propia, igual que HU-22. |
| Eventos | `checkout.session.completed` (`payment_status` = `paid` o `unpaid`), `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`, `checkout.session.expired` | Cubren tarjeta y pagos diferidos. |
| Idempotencia | Se reutiliza `EventosWebhook` y `RecibirWebhook` de HU-22 con proveedor `stripe` e id de evento `evt_...`, y consumidores `IManejadorEventoProveedor` | Constitución VI; una sola política de reintentos. |
| Intentos | Tabla `IntentosPago` (Creado/Pendiente/Pagado/Fallido/Expirado) | Permite reintentar un pago y reconciliar. |
| Monto | Se compara `amount_total` (centavos) con `TotalMxn × 100` del pedido antes de marcar pagado | CE-003; defensa ante manipulación. |
| Dependencias | Ninguna nueva | Principio IV. |
