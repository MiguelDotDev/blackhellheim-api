# Tareas 017: Subida de arte

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero.

## Fase 1
- [ ] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla); confirmar HU-13 y HU-16 integradas.
- [ ] T002 [BE] Decidir y agregar dependencias (`Azure.Storage.Blobs`, decodificador); verificar y anotar licencia de cada una (no ImageSharp); regenerar `packages.lock.json`; comprobar que el `Dockerfile` sigue compilando.
## Fase 2: Validación
- [ ] T003 [P] [BE] Pruebas de `ValidadorArte` (un caso por código de rechazo + caso exitoso + límites exactos + dimensiones máximas + APNG).
- [ ] T004 [BE] `ValidadorArte` y opciones `Arte__*` validadas al arrancar.
## Fase 3: Almacenamiento
- [ ] T005 [BE] Puerto `IAlmacenArchivos` y doble en memoria.
- [ ] T006 [P] [BE] Pruebas de integración del adaptador con Azurite (guardar, existe, eliminar, falla → 503).
- [ ] T007 [BE] `AlmacenBlobAzure` y registro en DI.
- [ ] T008 [DB] Migración `AgregarHashArte` solo si hace falta (índice único de hash).
## Fase 4: Caso de uso y API
- [ ] T009 [P] [BE] Pruebas de `SubirArte`: éxito, deduplicado, sin huérfanos si falla el registro, cancelación.
- [ ] T010 [BE] Manejador y `ArteAdminEndpoints` (multipart, límite 200 MB por ruta, política Administrador).
- [ ] T011 [BE] Pruebas de endpoint de todos los códigos; 201 devuelve URL pública.
- [ ] T012 [MANUAL] Crear Storage Account y contenedor `arte` público; guardar cadena en secretos de Azure.
## Fase 5: Cierre
- [ ] T013 [BE] `build`/`test` en verde; backlog = Hecha; commit `HU-17: subida de arte a Azure Blob`.

**Trazabilidad**: RF-001→T005-7; RF-002→T003-4; RF-003→T001,T010-11; RF-004→T009; RF-005→T004; RF-006→T008-9; RF-007→T006,T012; CE-003→T003 (prueba con flujo de 201 MB generado sin memoria).
