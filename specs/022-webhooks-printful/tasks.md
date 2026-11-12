# Tareas 022: Webhooks de Printful

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero.

## Fase 1
- [x] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla); quitar `x-pendiente` al implementar.
- [x] T002 [BE] Formato de firma anotado en `investigacion.md` (suposición NO verificada contra Printful real; confirmar en T003 [MANUAL]).
- [ ] T003 [MANUAL] Definir `Printful__SecretoWebhook` en `.env` y secretos de Azure; registrar el webhook en Printful con la URL pública (requiere HU-25/HU-40).
## Fase 2: US1 y US2 (P1)
- [x] T004 [P] [BE] [US2] Pruebas de `VerificadorFirmaHmac` (válida, alterada, ausente, mal formada, hex inválido) y de cuerpo >1 MB, JSON inválido, sin id/tipo y marca de tiempo fuera de 5 min (si Printful la envía).
- [x] T005 [BE] [US2] `VerificadorFirmaHmac` (HMAC-SHA256, `CryptographicOperations.FixedTimeEquals`) en `src/Infraestructura/Webhooks`.
- [x] T006 [P] [BE] [US1] Pruebas de `RecibirWebhook` con repositorio y consumidores dobles (consumidor por tipo, tipo sin consumidor, `Procesado`).
- [x] T007 [DB] [US1] `EventoWebhook` en Dominio, configuración EF y migración `AgregarEventosWebhook` (índice único Proveedor+IdExterno).
- [x] T008 [BE] [US1] Puerto `IManejadorEventoProveedor`, caso de uso `RecibirWebhook` y `WebhooksEndpoints` (`POST /webhooks/printful`, 503 sin secreto).
## Fase 3: US3 idempotencia (P1)
- [x] T009 [P] [BE] [US3] Pruebas: repetido → 200 sin reprocesar; consumidor que falla → `Fallido` y 500; reintento reprocesa; 20 entregas paralelas con SQL Server real (CE-002); mismo id con cuerpo distinto conserva el primero y registra advertencia.
- [x] T010 [BE] [US3] Inserción con captura de violación única y relectura; reintento de `Fallido`.
## Fase 4: Cierre
- [x] T011 [BE] `build`/`test` en verde; backlog = Hecha; commit `HU-22: webhooks de Printful`.

**Trazabilidad**: RF-001→T008; RF-002→T004-5; RF-003→T007; RF-004→T006,T008; RF-005→T009-10; RF-006→todas; CE-001→T004; CE-002→T009.
