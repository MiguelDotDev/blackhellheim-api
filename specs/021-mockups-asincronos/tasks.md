# Tareas 021: Mockups asíncronos

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero (constitución III). Formato: `- [ ] Tnnn [P] [USn] [etiqueta] descripción con ruta`.

## Fase 1: Preparación
- [x] T001 [BE] Escribir la prueba de contrato de `contratos/openapi.yaml` en `src/Pruebas/Api` (falla hasta tener los endpoints); al terminar la fase 6, quitar `x-pendiente` del contrato.
- [x] T002 [BE] Crear `specs/021-mockups-asincronos/investigacion.md` solo con la lista de supuestos de Printful v2 que el código fija (rutas, campos, forma del evento `mockup_task_finished`, host de descarga `files.cdn.printful.com`) y que NO se verificaron; el razonamiento está en `research.md`; sin dependencias nuevas.
- [ ] T003 [MANUAL] Verificar con token real de Printful y URL pública del arte un mockup de prueba y la forma real del evento (cuenta y secretos de la titular; no versionar valores).

## Fase 2: Base común (bloquea las historias)
- [x] T004 [DB] Dominio `src/Dominio/Mockups`: `TareaMockup` (Id, DisenoId, PrintfulTaskId varchar(60), Estado `Pendiente`/`Completada`/`Fallida` varchar(12), Motivo varchar(300) nullable, CreadaUtc, FinalizadaUtc nullable) e `ImagenMockup` (Id, TareaId, Color varchar(60), Posicion varchar(40), Url varchar(500), NombreBlob varchar(200)).
- [x] T005 [DB] Configuración EF en `src/Infraestructura/Persistencia/Configuraciones` y migración `AgregarMockups`: FK a Disenos; único (PrintfulTaskId); índice único filtrado sobre DisenoId donde Estado = 'Pendiente'; índice (Estado, CreadaUtc); `ImagenesMockup` con FK a TareasMockup en cascada y único (TareaId, Color, Posicion); actualizar la prueba que cuenta las migraciones.
- [x] T006 [BE] Extender el puerto `IProveedorImpresion` (`src/Aplicacion/Impresion`) con `CrearTareaMockupAsync(SolicitudMockup)`, `ConsultarTareaMockupAsync(id)` y `DescargarImagenAsync(Uri, maxBytes)` y los tipos propios `SolicitudMockup`, `ResultadoTareaMockup`, `MockupGenerado`; actualizar el adaptador `src/Infraestructura/Printful` (descarga solo https y hosts de `Mockups__HostsDescargaPermitidos`, por omisión `files.cdn.printful.com`; con pruebas: https obligatorio, host fuera de la lista rechazado) y los dobles de prueba existentes.
- [x] T007 [BE] `OpcionesMockups` (`Mockups__IntervaloPollingSegundos`=30, `EsperaMinimaSegundos`=120, `TiempoMaximoMinutos`=30, `HostsDescargaPermitidos` (por omisión `files.cdn.printful.com`), `MaxBytesImagen`=20 MB, `Posicion`=front, `Tecnica`=dtg de `ValoresImpresion.Tecnicas`) validadas al arrancar, y `Api/Comun/RegistroMockups.cs`; puerto `IRepositorioMockups` en `src/Aplicacion/Mockups` con un método atómico `CompletarAsync(tareaId, imagenes)` (inserta imágenes y cambia `Pendiente`→`Completada` en una transacción; devuelve false si otra finalización ganó).

## Fase 3: US1 pedir mockups (P1)
Criterio independiente: con el doble de Printful, `POST` responde 202 y deja la tarea `Pendiente`.
- [x] T008 [P] [BE] [US1] Pruebas de `PedirMockups` y del endpoint POST: 202, duplicado 409, sin arte o sin variantes activas 400, diseño inexistente 404, sin sesión 401, sin rol Administrador 403, Printful caído 502/503/504 (`src/Pruebas/Mockups`).
- [x] T009 [BE] [US1] Caso de uso `PedirMockups` y repositorio (captura de la violación del índice filtrado → 409) en `src/Aplicacion/Mockups` y `src/Infraestructura/Mockups`.
- [x] T010 [BE] [US1] `src/Api/Endpoints/MockupsAdminEndpoints.cs` con `POST /admin/disenos/{id}/mockups` (solo Administrador, DTO `TareaMockup`, ProblemDetails).

## Fase 4: US2 recibir el resultado (P1)
Criterio independiente: finalizar una tarea con el doble deja `Completada` y sus imágenes en el almacén doble, sin duplicar al repetir.
- [x] T011 [P] [BE] [US2] Pruebas de `FinalizarTareaMockup`: éxito, repetido sin duplicar, `failed` de Printful → `Fallida` con motivo truncado a 300, descarga fallida (sigue `Pendiente`), imagen no PNG/JPEG por firma binaria o mayor a 20 MB (se descarta), almacén caído (sigue `Pendiente`), una imagen por (color, posición), mockup de variante inexistente en la base local descartado.
- [x] T012 [BE] [US2] `FinalizarTareaMockup`: si la tarea no está `Pendiente` termina; descarga por el puerto, sube con `IAlmacenArchivos` con nombre determinista `mockups/{tareaId}/{colorSlug}-{posicion}.{ext}` (color tomado de la `Variante` local) y, en una transacción, inserta las imágenes y cambia `Pendiente`→`Completada` con actualización condicional.
- [x] T013 [P] [BE] [US2] Pruebas de `ManejadorMockupTaskFinished` (extrae el id de tarea, tarea desconocida se ignora con 200, evento repetido no duplica) y de concurrencia: 10 finalizaciones simultáneas con SQL Server real dejan un solo conjunto de imágenes.
- [x] T014 [BE] [US2] `ManejadorMockupTaskFinished : IManejadorEventoProveedor` (proveedor `printful`, tipo `mockup_task_finished`) y su registro en `RegistroMockups`.

## Fase 5: US3 polling de respaldo (P1)
Criterio independiente: con reloj falso, una tarea pendiente se completa o se agota sin evento.
- [x] T015 [P] [BE] [US3] Pruebas del polling con `TimeProvider` falso: respeta la espera mínima de 2 min, completa, marca `Fallida` por tiempo agotado tras 30 min, no pisa tareas ya finalizadas, evento y polling simultáneos no duplican, y una tarea sin evento (HU-22 lo rechazó por no traer `id`) se completa solo con polling.
- [x] T016 [BE] [US3] `ServicioPollingMockups : BackgroundService` en `src/Api/Comun/RegistroMockups.cs` (necesita `ISender`, que Infraestructura no referencia) (un ámbito por ciclo, usa `FinalizarTareaMockup`) y su registro.

## Fase 6: US4 consulta y cierre (P2)
Criterio independiente: `GET` lista tareas e imágenes.
- [x] T017 [P] [BE] [US4] Pruebas del GET `/admin/disenos/{id}/mockups`: 200 con tareas e imágenes (más recientes primero), 404, 401, 403.
- [x] T018 [BE] [US4] Consulta y endpoint GET en `MockupsAdminEndpoints`; quitar `x-pendiente` del contrato y verificar que el documento publicado coincide.

## Fase 7: Cierre
- [x] T019 [BE] Añadir las variables `Mockups__*` al `compose.yaml` (sin secretos), `dotnet build`/`dotnet test` en verde, tareas marcadas, fila de `docs/scrum/backlog.md` = Hecha y commit `HU-21: mockups asíncronos`.

## Dependencias y paralelismo
Fase 2 bloquea el resto. US1, US2 y US3 pueden avanzar tras la fase 2, pero US3 usa `FinalizarTareaMockup` (T012) y US2 necesita tareas creadas por US1 solo en pruebas (se siembran). Marcadas `[P]` las pruebas de cada fase. MVP = fases 1–4 (pedir y recibir por evento); US3 garantiza CE-001.

**Trazabilidad**: RF-001→T008–T010,T017–T018; RF-002→T006; RF-003→T013–T014; RF-004→T007,T015–T016; RF-005→T011–T012; RF-006→T011–T013; RF-007→todas las pruebas; CE-001→T015; CE-002→T013.
