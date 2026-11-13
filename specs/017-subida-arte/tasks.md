# Tareas 017: Subida de arte

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero.

## Fase 1
- [x] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla); confirmar HU-13 y HU-16 integradas.
- [x] T002 [BE] Decidir y agregar dependencias. **Decisión (2026-11-11)**: la cabecera PNG (firma, IHDR, chunks previos a IDAT) se lee a mano sin librería; la spec exige medir la transparencia real de los píxeles, así que se decodifica con **SkiaSharp 4.156.0** (MIT, más `SkiaSharp.NativeAssets.Linux.NoDependencies`, MIT). Además `Azure.Storage.Blobs` 12.30.1 (MIT) y, solo en pruebas, `Testcontainers.Azurite` 4.16.0 (MIT). Licencias verificadas en el nuspec. `packages.lock.json` regenerados; `restore --locked-mode` y build Release en verde (imagen Docker no reconstruida).
- [ ] T003a [MANUAL] Confirmar la resolución mínima de arte (propuesta 4500×5400 px) con el proveedor de impresión.
## Fase 2: Validación
- [x] T003 [P] [BE] Pruebas de `ValidadorArte` (un caso por código de rechazo + caso exitoso + límites exactos + dimensiones máximas + APNG).
- [x] T004 [BE] `ValidadorArte` y opciones `Arte__*` validadas al arrancar.
## Fase 3: Almacenamiento
- [x] T005 [BE] Puerto `IAlmacenArchivos` y doble en memoria.
- [x] T006 [P] [BE] Pruebas de integración del adaptador con Azurite (guardar, existe, eliminar, falla → 503).
- [x] T007 [BE] `AlmacenBlobAzure` y registro en DI.
- [x] T008 [DB] Verificado: `ArchivosArte` de HU-16 ya trae `HashSha256` único; sin migración. Verificar que `ArchivosArte` de HU-16 ya trae `HashSha256` único; no se crea migración.
## Fase 4: Caso de uso y API
- [x] T009 [P] [BE] Pruebas de `SubirArte`: éxito, deduplicado, sin huérfanos si falla el registro, cancelación.
- [x] T010 [BE] Manejador y `ArteAdminEndpoints` (multipart, límite 200 MB por ruta, política Administrador).
- [x] T011 [BE] Pruebas de endpoint de todos los códigos; 201 devuelve URL pública.
- [x] T012 [MANUAL] Crear Storage Account y contenedor `arte` público; guardar cadena en secretos de Azure. _(cuenta bhhst229976c3 y contenedor arte creados en Azure el 13 nov 2026 por crear-recursos.sh; subida de arte aún sin probar en producción)_
## Fase 5: Cierre
- [x] T013 [BE] `build`/`test` en verde; backlog = Hecha; commit `HU-17: subida de arte a Azure Blob`.

**Trazabilidad**: RF-001→T005-7; RF-002→T003-4; RF-003→T001,T010-11; RF-004→T009; RF-005→T004; RF-006→T008-9; RF-007→T006,T012; CE-003→T003 (prueba con flujo de 201 MB generado sin memoria).
