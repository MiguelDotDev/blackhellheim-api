# Investigación 021 (T002): supuestos de Printful v2 fijados en el código

**Ninguno está verificado contra Printful real** (sin llamadas externas en esta sesión). El razonamiento está en [research.md](research.md); la verificación es `[MANUAL]` (T003).

| Supuesto | Dónde está en el código |
|---|---|
| `POST /v2/mockup-tasks` con `{format, products:[{source, catalog_product_id, catalog_variant_ids, placements:[{placement, technique, layers:[{type:"file", url}]}]}]}`; respuesta `data[0].id` | `ProveedorImpresionPrintful.CrearTareaMockupAsync` |
| `GET /v2/mockup-tasks?id={id}`; `data[0].status` = `pending`/`completed`/`failed`; `failure_reasons`; `catalog_variant_mockups[].{catalog_variant_id, mockups[].{placement, mockup_url}}` | `ConsultarTareaMockupAsync` |
| Host de las imágenes: `files.cdn.printful.com` y `printful-upload.s3-accelerate.amazonaws.com` (https; ver hallazgo del 2026-11-27), configurable en `Mockups__HostsDescargaPermitidos` | `OpcionesMockups`, `DescargarImagenAsync` |
| El evento `mockup_task_finished` trae el id de tarea en `data.mockup_task.id`, `data.task.id` o `data.id`; si no coincide, el polling completa la tarea | `ManejadorMockupTaskFinished.ExtraerIdTarea` |
| Riesgo heredado de HU-22: si el evento no trae `id` propio, HU-22 lo rechaza con 400 (ver `specs/022-webhooks-printful/investigacion.md`) | — |
| Printful debe poder leer la URL del arte: en local con Azurite no es pública | `PedirMockups` usa `ArchivoArte.Url` |

## Hallazgo verificado contra Printful real (2026-11-20): falta `X-PF-Store-Id`
`POST /v2/mockup-tasks` respondía 400 «This endpoint requires `store_id`!»: el token es de cuenta y el cliente no enviaba `X-PF-Store-Id`. Con la cabecera Printful ya no objeta la tienda. Corrección: opción `Printful__IdTienda` (no es secreto; vacía = no se envía) y cabecera en crear y consultar tarea (`ObtenerAsync(..., deTienda: true)`). Los endpoints de catálogo (`/v2/catalog-*`) no la necesitan. Los 4xx/5xx de esas dos llamadas se registran en Warning con código y primeros 500 caracteres del cuerpo.

## Hallazgo (2026-11-27): host de `mockup_url`
Con Printful real la tarea cerraba `Fallida` con «Printful no devolvió imágenes válidas.». La documentación oficial v2 (`developers.printful.com/docs/v2-beta`, `GET /v2/mockup-tasks`) confirma los nombres de campo que usa el código y muestra `mockup_url` en `printful-upload.s3-accelerate.amazonaws.com`, ausente de la lista permitida. La URL es temporal (se borra tras un día). Corrección: el host se admite por omisión; `failure_reasons[{type,detail}]` queda en `Motivo` y en un Warning con conteos y hosts rechazados. **Sin confirmar** con la respuesta de una tarea real (faltó su id).
