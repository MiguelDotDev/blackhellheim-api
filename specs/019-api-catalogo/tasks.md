# Tareas 019: API de catálogo

Etiquetas: [BE] [DB]. Pruebas primero.

- [x] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla); confirmar HU-16 y HU-20 integradas (si HU-20 no, usar `precio=null`).
- [x] T002 [P] [BE] Pruebas de validadores (tamaño 0/51, orden inválido, mín>máx, texto >100) y de manejadores con `IConsultasCatalogo` doble (borrador/archivado excluidos, 404, filtros combinados, orden).
- [x] T003 [BE] Validadores, manejadores y DTOs.
- [x] T004 [P] [BE] Pruebas con Testcontainers: filtros y orden por precio calculado (mín/máx, asc/desc, desempate por id), filtros por color/talla solo de variantes activas, precio mín/máx, paginación fuera de rango, caracteres especiales parametrizados, CE-001 con 1000 diseños sembrados por la prueba.
- [x] T005 [DB] `ConsultasCatalogo` con proyección, cálculo de precio en memoria (filtro/orden/paginación por precio tras calcular) sin migración `IndicesCatalogo`: CE-001 (1000 diseños, 12 elementos, < 300 ms) pasa sin índices nuevos.
- [x] T006 [BE] Pruebas y `CatalogoEndpoints` públicos (sin `RequireAuthorization`), `ETag`, `Cache-Control: public, max-age=60`.
- [x] T007 [BE] Verificar que ningún DTO expone costo base, margen ni ids internos de Printful.
- [x] T008 [BE] `build`/`test` en verde; backlog = Hecha; commit `HU-19: API de catálogo`.

**Trazabilidad**: RF-001→T002-6; RF-002→T002,T006; RF-003→T002,T004; RF-004→T007; RF-005→T006; RF-006→T001-2; RF-007→T004-5; CE-001→T004.
