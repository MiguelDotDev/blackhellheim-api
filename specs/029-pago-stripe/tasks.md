# Tareas 029: Pagar con Stripe (parte API)

Etiquetas: [BE] [DB] [CI] [MANUAL]. Pruebas primero (constitución III). Formato: `- [ ] Tnnn [P] [USn] [etiqueta] descripción con ruta`.

## Fase 1: Preparación
- [ ] T001 [BE] Prueba de contrato de `contratos/openapi.yaml` en `src/Pruebas/Api` (falla hasta tener los endpoints); al final quitar `x-pendiente`.
- [ ] T002 [BE] Crear `specs/029-pago-stripe/investigacion.md` con los supuestos de Stripe que fija el código (rutas, parámetros, eventos, formato de firma) marcados como NO verificados; el razonamiento está en `research.md`.
- [ ] T003 [MANUAL] Crear la cuenta de Stripe, obtener claves de prueba, activar tarjeta y OXXO (y SPEI solo tras verificarlo; luego añadirlo a `Stripe__MetodosPago`), registrar el webhook con la URL pública (HU-25/HU-40) y cargar `Stripe__ClaveSecreta` y `Stripe__SecretoWebhook` en `.env` local, Azure y los secretos de GitHub (etiqueta CI); nunca en el repositorio.

## Fase 2: Base común
- [ ] T004 [DB] Dominio `src/Dominio/Pagos`: `IntentoPago` con estados `Creado`, `Pendiente`, `Pagado`, `Fallido`, `Expirado` y transiciones (`Creado→Pendiente|Pagado|Fallido|Expirado`, `Pendiente→Pagado|Fallido|Expirado`; los demás finales).
- [ ] T005 [DB] Configuración EF y migración `AgregarIntentosPago`: `IntentosPago` con SesionStripeId varchar(100) único, Url varchar(500), Estado varchar(12), Metodo varchar(30) nullable, MontoCentavos bigint, FK a Pedidos en cascada e índice (PedidoId, Estado); actualizar la prueba que cuenta migraciones.
- [ ] T006 [BE] Puerto `IPasarelaPago` en `src/Aplicacion/Pagos` (`CrearSesionAsync` → `SesionPago`), opciones `Stripe__*` validadas al arrancar y `src/Api/Comun/RegistroPagos.cs`.

## Fase 3: US1 iniciar el pago (P1)
Criterio independiente: con pasarela doble, pagar un pedido pendiente devuelve la URL y reutiliza la sesión vigente.
- [ ] T007 [P] [BE] [US1] Pruebas de `IniciarPago` y del endpoint: éxito (200, monto = total del pedido en centavos), sesión vigente reutilizada, sesión expirada → nueva, pedido ajeno o inexistente → 404, pedido que no está `PendientePago` → 409, sin sesión → 401, Stripe caído → 502/504, sin clave → 503.
- [ ] T008 [BE] [US1] Adaptador `PasarelaPagoStripe` en `src/Infraestructura/Pagos` (HTTP directo, formulario, sin dependencias nuevas) con pruebas de JSON de ejemplo.
- [ ] T009 [BE] [US1] Caso de uso `IniciarPago` (usa `RecalcularPedidoPendiente` de HU-28) y `src/Api/Endpoints/PagosEndpoints.cs` con `POST /pedidos/{id}/pago`.

## Fase 4: US2 webhook firmado (P1)
Criterio independiente: la firma inválida se rechaza y el evento repetido no se reprocesa.
- [ ] T010 [P] [BE] [US2] Pruebas de `VerificadorFirmaStripe` (válida, alterada, ausente, mal formada, marca de tiempo a más de 5 min pasada o futura, varios `v1`) y del endpoint (200/400/401/413/500/503).
- [ ] T011 [BE] [US2] `VerificadorFirmaStripe` (HMAC-SHA256 de `marca.cuerpo`, tiempo constante) y `POST /webhooks/stripe` reutilizando `RecibirWebhook` y la tabla de eventos de HU-22 con proveedor `stripe`.

## Fase 5: US3 marcar pagado y pagos diferidos (P1)
Criterio independiente: cada evento deja el intento y el pedido en el estado esperado, sin duplicar.
- [ ] T012 [P] [BE] [US3] Pruebas de `ManejadorPagoStripe`: completed pagado → `Pagado`, completed no pagado (OXXO/SPEI) → intento `Pendiente` y pedido en `PendientePago`, async éxito → `Pagado`, async fallo → intento `Fallido`, expired → `Expirado`, monto cobrado distinto del total → no se marca y se registra, sesión desconocida → 200 sin efectos, evento repetido, segunda sesión pagada del mismo pedido → se registra como duplicada sin tocar el pedido.
- [ ] T013 [BE] [US3] `ManejadorPagoStripe : IManejadorEventoProveedor` (proveedor `stripe`, tipos `checkout.session.*`) y su registro; llama a `MarcarPedidoPagado` de HU-28.
- [ ] T014 [P] [BE] [US3] Prueba con SQL Server real en `src/Pruebas/Pagos`: 20 entregas paralelas del mismo evento → un pedido pagado y un mensaje `pedido.pagado` (CE-002).

## Fase 6: Cierre
- [ ] T015 [BE] Quitar `x-pendiente`, `dotnet build`/`dotnet test` en verde, añadir `Stripe__*` (sin valores) a `compose.yaml`, tareas marcadas, fila de `docs/scrum/backlog.md` en Hecha para la parte API y commit `HU-29: pago con Stripe`.

## Dependencias y paralelismo
Bloqueado por HU-28 (`MarcarPedidoPagado`, `RecalcularPedidoPendiente`); Fase 2 bloquea el resto; US2 es independiente de US1. MVP = fases 1–4 con HU-28.

**Trazabilidad**: RF-001→T007–T011; RF-002→T006,T008; RF-003→T010–T011; RF-004→T011,T014; RF-005→T004–T005,T012–T013; RF-006→T007,T009; RF-007→todas; CE-001→T010; CE-002→T014; CE-003→T012.
