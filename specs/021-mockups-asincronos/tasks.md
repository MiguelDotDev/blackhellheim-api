# Tareas 021: Mockups asíncronos

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero.

## Fase 1
- [ ] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla); completar esquemas y quitar `x-pendiente`.
- [ ] T002 [BE] Confirmar rutas y campos de mockup-tasks de Printful v2; anotar en `investigacion.md`; sin dependencias nuevas.
- [ ] T003 [MANUAL] Verificar con token real de Printful un mockup de prueba.
## Fase 2: US1 pedir (P1)
- [ ] T004 [P] [BE] [US1] Pruebas de `PedirMockups` (éxito 202, duplicado 409, sin arte/variantes 400, 404, 403, Printful caído 502).
- [ ] T005 [DB] [US1] `TareaMockup`/`ImagenMockup`, configuración EF, migración `AgregarMockups` (índice filtrado de una pendiente por diseño).
- [ ] T006 [BE] [US1] Extender `IProveedorImpresion` y adaptador/doble; caso de uso y `MockupsAdminEndpoints` (POST).
## Fase 3: US2 y US3 finalizar (P1)
- [ ] T007 [P] [BE] [US2] Pruebas de `FinalizarTareaMockup` (éxito, repetido sin duplicar, tarea fallida, descarga fallida, tipo/tamaño inválido, almacén caído, 10 finalizaciones concurrentes con SQL Server).
- [ ] T008 [BE] [US2] `FinalizarTareaMockup` con actualización condicional, descarga acotada y copia a `IAlmacenArchivos`; manejador de `mockup_task_finished` en el despacho de HU-22.
- [ ] T009 [P] [BE] [US3] Pruebas del polling con reloj falso (espera mínima, completa, tiempo agotado a 30 min, no pisa lo ya finalizado).
- [ ] T010 [BE] [US3] `BackgroundService` de polling y opciones `Mockups__*` validadas al arrancar.
## Fase 4: US4 consulta y cierre
- [ ] T011 [BE] [US4] Pruebas y GET `/admin/disenos/{id}/mockups`.
- [ ] T012 [BE] `build`/`test` en verde; backlog = Hecha; commit `HU-21: mockups asíncronos`.

**Trazabilidad**: RF-001→T004,T006,T011; RF-002→T006; RF-003→T008; RF-004→T009-10; RF-005→T007-8; RF-006→T007-8; RF-007→todas; CE-001→T009; CE-002→T007.
