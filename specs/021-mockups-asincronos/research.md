# Investigación 021 (Fase 0)

Sin llamadas reales: todo lo de Printful es **suposición a verificar** (`[MANUAL]`, T003).

| Tema | Decisión | Motivo / alternativas |
|---|---|---|
| Crear tarea | `POST /v2/mockup-tasks` con producto de catálogo, ids de variante, una colocación (`Mockups__Posicion`, por omisión `front`) y la técnica `Mockups__Tecnica` (por omisión `dtg`) y la URL pública del arte | API v2 que ya usa HU-15 para el catálogo. Alternativa v1 (`/mockup-generator`) descartada por ser el flujo antiguo. |
| Consultar tarea | `GET /v2/mockup-tasks?id={id}`; estados `pending`/`completed`/`failed` | Es el respaldo del evento. |
| Resultado | Lista de mockups por variante con URL temporal de imagen | Las URL caducan: se copian de inmediato a Blob. |
| Evento | `mockup_task_finished`; el id de tarea se toma del `data` | Formato real sin confirmar; el polling cubre la diferencia. Relacionado con el riesgo de `id` de evento en HU-22. |
| Descarga | Por puerto (`IProveedorImpresion.DescargarImagenAsync`), https y lista de hosts, 20 MB, firma binaria PNG/JPEG | Principio VII y anti-SSRF; la cabecera `Content-Type` no es confiable. |
| Concurrencia | Blobs con nombre determinista + transacción de inserción y cambio condicional de estado | Evita estados intermedios y duplicados sin bloqueos. Alternativa: estado `Procesando` con arrendamiento (más estados y recuperación de caídas). |
| Reintentos de descarga | Los hace el siguiente ciclo de polling | Evita un contador y una cola; el límite es el tiempo máximo. |
| Dependencias | Ninguna nueva | Principio IV. |
