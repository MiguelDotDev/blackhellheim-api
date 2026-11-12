# Tareas 028: Pedidos con estados e idempotencia

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero (constitución III). Formato: `- [ ] Tnnn [P] [USn] [etiqueta] descripción con ruta`.

## Fase 1: Preparación
- [ ] T001 [BE] Prueba de contrato de `contratos/openapi.yaml` en `src/Pruebas/Api` (falla hasta tener los endpoints); al final quitar `x-pendiente`.
- [ ] T002 [MANUAL] Confirmar con la titular los datos mínimos del domicilio de envío.

## Fase 2: Base común
- [ ] T003 [DB] Dominio `src/Dominio/Pedidos`: `Pedido` con líneas, `EstadoPedido` (`PendientePago, Pagado, EnProduccion, Enviado, Entregado, Cancelado, Reembolsado`) y transiciones válidas (`PendientePago→Pagado|Cancelado`, `Pagado→EnProduccion|Reembolsado`, `EnProduccion→Enviado|Reembolsado`, `Enviado→Entregado`) y `MensajeSalida`; `Pagado` hace inmutables líneas, envío y total.
- [ ] T004 [DB] Configuración EF y migración `AgregarPedidos` con las tablas de `modelo-datos.md`: `Pedidos` (Estado varchar(16), ClaveIdempotencia varchar(64), HuellaCuerpo char(64), montos decimal(18,2), CodigoPostal char(5), único (UsuarioId, ClaveIdempotencia), índice (UsuarioId, CreadoUtc)), `LineasPedido` (Cantidad tinyint entre 1 y 10, único (PedidoId, VarianteId), FK en cascada) y `MensajesSalida` (Tipo varchar(60), ProximoIntentoUtc datetimeoffset nullable, Error varchar(500) nullable, índice (ProcesadoUtc, CreadoUtc)); actualizar la prueba que cuenta migraciones.
- [ ] T005 [BE] Puerto `IRepositorioPedidos` en `src/Aplicacion/Pedidos` y registro en `src/Api/Comun/RegistroPedidos.cs`.

## Fase 3: US1 crear el pedido (P1)
Criterio independiente: con sesión y una clave, crear un pedido calcula los montos en el servidor y repetirlo devuelve el mismo.
- [ ] T006 [P] [BE] [US1] Pruebas de `CrearPedido`: éxito (201, PendientePago, montos del servidor), repetición con misma clave y cuerpo (200, mismo id), misma clave con otro cuerpo (409), sin clave/sin líneas/cantidad fuera de rango/variante inexistente o inactiva/domicilio inválido/método de envío ya no disponible (400), sin tipo de cambio (503), sin sesión (401), un precio enviado por el cliente se ignora.
- [ ] T007 [BE] [US1] Caso de uso `CrearPedido` (valida, recalcula líneas con `ICalculadoraPrecios` y envío con el puerto de HU-26, huella SHA-256, inserción con captura de la violación única y relectura; vaciar el carrito del servidor solo si HU-24 ya está implementada) en `src/Aplicacion/Pedidos` y `src/Infraestructura/Pedidos`.
- [ ] T008 [BE] [US1] `src/Api/Endpoints/PedidosEndpoints.cs` con `POST /pedidos` (cabecera `Idempotency-Key`) y `GET /pedidos/{id}` (de la dueña; ajeno → 404).

## Fase 4: US2 estados y congelación (P1)
Criterio independiente: las transiciones y la congelación se prueban en el dominio y por el caso de uso.
- [ ] T009 [P] [BE] [US2] Pruebas de dominio: cada transición válida y cada inválida, `Pagado` congela montos, `Pagado` dos veces es idempotente, cancelar solo desde `PendientePago`.
- [ ] T010 [BE] [US2] Métodos de transición del dominio y casos de uso internos `MarcarPedidoPagado` y `RecalcularPedidoPendiente` (para HU-29, RF-008) en `src/Aplicacion/Pedidos`.

## Fase 5: US3 outbox (P1)
- [ ] T011 [P] [BE] [US3] Pruebas con SQL Server real en `src/Pruebas/Pedidos`: 20 creaciones simultáneas con la misma clave → un pedido y un mensaje `pedido.creado` (CE-001); crear y pagar escriben `pedido.creado` y `pedido.pagado` en la misma transacción; un fallo no deja pedido sin mensaje.
- [ ] T012 [BE] [US3] Escritura transaccional del mensaje de salida junto al pedido y a cada cambio de estado relevante.

## Fase 6: Cierre
- [ ] T013 [BE] Quitar `x-pendiente`, `dotnet build`/`dotnet test` en verde, tareas marcadas, fila de `docs/scrum/backlog.md` = Hecha y commit `HU-28: pedidos`.

## Dependencias y paralelismo
Fase 2 bloquea el resto; T007 usa el puerto de cotización de HU-26 (si no está implementada, se usa un doble y se documenta); T010 antes de que HU-29 marque pagado. MVP = fases 1–3.

**Trazabilidad**: RF-001→T006–T008; RF-002→T006–T007; RF-003→T003,T009–T010; RF-004→T003,T009; RF-005→T006–T007,T011; RF-006→T011–T012; RF-007→todas; RF-008→T009–T010; CE-001→T011; CE-002→T009; CE-003→T009.
