# Especificación 028: Pedidos con estados e idempotencia

**Historia**: HU-28 (Sprint 8) · **Repos**: api · **Estado**: Spec lista · **Creada**: 2026-11-12

**Entrada**: «Quiero pedidos con estados e idempotencia. Criterio: reintentar un pago no duplica el pedido; el precio queda congelado al pagar; solo usuarios autenticados crean pedidos.»

**Bloqueado por**: HU-13 (sesión, implementada), HU-16 (variantes, implementada), HU-20 (precios, implementada), HU-24 (carrito, spec lista, no implementada) y HU-26 (cotización de envío, spec lista, no implementada). Alimenta a HU-29 (pago), HU-30 (borrador en Printful), HU-31 (código de creadora), HU-32 (envío), HU-34/38 (historial y panel). Constitución VI: la creación del pedido usa tabla outbox.

## Escenarios de uso y pruebas

### HU 1 — Crear el pedido desde el carrito (P1)
1. **Dado** una clienta con sesión, líneas (variante y cantidad), un domicilio mexicano y un método de envío cotizado, **cuando** crea el pedido con una clave de idempotencia, **entonces** se guarda en estado `PendientePago` con los precios calculados por el servidor, el costo de envío recalculado por el servidor, el total y el domicilio, y responde `201`.
2. Con la misma clave de idempotencia y el mismo cuerpo, la repetición devuelve el mismo pedido (`200`) sin crear otro; con la misma clave y un cuerpo distinto → `409`.
3. Sin sesión → `401`; sin clave de idempotencia, sin líneas, variante inexistente o inactiva, cantidad fuera de 1 a 10 o domicilio inválido → `400`; sin tipo de cambio → `503`.

### HU 2 — Estados y precio congelado (P1)
1. Estados: `PendientePago` → `Pagado` → `EnProduccion` → `Enviado` → `Entregado`; `PendientePago` → `Cancelado`; `Pagado`/`EnProduccion` → `Reembolsado`. Cualquier otra transición se rechaza. `Cancelado` y `Reembolsado` existen en el dominio, pero ningún endpoint de esta historia los produce (la cancelación y el reembolso se especificarán con quien los use).
2. Al pasar a `Pagado` los precios, el envío y el total quedan **congelados**: ninguna operación posterior los modifica. Mientras es `PendientePago` los montos solo pueden refrescarse por la operación de HU-29 inmediatamente antes de crear la sesión de pago (RF-008); el total que se cobra es el último calculado y es el que queda congelado al pasar a `Pagado`.
3. Marcar pagado dos veces es idempotente (el segundo intento no cambia nada).

### HU 3 — Outbox al crear y al pagar (P1)
Crear el pedido escribe, en la misma transacción, un mensaje `pedido.creado` en la tabla outbox; al pasar a `Pagado` se escribe `pedido.pagado` (que HU-30 consumirá). Un fallo a mitad no deja pedido sin mensaje ni mensaje sin pedido.

### Casos límite
Dos creaciones simultáneas con la misma clave (una crea, la otra devuelve el mismo pedido); carrito del servidor (HU-24) vaciado tras crear el pedido (solo si la creación fue por primera vez); variante desactivada entre la cotización y la creación (400).

## Requisitos funcionales
- **RF-001** `POST /pedidos` con sesión y cabecera `Idempotency-Key`; `GET /pedidos/{id}` de la dueña (el listado paginado es de HU-34).
- **RF-002** El cliente nunca envía precios ni totales; el servidor calcula líneas y envío (principio V).
- **RF-003** Estados y transiciones válidas como en HU 2; transición inválida → error de dominio.
- **RF-004** Congelación de montos al pasar a `Pagado`.
- **RF-005** Idempotencia por (usuaria, clave) con comparación de la huella del cuerpo.
- **RF-006** Tabla outbox escrita en la misma transacción que el pedido y sus cambios de estado.
- **RF-008** Un pedido `PendientePago` puede refrescar sus montos con los precios y el envío vigentes (operación interna para HU-29); un pedido `Pagado` nunca.
- **RF-007** Pruebas de éxito, error y validación; ningún servicio externo se llama.

## Criterios de éxito
- **CE-001** 20 creaciones simultáneas con la misma clave producen un solo pedido y un solo mensaje `pedido.creado`.
- **CE-002** El 100 % de las transiciones fuera de la tabla de estados se rechaza.
- **CE-003** Tras `Pagado`, el 100 % de los intentos de cambiar montos falla.

## Entidades
Pedido (usuaria, estado, domicilio, método y costo de envío, subtotal, total, clave de idempotencia), línea de pedido (variante, cantidad, precio unitario) y mensaje de outbox.

## Supuestos y fuera de alcance
El pago (HU-29), el borrador en Printful (HU-30), el código de creadora (HU-31, agregará su campo opcional) y las notificaciones quedan fuera. El servidor que despacha el outbox se implementa con cada consumidor (HU-30 el primero); aquí solo se escribe. El domicilio es mexicano (estado, ciudad, código postal de 5 dígitos, calle, teléfono). `[MANUAL]`: confirmar con la titular los datos mínimos del domicilio y el nombre fiscal fuera de alcance (CFDI diferido).
