# Investigación 030 (Fase 0)
Sin llamadas reales; las rutas y campos de Printful son **suposiciones a verificar** (`[MANUAL]`).

| Tema | Decisión | Motivo / alternativas |
|---|---|---|
| Crear el borrador | `POST /v2/orders` con `external_id` = id del pedido, destinatario, `shipping` = método elegido y `order_items` (`source: catalog`, `catalog_variant_id`, `quantity`, `placements` con la capa de archivo del arte); sin confirmar | Un pedido de Printful nace como borrador y se confirma aparte; es lo que pide el criterio. |
| Confirmar | `POST /v2/orders/{id}/confirmation`, solo con `Printful__ConfirmarPedidosReales = true` | Costo real solo con bandera explícita; apagada por omisión, en desarrollo y en CI. |
| Idempotencia | Antes de crear, buscar por `external_id` (`GET /v2/orders?external_id=`); localmente, índice único (PedidoId) en `BorradoresPedido` | Evita duplicados ante reintentos o instancias concurrentes. |
| Despachador | `BackgroundService` que reclama mensajes `pedido.pagado` con actualización condicional (`Intentos`, `ProximoIntentoUtc`) | Constitución VI; seguro con varias instancias, igual que HU-21/22. |
| Reintentos | Espera `30 s × 2^intento` hasta 5 intentos; falla definitiva (diseño sin arte, variante inactiva, domicilio rechazado) no se reintenta | Distingue temporal de definitivo. |
| Dependencias | Ninguna nueva | Principio IV. |
