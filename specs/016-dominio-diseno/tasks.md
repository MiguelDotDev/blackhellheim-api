# Tareas 016: Dominio de diseño

Etiquetas: [BE] [DB]. Pruebas primero.

## Fase 1
- [ ] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla); confirmar HU-13 integrada.
## Fase 2: Dominio
- [ ] T002 [P] [BE] Pruebas de `Diseno`: crear, nombre 3–80, slug, publicar sin arte/sin variantes (error), mismo producto base, quitar última variante publicada, archivar/restaurar.
- [ ] T003 [BE] `Diseno`, `ProductoBase`, `Variante`, `ArchivoArte`, `EstadoDiseno`.
## Fase 3: Persistencia
- [ ] T004 [DB] Configuraciones EF, índices únicos, `rowversion` y migración `AgregarDominioDisenos` (verificar que no toca tablas de Identity).
- [ ] T005 [BE] Pruebas con Testcontainers: aplica la migración, duplicados rechazados, concurrencia, ida y vuelta de un diseño completo (CE-003).
- [ ] T006 [BE] `IRepositorioDisenos` y su implementación.
## Fase 4: Aplicación y API
- [ ] T007 [P] [BE] Pruebas de manejadores `Crear`, `Editar`, `Obtener`, `Listar` (éxito, nombre duplicado 409, variante inválida 400, 412, aviso de términos).
- [ ] T008 [BE] Validadores y manejadores.
- [ ] T009 [BE] Pruebas y `DisenosAdminEndpoints` con política Administrador y `ETag`.
- [ ] T010 [MANUAL] Curar la lista de términos bloqueados de marcas/bandas en la configuración.
## Fase 5: Cierre
- [ ] T011 [BE] `build`/`test` en verde; backlog = Hecha; commit `HU-16: dominio de diseño, producto base y variante`.

**Trazabilidad**: RF-001→T002-3; RF-002→T004-5; RF-003→T002,T007; RF-004→T005,T009; RF-005→T001,T007-9; RF-006→T004 (sin columna de precio); RF-007→todas las de prueba.
