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
