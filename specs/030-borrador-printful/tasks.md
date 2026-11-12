# Tareas 030: Borrador del pedido en Printful

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero (constitución III). Formato: `- [ ] Tnnn [P] [USn] [etiqueta] descripción con ruta`.

## Fase 1: Preparación
- [ ] T001 [BE] Crear `specs/030-borrador-printful/investigacion.md` con los supuestos de Printful que fija el código (`POST /v2/orders`, `external_id`, confirmación, búsqueda por `external_id`) marcados como NO verificados; el razonamiento está en `research.md`.
- [ ] T002 [MANUAL] Verificar con token real un borrador de prueba, la URL pública del arte y decidir cuándo encender `Printful__ConfirmarPedidosReales`; no versionar valores.

## Fase 2: Base común
- [ ] T003 [DB] Dominio `src/Dominio/Pedidos/BorradorPedido` (PedidoId, PrintfulOrderId, Estado `Creado`/`Confirmado`/`Fallido`, Motivo) y configuración EF con migración `AgregarBorradoresPedido`: `PrintfulOrderId` varchar(60), `Estado` varchar(12), `Motivo` varchar(300) nullable, único (PedidoId), único filtrado (PrintfulOrderId); (la columna `ProximoIntentoUtc` de `MensajesSalida` la crea HU-28); actualizar la prueba que cuenta migraciones.
- [ ] T004 [BE] Ampliar `IProveedorImpresion` con `CrearPedidoBorradorAsync`, `ObtenerPedidoPorExternalIdAsync` y `ConfirmarPedidoAsync` (y tipos propios del puerto); actualizar el adaptador `src/Infraestructura/Printful` y todos los dobles existentes.
- [ ] T005 [BE] Opciones `Printful__ConfirmarPedidosReales` (false) y `Salida__IntervaloSegundos` (15) validadas al arrancar y `src/Api/Comun/RegistroSalida.cs`.

## Fase 3: US1 borrador al pagar (P1)
Criterio independiente: con Printful doble, un mensaje `pedido.pagado` deja el borrador `Creado` sin confirmar.
- [ ] T006 [P] [BE] [US1] Pruebas de `CrearBorradorPrintful`: éxito con bandera apagada (borrador `Creado`, pedido sigue `Pagado`), pedido cancelado o reembolsado → mensaje descartado, diseño sin arte → falla definitiva con motivo, variante inactiva → falla definitiva, domicilio rechazado → falla definitiva, Printful caído → falla temporal reintentable.
- [ ] T007 [BE] [US1] Adaptador `CrearPedidoBorradorAsync` (con JSON de ejemplo y pruebas) y caso de uso `CrearBorradorPrintful` en `src/Aplicacion/Pedidos` y `src/Infraestructura/Pedidos`.
- [ ] T008 [BE] [US1] `ServicioDespachoSalida : BackgroundService` en `src/Api/Comun/RegistroSalida.cs`: reclama mensajes `pedido.pagado` con actualización condicional y reintenta con espera `30 s × 2^intento` hasta 5 intentos, anotando el error.

## Fase 4: US2 confirmar solo con bandera (P1)
- [ ] T009 [P] [BE] [US2] Pruebas: bandera apagada nunca confirma (CE-001), bandera encendida confirma y pasa el pedido a `EnProduccion`, confirmar dos veces es idempotente.
- [ ] T010 [BE] [US2] Confirmación condicionada por `Printful__ConfirmarPedidosReales` en `CrearBorradorPrintful` y adaptador `ConfirmarPedidoAsync`.

## Fase 5: US3 idempotencia (P1)
- [ ] T011 [P] [BE] [US3] Pruebas: mismo mensaje dos veces no crea dos borradores; Printful ya tiene el `external_id` → se reutiliza; con SQL Server real, 10 despachadores concurrentes producen un solo borrador (CE-002) y los mensajes fallidos conservan su motivo (CE-003) en `src/Pruebas/Pedidos`.
- [ ] T012 [BE] [US3] Búsqueda previa por `external_id` y registro local único para garantizar un solo borrador.

## Fase 6: Cierre
- [ ] T013 [BE] `dotnet build`/`dotnet test` en verde, `Printful__ConfirmarPedidosReales` sin valor real en `compose.yaml`, tareas marcadas, fila de `docs/scrum/backlog.md` = Hecha y commit `HU-30: borrador en Printful`.

## Dependencias y paralelismo
Bloqueado por HU-28 (outbox, pedido) y HU-29 (produce `pedido.pagado`); Fase 2 bloquea el resto. MVP = fases 1–3.

**Trazabilidad**: RF-001→T008; RF-002→T004; RF-003→T003; RF-004→T005,T009–T010; RF-005→T006,T008; RF-006→todas; CE-001→T009; CE-002→T011; CE-003→T011.
