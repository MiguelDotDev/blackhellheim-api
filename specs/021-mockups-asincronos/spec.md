# Especificación 021: Mockups asíncronos de Printful

**Historia**: HU-21 (Sprint 6) · **Repos**: api · **Estado**: Spec lista · **Creada**: 2026-11-11

**Entrada**: «Quiero mockups asíncronos de Printful. Criterio: se pide la tarea, llega mockup_task_finished, hay polling de respaldo y las imágenes se copian a Blob.»

**Bloqueado por**: HU-15 (cliente Printful), HU-16 (diseños y variantes), HU-17 (`IAlmacenArchivos` y arte subido) y HU-22 (webhooks y despacho de eventos). La verificación contra Printful real es `[MANUAL]` (cuenta y token). Alimenta a HU-19/HU-23 (galería del detalle).

## Escenarios de uso y pruebas

### HU 1 — Pedir los mockups de un diseño (P1)
1. **Dado** un diseño con arte y variantes, **cuando** la administradora pide generar mockups, **entonces** se crea una tarea en Printful, se guarda como `Pendiente` y responde `202` con su id, sin esperar el resultado.
2. **Dado** una tarea pendiente del mismo diseño, **entonces** no se crea otra (`409`).
3. Diseño sin arte o sin variantes → `400`; diseño inexistente → `404`; sin rol Administrador → `403`.

### HU 2 — Recibir el resultado (P1)
1. **Dado** un evento `mockup_task_finished` verificado (HU-22), **entonces** la tarea pasa a `Completada`, las imágenes se descargan y se copian al almacén de archivos, y se registran por diseño y color.
2. El mismo evento repetido no duplica imágenes. Una tarea fallida en Printful queda `Fallida` con motivo.

### HU 3 — Polling de respaldo (P1)
1. **Dado** una tarea `Pendiente` por más de 2 min sin evento, **entonces** un proceso periódico la consulta a Printful y la completa igual que el evento; tras 30 min sin resultado queda `Fallida` (tiempo agotado).
2. Evento y polling simultáneos no duplican el trabajo.

### HU 4 — Consultar mockups (P2)
`GET /admin/disenos/{id}/mockups` lista tareas e imágenes (URL del almacén, color, posición).

### Casos límite
Descarga de imagen que falla (se reintenta; la tarea sigue pendiente hasta agotar intentos); imagen no PNG/JPEG o >20 MB (se descarta); URLs temporales de Printful que caducan; límite de peticiones de Printful (429, usa el reintento de HU-15); almacén caído (503, la tarea sigue pendiente).

## Requisitos funcionales
- **RF-001** `POST /admin/disenos/{id}/mockups` (202) y `GET /admin/disenos/{id}/mockups`.
- **RF-002** Extensión del puerto `IProveedorImpresion` para crear y consultar tareas de mockup.
- **RF-003** Manejador del evento `mockup_task_finished` registrado en el despacho de HU-22.
- **RF-004** Servicio en segundo plano de polling con intervalo y tiempo máximo configurables.
- **RF-005** Copia de imágenes con `IAlmacenArchivos`; nombres generados por el servidor.
- **RF-006** Finalización idempotente (evento y polling comparten el mismo caso de uso).
- **RF-007** Pruebas con dobles de Printful y almacén; nunca red real.

## Criterios de éxito
- **CE-001** El 100 % de las tareas terminan en `Completada` o `Fallida` (ninguna queda pendiente más de 30 min).
- **CE-002** 10 finalizaciones concurrentes de la misma tarea dejan un solo conjunto de imágenes.

## Supuestos y fuera de alcance
Rutas, campos y variantes de mockup de Printful v2 se confirman en T002 (`[MANUAL]` con token). Mostrar la galería en la web (HU-23) y agregar las imágenes al catálogo público se hacen al integrarlas; esta HU solo las deja disponibles.
