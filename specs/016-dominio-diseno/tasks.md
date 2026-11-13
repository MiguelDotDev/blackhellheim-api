# Tareas 016: Dominio de diseño

Etiquetas: [BE] [DB]. Pruebas primero.

## Fase 1
- [X] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla); confirmar HU-13 integrada.
## Fase 2: Dominio
- [X] T002 [P] [BE] Pruebas de `Diseno`: crear, nombre 3–80, slug, publicar sin arte/sin variantes (error), mismo producto base, quitar última variante publicada, archivar/restaurar.
- [X] T003 [BE] `Diseno`, `ProductoBase`, `Variante`, `ArchivoArte`, `EstadoDiseno`.
## Fase 3: Persistencia
- [X] T004 [DB] Configuraciones EF, índices únicos, `rowversion` y migración `AgregarDominioDisenos` (verificar que no toca tablas de Identity).
- [X] T005 [BE] Pruebas con Testcontainers: aplica la migración, duplicados rechazados, concurrencia, ida y vuelta de un diseño completo (CE-003).
- [X] T006 [BE] `IRepositorioDisenos` y su implementación.
## Fase 4: Aplicación y API
- [X] T007 [P] [BE] Pruebas de manejadores `Crear`, `Editar`, `Obtener`, `Listar` (éxito, nombre duplicado 409, variante inválida 400, 412, aviso de términos).
- [X] T008 [BE] Validadores y manejadores.
- [X] T009 [BE] Pruebas y `DisenosAdminEndpoints` con política Administrador y `ETag`.
- [X] T010 [MANUAL] Curar la lista de términos bloqueados de marcas/bandas en la configuración (`Disenos:TerminosBloqueados` en `appsettings.json`; propuesta en `docs/scrum/propuesta-terminos-bloqueados.md`).
## Fase 5: Cierre
- [X] T011 [BE] `build`/`test` en verde; backlog = Hecha; commit `HU-16: dominio de diseño, producto base y variante`.

**Trazabilidad**: RF-001→T002-3; RF-002→T004-5; RF-003→T002,T007; RF-004→T005,T009; RF-005→T001,T007-9; RF-006→T004 (sin columna de precio); RF-007→todas las de prueba.

**Notas de implementación**: la lista de términos bloqueados (T010 `[MANUAL]`) quedó curada en `Disenos:TerminosBloqueados` (T010; editable por variables de entorno `Disenos__TerminosBloqueados__N`); la migración `AgregarDominioDisenos` ya incluye `HashSha256` único en `ArchivosArte`. El contrato dejó de llevar `x-pendiente`, por lo que la prueba de contrato lo compara con la API publicada.

**Limitación conocida (T010)**: la comparación sigue siendo subcadena sin distinguir mayúsculas (`Contains` + `OrdinalIgnoreCase`). No normaliza acentos ni símbolos («Beyonce» ≠ «Beyoncé», «AC DC» ≠ «AC/DC») ni respeta límites de palabra («Fox» coincide con «Foxtrot»), así que hay falsos negativos y positivos; por eso se excluyeron los términos ambiguos salvo en frase. Mejora diferida: normalizar texto y término y buscar palabra completa. Solo genera un aviso, no bloquea.
