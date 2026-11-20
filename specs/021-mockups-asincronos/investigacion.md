# Investigación 021 (T002): supuestos de Printful v2 fijados en el código

**Ninguno está verificado contra Printful real** (sin llamadas externas en esta sesión). El razonamiento está en [research.md](research.md); la verificación es `[MANUAL]` (T003).

| Supuesto | Dónde está en el código |
|---|---|
| `POST /v2/mockup-tasks` con `{format, products:[{source, catalog_product_id, catalog_variant_ids, placements:[{placement, technique, layers:[{type:"file", url}]}]}]}`; respuesta `data[0].id` | `ProveedorImpresionPrintful.CrearTareaMockupAsync` |
| `GET /v2/mockup-tasks?id={id}`; `data[0].status` = `pending`/`completed`/`failed`; `failure_reasons`; `catalog_variant_mockups[].{catalog_variant_id, mockups[].{placement, mockup_url}}` | `ConsultarTareaMockupAsync` |
| Host de las imágenes: `files.cdn.printful.com` (https), configurable en `Mockups__HostsDescargaPermitidos` | `OpcionesMockups`, `DescargarImagenAsync` |
| El evento `mockup_task_finished` trae el id de tarea en `data.mockup_task.id`, `data.task.id` o `data.id`; si no coincide, el polling completa la tarea | `ManejadorMockupTaskFinished.ExtraerIdTarea` |
| Riesgo heredado de HU-22: si el evento no trae `id` propio, HU-22 lo rechaza con 400 (ver `specs/022-webhooks-printful/investigacion.md`) | — |
| Printful debe poder leer la URL del arte: en local con Azurite no es pública | `PedirMockups` usa `ArchivoArte.Url` |
