# AGENTS.md · blackhellheim-api

Reglas para agentes de código (OpenCode, Claude Code y otros). La fuente de verdad de los principios es `.specify/memory/constitution.md`; léela primero.

## Qué es
API .NET 10 de la tienda BlackHellHeim (playeras Print-On-Demand, México, MXN). Marca siempre escrita **BlackHellHeim**.

## Estructura
- `src/Dominio`, `src/Aplicacion`, `src/Infraestructura`, `src/Api`, `src/Pruebas` (Clean Architecture; dependencias hacia adentro).
- `specs/NNN-slug/` una carpeta por historia: `spec.md`, `plan.md`, `tasks.md`, `modelo-datos.md`, `contratos/openapi.yaml`.
- `docs/scrum/`, `docs/legal/`, `docs/fiscal/`.

## Reglas
1. No implementes nada sin spec y tasks aprobadas en `specs/NNN-slug/`.
2. Actualiza el OpenAPI antes del código del endpoint.
3. Pruebas unitarias para cada regla (éxito, error, validación).
4. Los controladores devuelven DTOs, nunca entidades. Errores con ProblemDetails.
5. El precio siempre lo calcula el backend.
6. Proveedores externos solo vía puertos (`IProveedorImpresion`, `IPasarelaPago`, `IAlmacenArchivos`, `IProveedorTipoCambio`, `IEnvioNotificaciones`).
7. Cada cambio de esquema incluye migración de EF Core.
8. No agregues dependencias con licencia comercial (MediatR 13+, AutoMapper, FluentAssertions). Verifica licencia de lo nuevo.
9. Nunca escribas secretos ni los subas; el repositorio es público.
10. Código, specs y commits en español.

## Comandos
```bash
dotnet build
dotnet test
dotnet ef migrations add <Nombre> -p src/Infraestructura -s src/Api
docker compose up -d   # SQL Server y Azurite locales
```

## Flujo SDD
`/speckit.specify` → `/speckit.clarify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.analyze` → `/speckit.implement`. Las tareas llevan etiqueta [BE] [FE] [DB] [CI] y se importan a Jira (proyecto BHH) como subtareas.

## Flujo por historia de usuario y ramas

**Ramas**: `main` (despliega a Azure, solo recibe de `release`), `release` (pruebas y aprobación), `developer` (integración) y `sprint/NN` (una por sprint, sale de `developer`). Nunca hagas commit directo a `main`, `release` ni `developer`.

**Trabajo por historia (requisito académico)**:

1. La fuente de verdad de qué HU pertenece a qué sprint es `docs/scrum/backlog.md`. Antes de empezar, identifica la HU y su sprint, y trabaja sobre la rama `sprint/NN` de ese sprint (si no existe, dime y no la crees tú). No uses ramas por historia.
2. Cada HU corresponde a una carpeta en `specs/`. Implementa una HU a la vez; no mezcles HU distintas en el mismo commit.
3. Cuando terminen todas las tareas de una HU y sus pruebas pasen, haz commit con el mensaje `"HU-N: <título corto>"` y actualiza su fila en `backlog.md` (Spec y Estado = Hecha). Después DETENTE y dime textualmente: `"HU-N terminada. Haz push a sprint/NN"` con el comando exacto. No hagas push tú.
4. Cuando todas las HU de un sprint estén en Hecha, dime textualmente: `"Sprint N completo. Abre pull request de sprint/NN a developer"`. Luego developer a release y release a main solo con mi aprobación explícita.
5. Si una HU toca ambos repos, trabaja la parte de este repo, márcalo en la fila y avísame qué falta en el otro.
6. Si el backlog y las carpetas de specs no coinciden, avísame en vez de suponer.
