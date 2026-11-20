# Validación 021

Prerrequisitos: `dotnet`, Docker (SQL Server de pruebas), HU-15/16/17/22 integradas.

1. `dotnet test --filter "FullyQualifiedName~Mockups"`: casos de uso, polling, endpoints y concurrencia con dobles (sin red).
2. Contrato: `dotnet test --filter "FullyQualifiedName~Contrato"` (el documento publicado coincide con [contratos/openapi.yaml](contratos/openapi.yaml)).
3. `docker compose up -d` (requiere `.env` local, no versionado) y, como administradora, `POST /admin/disenos/{id}/mockups` → `202` con la tarea `Pendiente`; repetirlo → `409`; `GET` la lista.
4. `[MANUAL]` con token real de Printful y URL pública del arte: la tarea llega a `Completada` por evento o por polling, y las imágenes quedan en Blob.
