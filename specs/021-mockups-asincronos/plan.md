# Plan 021: Mockups asíncronos

**Spec**: [spec.md](spec.md) · **Investigación**: [research.md](research.md) · **Modelo**: [modelo-datos.md](modelo-datos.md) · **Contrato**: [contratos/openapi.yaml](contratos/openapi.yaml) · **Validación**: [quickstart.md](quickstart.md)

## Contexto técnico
- .NET 10, EF Core 10 (SQL Server), Mediator (MIT) con el pipeline existente, xUnit; patrón de HU-15/16/17/22.
- **Dependencias nuevas: ninguna** (`HttpClient`, `BackgroundService`, `System.Text.Json` de la plataforma; SkiaSharp ya está por HU-17, MIT). Principio IV cumplido.
- Almacenamiento: `IAlmacenArchivos` (HU-17). Eventos: `IManejadorEventoProveedor` (HU-22, ya integrada). Proveedor: `IProveedorImpresion` (HU-15).
- Sin red real en pruebas: doble de `IProveedorImpresion`, `IAlmacenArchivos` en memoria, `TimeProvider` falso, SQL Server de Testcontainers para concurrencia.

## Revisión de la constitución
| Principio | Resultado |
|---|---|
| I Arquitectura limpia | Cumple: entidades en Dominio, casos de uso y puertos en Aplicación, repositorio/polling/adaptador en Infraestructura, DTO en la API. |
| II Contrato primero | Cumple: `contratos/openapi.yaml` completo (esquemas y errores) antes del código; T001 quita `x-pendiente`. |
| III Pruebas | Cumple: cada regla con éxito/error/validación (ver Pruebas). |
| IV Software libre | Cumple: sin dependencias nuevas. |
| V Precio en backend | No aplica (los mockups no llevan precio). |
| VI Webhooks idempotentes | Cumple: la verificación de firma e idempotencia del evento son de HU-22; la finalización es además idempotente por actualización condicional. |
| VII Puertos | Cumple (RF-002 y RF-008): la descarga de imágenes de Printful también pasa por un puerto: se agrega `DescargarImagenAsync` a `IProveedorImpresion` (no `HttpClient` directo en el caso de uso). |
| VIII Seguridad | Endpoints solo Administrador; URL de descarga restringida a https y a una lista de hosts configurable (anti-SSRF); sin secretos nuevos. |
| IX, X, XI | Cumple / no aplica / español. |

## Decisiones
1. **Entidades** `TareaMockup` (Pendiente/Completada/Fallida) e `ImagenMockup`; migración `AgregarMockups`. Índice único filtrado: una `Pendiente` por diseño.
2. **Puerto** `IProveedorImpresion` gana `CrearTareaMockupAsync(SolicitudMockup)`, `ConsultarTareaMockupAsync(id)` y `DescargarImagenAsync(Uri, maxBytes)`; tipos propios del puerto (`ResultadoTareaMockup`, `MockupGenerado`). El doble y el adaptador Printful los implementan. Rutas/campos de Printful v2: supuestos en `research.md`, verificables solo con token (`[MANUAL]`).
3. **Pedir** (`PedirMockups`): valida diseño (existe, tiene arte, ≥1 variante activa), consulta que no haya `Pendiente` (409), crea la tarea en Printful y guarda la fila. Si la inserción choca con el índice filtrado (carrera) responde 409 y la tarea huérfana de Printful se ignora (inofensiva). Si Printful falla: 502/503/504 por el mapeo existente de HU-15.
4. **Finalizar** (`FinalizarTareaMockup`, caso de uso único para evento y polling): (a) lee la tarea; si no está `Pendiente` termina sin hacer nada; (b) descarga cada imagen por el puerto (límites: 20 MB, PNG/JPEG por firma binaria, no por cabecera) y la sube con `IAlmacenArchivos` con **nombre determinista** `mockups/{tareaId}/{colorSlug}-{posicion}.{ext}` (el color sale de la `Variante` local, nunca de texto de Printful); (c) en **una transacción** inserta las imágenes y pasa la tarea `Pendiente`→`Completada` con actualización condicional; el perdedor de una carrera no inserta y los blobs idénticos solo se reescriben. Esto da CE-002 y evita `Completada` sin imágenes si el proceso cae a mitad.
5. **Una imagen por (color, posición)**: Printful devuelve un mockup por variante (talla); se conserva el primero por color.
6. **Fallas**: descarga/almacén caído → la tarea sigue `Pendiente` y el siguiente ciclo de polling reintenta (no hay contador aparte); Printful reporta `failed` → `Fallida` con motivo truncado a 300; tras `TiempoMaximoMinutos` pendiente → `Fallida` «tiempo agotado» (CE-001).
7. **Evento**: `ManejadorMockupTaskFinished : IManejadorEventoProveedor` (proveedor `printful`, tipo `mockup_task_finished`) extrae el id de tarea del `data` del evento y llama a `FinalizarTareaMockup`; si no encuentra la tarea local, la ignora (200). El formato real del payload está sin verificar (`[MANUAL]`); el polling garantiza el resultado aunque el evento no coincida.
8. **Polling**: `ServicioPollingMockups : BackgroundService` con un ámbito por ciclo; consulta tareas `Pendiente` con antigüedad ≥ `EsperaMinimaSegundos`. Seguro con varias instancias gracias a la actualización condicional. Opciones `Mockups__*` validadas al arrancar: `IntervaloPollingSegundos` (30), `EsperaMinimaSegundos` (120), `TiempoMaximoMinutos` (30), `HostsDescargaPermitidos` (por omisión `files.cdn.printful.com` y `printful-upload.s3-accelerate.amazonaws.com`), `MaxBytesImagen` (20 MB), `Posicion` (`front`), `Tecnica` (`dtg`, de `ValoresImpresion.Tecnicas`).
9. **Consulta**: `GET` devuelve tareas (más recientes primero) con sus imágenes; sin paginación (pocas tareas por diseño).

## Estructura
`Dominio/Mockups`, `Aplicacion/Mockups` (casos de uso, manejador de evento, `IRepositorioMockups`, opciones), `Infraestructura/Mockups` (repositorio), `Api/Comun` (registro y polling) y `Infraestructura/Printful` (adaptador), `Api/Endpoints/MockupsAdminEndpoints.cs`, `Api/Comun/RegistroMockups.cs`.

## Pruebas
Casos de uso con dobles (éxito, repetido, falla de descarga, tipo/tamaño inválido, almacén caído, Printful `failed`, tiempo agotado); polling con reloj falso (espera mínima, completa, agotado, no pisa lo finalizado); endpoints (202/400/401/403/404/409/502); SQL Server real (10 finalizaciones concurrentes → un solo conjunto de imágenes; índice filtrado); contrato.

## Bloqueos y [MANUAL]
Bloqueado por HU-15, 16, 17 y 22 (todas integradas). `[MANUAL]`: token de Printful, verificación de un mockup real y de la forma del evento; la URL del arte debe ser pública para que Printful la lea (en local con Azurite no lo es; HU-25/HU-40 para producción).
