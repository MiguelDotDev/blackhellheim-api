# Especificación 000: Fundación y DevOps (API)

**Épica**: E0 Fundación y DevOps · **Sprints**: 1 y 2 · **Jira**: BHH (componentes BE, DB, CI)
**Estado**: borrador semilla. Completar con `/speckit.specify` y `/speckit.clarify`.

## Objetivo
Dejar el repositorio listo para que cada historia posterior se construya sobre una base probada y desplegable.

## Historias de usuario
1. Como desarrolladora, quiero una solución .NET 10 con las capas Dominio, Aplicación, Infraestructura, Api y Pruebas para respetar la regla de dependencias.
2. Como desarrolladora, quiero CI en GitHub Actions (build, pruebas, SonarQube Cloud y CodeQL) para que ningún cambio entre sin pasar controles.
3. Como desarrolladora, quiero SQL Server y Azurite con Docker Compose y migraciones de EF Core para trabajar en local.
4. Como desarrolladora, quiero OpenAPI con Scalar, ProblemDetails y pipeline de Mediator para tener un contrato visible y errores uniformes.
5. Como frontend, quiero el OpenAPI publicado para generar el cliente y levantar un mock.
6. Como administradora, quiero despliegue en Azure Container Apps (API con ingress interno) desde GitHub Actions con imágenes en GHCR.

El detalle y los números de historia están en el documento "BlackHellHeim: backlog Scrum y estructura SDD".

## Criterios de aceptación
- `dotnet build` y `dotnet test` pasan en CI en cada pull request.
- Una prueba de arquitectura falla si Dominio referencia otra capa.
- `docker compose up` deja la API funcionando con base de datos migrada.
- `/scalar` muestra el contrato y los errores salen como ProblemDetails.
- El despliegue a Azure se ejecuta desde GitHub Actions sin secretos en el repositorio.

## Fuera de alcance
Autenticación, catálogo, pagos (ver specs posteriores).

## Puntos abiertos
- Forma exacta de la dirección por defecto de Container Apps y comportamiento del ingress interno.
- Límites de la oferta gratuita de Azure SQL.
