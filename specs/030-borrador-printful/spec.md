# Especificación 030: Borrador del pedido en Printful tras un pago aprobado

**Historia**: HU-30 (Sprint 8) · **Repos**: api · **Estado**: Spec lista · **Creada**: 2026-11-12

**Entrada**: «Quiero que un pago aprobado cree el borrador en Printful. Criterio: el pedido se crea como borrador con external_id; confirmar solo si la bandera real está activa.»

**Bloqueado por**: HU-28 (pedidos y outbox, spec lista, no implementada), HU-29 (pago, spec lista, no implementada: produce el mensaje `pedido.pagado` al marcar pagado), HU-15 (cliente Printful, implementada), HU-17 (arte con URL pública, implementada). Cuenta y token de Printful son `[MANUAL]`. Alimenta a HU-32 (envíos, usa el `external_id`).

## Escenarios de uso y pruebas

### HU 1 — Crear el borrador cuando el pago se aprueba (P1)
1. **Dado** un mensaje de outbox `pedido.pagado`, **cuando** el despachador lo procesa, **entonces** se crea en Printful un pedido en **borrador** (no confirmado) con `external_id` igual al id del pedido, el domicilio, el método de envío elegido y, por cada línea, la variante de Printful, la cantidad y el arte del diseño (URL pública); el pedido pasa a `EnProduccion` solo si la bandera real está activa (ver HU 2) y, con la bandera apagada, queda `Pagado` con el borrador registrado.
2. Se guarda el id del pedido en Printful y el estado del borrador (`Creado`).
3. El mensaje se marca procesado solo si el borrador quedó creado; si Printful falla, el mensaje se reintenta con espera creciente (hasta 5 intentos) y queda con el error visible.

### HU 2 — Confirmar solo con la bandera real (P1)
1. Con `Printful__ConfirmarPedidosReales = false` (valor por omisión y el de desarrollo y CI) el pedido nunca se confirma: el borrador no genera costo ni envío.
2. Con la bandera en `true`, tras crear el borrador se confirma el pedido en Printful y el pedido pasa a `EnProduccion`; confirmar dos veces es idempotente.

### HU 3 — Idempotencia (P1)
Procesar dos veces el mismo mensaje no crea dos borradores: se usa el `external_id` como llave (si Printful ya lo tiene, se reutiliza el existente) y el registro local impide repetir. Diez despachadores concurrentes producen un solo borrador.

### Casos límite
Diseño sin arte o variante inactiva al momento de despachar (el mensaje falla de forma definitiva y se anota, sin reintentar); domicilio rechazado por Printful (falla definitiva con el motivo); Printful saturado (429, usa el reintento de HU-15); pedido cancelado o reembolsado antes de procesar (se descarta el mensaje).

## Requisitos funcionales
- **RF-001** Despachador en segundo plano que consume mensajes `pedido.pagado` de la tabla outbox con reclamo condicional (seguro con varias instancias).
- **RF-002** Ampliación del puerto `IProveedorImpresion` con crear, consultar y confirmar pedido por `external_id`.
- **RF-003** Registro local del borrador (pedido, id de Printful, estado `Creado`/`Confirmado`/`Fallido`, motivo).
- **RF-004** Bandera `Printful__ConfirmarPedidosReales` apagada por omisión; ninguna prueba ni entorno de CI la enciende.
- **RF-005** Reintentos con espera creciente y tope de intentos; fallas definitivas distinguidas de las temporales (un mensaje con `ProcesadoUtc` y `Error` no nulo es una falla definitiva; con `Error` nulo, un éxito).
- **RF-006** Pruebas de éxito, error y validación con Printful doble; nunca red real.

## Criterios de éxito
- **CE-001** Con la bandera apagada, el 100 % de los pedidos pagados termina con borrador y ninguno confirmado.
- **CE-002** 10 despachadores concurrentes del mismo mensaje producen un solo borrador.
- **CE-003** El 100 % de los mensajes fallidos queda con motivo visible tras agotar los intentos.

## Entidades
Borrador de pedido en el proveedor (pedido, id externo, estado, motivo) y mensaje de outbox (de HU-28).

## Supuestos y fuera de alcance
Riesgo de continuidad: con la bandera apagada el pedido queda en `Pagado` y la tabla de estados de HU-28 no permite pasar de `Pagado` a `Enviado`; por eso HU-32 (envíos), HU-38 (marcar entregado) y HU-42 (flujo completo) quedan **bloqueadas por esta decisión** en desarrollo: deberán simular la confirmación o definir cómo avanzar sin ella, sin que esta historia invente transiciones. Las rutas, campos y la forma de crear borradores de Printful (v2) no están verificados (`[MANUAL]` con token). Para que Printful lea el arte la URL debe ser pública (en local con Azurite no lo es). Reembolsos y cancelación en Printful quedan fuera. El mapeo de tallas o colores usa el `PrintfulVarianteId` guardado en la variante.
