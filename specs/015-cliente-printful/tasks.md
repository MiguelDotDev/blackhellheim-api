# Tareas 015: Cliente de Printful

Etiquetas: [BE] [MANUAL]. Pruebas primero.

## Fase 1
- [X] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla) y verificar que HU-13 está integrada (si no, implementar casos de uso y dejar los endpoints como última tarea).
- [X] T002 [BE] Verificar rutas, campos y límites de la API v2 de Printful en su documentación; anotar en `investigacion.md` (con fecha) y registrar licencia: no hay dependencias nuevas.
- [ ] T003 [MANUAL] Crear cuenta de Printful y token privado; guardar `Printful__Token` en `.env` local (nunca en el repo).

## Fase 2: Aplicación
- [X] T004 [P] [BE] Pruebas de los manejadores `ObtenerProducto`, `ListarProductos`, `ListarVariantes` con doble de `IProveedorImpresion` (éxito, 404, técnica/país inválidos → 400, lista vacía).
- [X] T005 [BE] Puerto, modelos, validadores y manejadores.

## Fase 3: Adaptador
- [X] T006 [P] [BE] Pruebas de `ManejadorReintentos`: 429 con `Retry-After` en segundos y en fecha, tope 60 s, agotamiento → 503, 5xx con espera creciente, 401/403 sin reintento, tiempo agotado → 504.
- [X] T007 [BE] `ManejadorReintentos` y `OpcionesPrintful` (validadas al arrancar).
- [X] T008 [P] [BE] Pruebas del adaptador con JSON de ejemplo: mapeo, filtro técnica+país, JSON inesperado → 502, moneda ≠ USD rechazada, token ausente de mensajes.
- [X] T009 [BE] `ProveedorImpresionPrintful` con caché de 10 min y registro en DI.

## Fase 4: API
- [X] T010 [BE] Pruebas de endpoints (200, 400, 401, 403, 404, 503) con adaptador doble en `FabricaApiPruebas`.
- [X] T011 [BE] `PrintfulAdminEndpoints` con política Administrador (HU-13).
- [X] T012 [BE] `build` y `test` en verde; ningún test usa red.
- [X] T013 [BE] Actualizar backlog (Estado = Hecha) y commit `HU-15: cliente de Printful`.

**Trazabilidad**: RF-001→T004-5; RF-002→T008-9; RF-003→T008; RF-004→T006-7; RF-005→T004,T006; RF-006→T010-11; RF-007→T009; RF-008→T001; CE-001→T006; CE-002→T012.

**Notas de implementación**: `ObtenerDisponibilidadAsync` (RF-001) se integró en `ListarVariantesAsync` (la disponibilidad se usa solo para filtrar). El token ausente no impide arrancar: los endpoints responden 502 `proveedor_fallo`. Verificación contra Printful real: [MANUAL] (T003 y `investigacion.md`).
