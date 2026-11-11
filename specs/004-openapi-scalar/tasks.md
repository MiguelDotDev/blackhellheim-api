# Tareas: Contrato OpenAPI publicado y documentación interactiva

**Entrada**: `specs/004-openapi-scalar/` (spec.md, plan.md, investigacion.md, modelo-datos.md, contratos/openapi.yaml, inicio-rapido.md)

**Prerrequisitos**: plan.md y spec.md. **Las historias 002, 002b y 003 deben estar implementadas** (aportan `/salud`, `/tiendas/{codigo}`, `ErrorUniforme`, `IContextoEjecucion`, correlación y `/salud/base`). Hoy `src/Api/Program.cs` solo expone `/salud`.

**Pruebas**: obligatorias y primero (constitución III). Cada prueba se escribe y se ve fallar antes de implementar. Nada se difiere.

**Formato**: `- [ ] T### [P?] [US?] [ETIQUETA] Descripción con ruta`. Etiquetas: `[BE]` código/pruebas del API, `[DB]` base de datos, `[CI]` automatización, `[MANUAL]` solo la desarrolladora. `[P]` = paralelizable.

## Fase 1: Preparación

- [ ] T001 [BE] Verificar prerrequisitos: existen `/tiendas/{codigo}` y `/salud/base` en `src/Api/`, el tipo `ErrorUniforme` y `IContextoEjecucion` (002b), y `ContextoBlackHellHeim` (003); si falta alguno, DETENER y avisar (no reimplementar otras historias).
- [ ] T002 [BE] Agregar `Microsoft.AspNetCore.OpenApi` 10.0.15 (MIT) y `Scalar.AspNetCore` 2.20.0 (MIT) a `src/Api/BlackHellHeim.Api.csproj`, y verificar de nuevo ambas licencias en `specs/004-openapi-scalar/investigacion.md`.
- [ ] T003 [P] [BE] Agregar `YamlDotNet` 18.1.0 (MIT) a `src/Pruebas/BlackHellHeim.Pruebas.csproj` y confirmar su licencia en `investigacion.md`.
- [ ] T004 [BE] Ejecutar `dotnet build BlackHellHeim.sln` y confirmar compilación sin advertencias nuevas con las dependencias agregadas.

## Fase 2: Contratos primero (bloquea las historias)

**Meta**: alinear los contratos antes de tocar código (AGENTS.md regla 2).

- [ ] T005 [P] [BE] Actualizar `specs/002-estructura-base-api/contratos/openapi.yaml`: `openapi: 3.1.1`; `/salud` con `$ref: EstadoSalud` y esquema `EstadoSalud` (`estado` requerido, tipo cadena, `examples: [ok]`, sin `const`); respuesta `500` con `ProblemDetails`; copia canónica de `ProblemDetails`.
- [ ] T006 [P] [BE] Actualizar `specs/002b-casos-de-uso-mediator/contratos/openapi.yaml`: `openapi: 3.1.1`, eliminar `const` de esquemas de respuesta (usar `examples`) y dejar `ProblemDetails` idéntico al canónico de `specs/004-openapi-scalar/contratos/openapi.yaml`.
- [ ] T007 [P] [BE] Actualizar `specs/003-sql-server-efcore/contratos/openapi.yaml`: `openapi: 3.1.1`, `ProblemDetails` con `errores` (idéntico al canónico), respuesta `500` en `/salud/base`, sin `const`.
- [ ] T008 [P] [BE] Revisar `specs/004-openapi-scalar/contratos/openapi.yaml`: `paths: {}`, esquema canónico `ProblemDetails` (requeridos `type`, `title`, `status`, `codigo`, `idCorrelacion`; `errores` objeto con arreglos de cadenas) y extensiones `x-documento-openapi` y `x-documentacion-interactiva`.

## Fase 3: Historia 1 — Contrato OpenAPI publicado y fiel (P1)

**Meta**: `GET /openapi/v1.json` publica un documento válido con todas las operaciones.

**Prueba independiente**: `curl http://localhost:5068/openapi/v1.json` devuelve OpenAPI 3.1 con título **BlackHellHeim API**, `/salud`, `/tiendas/{codigo}` y `/salud/base`, y cada operación con `500`.

### Pruebas primero

- [ ] T009 [P] [US1] [BE] Escribir `src/Pruebas/Api/OpenApiPublicadoPruebas.cs`: 200 `application/json`; `openapi` empieza con `3.1`; título `BlackHellHeim API`; contiene `/salud`, `/tiendas/{codigo}`, `/salud/base`; sin `/openapi` ni `/scalar`; cada operación tiene `500` `application/problem+json` con `$ref` a `ProblemDetails`. Debe fallar.
- [ ] T010 [P] [US1] [BE] Agregar a `OpenApiPublicadoPruebas.cs` la prueba de validez: el documento se parsea con `Microsoft.OpenApi` sin errores de lectura y sin referencias rotas (RF-002, CE-002).
- [ ] T011 [P] [US1] [BE] Agregar a `OpenApiPublicadoPruebas.cs` que `/salud` responde igual que antes y aparece documentado (RF-016, CE-008).

### Implementación

- [ ] T012 [US1] [BE] Crear `src/Api/OpenApi/TransformadorDocumentoBlackHellHeim.cs` (document transformer): `info.title = "BlackHellHeim API"`, versión de la API, esquema canónico de `ProblemDetails` y respuesta `500` en todas las operaciones.
- [ ] T013 [US1] [BE] Crear `src/Api/Endpoints/SaludEndpoints.cs` moviendo `/salud` desde `Program.cs` con un tipo `EstadoSalud` y metadatos (`WithName`, `Produces<EstadoSalud>`, `ProducesProblem(500)`), sin cambiar el contrato.
- [ ] T014 [US1] [BE] Registrar en `src/Api/Program.cs` `AddOpenApi("v1", ...)` con el transformador y `MapOpenApi("/openapi/{documentName}.json")` sin autenticación; verificar que `MapOpenApi` y Scalar no aparecen en el documento.
- [ ] T015 [US1] [BE] Ejecutar `dotnet test --filter OpenApiPublicadoPruebas` y confirmar T009, T010 y T011 en verde.

**Punto de control**: US1 funciona sola.

## Fase 4: Historia 2 — Página de documentación interactiva (P2)

**Meta**: `/scalar` muestra el contrato y las operaciones.

**Prueba independiente**: abrir `http://localhost:5068/scalar` muestra **BlackHellHeim API** y las operaciones en menos de 3 s.

### Pruebas primero

- [ ] T016 [P] [US2] [BE] Escribir `src/Pruebas/Api/DocumentacionScalarPruebas.cs`: `GET /scalar` redirige a `/scalar/`; `GET /scalar/` responde 200 `text/html`, menciona `BlackHellHeim API`, referencia `/openapi/v1.json` y no carga recursos de un CDN externo; responde en < 3 s. Debe fallar.
- [ ] T017 [P] [US2] [BE] Agregar a `DocumentacionScalarPruebas.cs` la prueba del documento no disponible (RF-009). Comportamiento exacto: la página `/scalar/` siempre responde 200 `text/html` y contiene un aviso oculto con el texto literal `No se pudo cargar el contrato OpenAPI de BlackHellHeim API. Verifica que /openapi/v1.json responda e inténtalo de nuevo.` (elemento con `id="aviso-documento"` y atributo `hidden`) junto con un script en línea que hace `fetch('/openapi/v1.json')` y, si la respuesta no es 2xx o la solicitud falla, quita `hidden`. La prueba verifica en el HTML el texto literal, el `id`, el atributo `hidden` y la referencia a `/openapi/v1.json`. Una segunda prueba fuerza que la generación del documento falle (transformador de prueba que lanza) y comprueba que `/openapi/v1.json` responde 500 `application/problem+json` con `codigo: error_inesperado` sin detalles internos, mientras `/scalar/` sigue en 200 con el aviso. Debe fallar.
- [ ] T018 [P] [US2] [BE] Agregar a `DocumentacionScalarPruebas.cs` (con `FabricaApiPruebas.WithWebHostBuilder(b => b.UseEnvironment("Production"))`) que en entorno `Production` `GET /scalar` (siguiendo la redirección a `/scalar/`) y `GET /openapi/v1.json` responden 200, y que `GET /pruebas/error` responde 404. Debe fallar.

### Implementación

- [ ] T019 [US2] [BE] Registrar en `src/Api/Program.cs` `MapScalarApiReference()` con título `BlackHellHeim API`, activos locales y agente de IA desactivado.
- [ ] T020 [US2] [BE] Implementar el aviso de T017 en `src/Api/Program.cs`/`src/Api/OpenApi/` inyectando el elemento y el script en la cabecera o el cuerpo de la página de Scalar mediante la opción de contenido personalizado del paquete; si `Scalar.AspNetCore` 2.20.0 no la ofrece, servir `/scalar/` desde un endpoint propio que devuelva el HTML de Scalar con el aviso, sin CDN externo y sin cambiar las rutas `/scalar` y `/scalar/`.
- [ ] T021 [US2] [BE] Ejecutar `dotnet test --filter DocumentacionScalarPruebas` en verde (T016, T017, la de `Production` de T018 y el aviso de T020).
- [ ] T022 [US2] [MANUAL] Abrir `/scalar` en el navegador y confirmar visualmente que se ven título, versión y las tres operaciones.

## Fase 5: Historia 3 — Errores uniformes como problema (P3)

**Meta**: validación (400) y error inesperado (500) comparten formato, correlación y no filtran detalles.

**Prueba independiente**: `GET /tiendas/B` → 400 y un endpoint que lanza → 500, ambos `application/problem+json` con `codigo` e `idCorrelacion`.

### Pruebas primero

- [ ] T023 [P] [US3] [BE] Escribir `src/Pruebas/Api/ErroresUniformesPruebas.cs`, caso validación: `GET /tiendas/B` → 400 `application/problem+json`, `codigo`, `idCorrelacion`, `errores.codigo`, cabecera `X-Correlation-ID`, sin `traceId`. Debe fallar.
- [ ] T024 [P] [US3] [BE] Agregar a `ErroresUniformesPruebas.cs`, caso inesperado: `GET /pruebas/error` (endpoint de prueba que lanza una `InvalidOperationException` con un mensaje interno que no debe aparecer en la respuesta), disponible solo cuando el entorno no es `Production` (Development y Pruebas) y excluido del documento; → 500 `application/problem+json`, `codigo: error_inesperado`, mensaje genérico, sin traza, sin rutas internas ni detalles, `idCorrelacion` y `X-Correlation-ID`.
- [ ] T025 [P] [US3] [BE] Agregar a `ErroresUniformesPruebas.cs` que, si no existe correlación, el manejador la genera; y que `GET /pruebas/error` responde 404 en el entorno `Production` y no aparece en `/openapi/v1.json`.

### Implementación

- [ ] T026 [US3] [BE] Crear `src/Api/OpenApi/ProblemaDeError.cs` con la traducción `ErrorUniforme → ProblemDetails` (400, 404, 408, 409, 503, 500) reutilizando el mapeo de 002b.
- [ ] T027 [US3] [BE] Crear `src/Api/Errores/OpcionesProblemDetails.cs` (`CustomizeProblemDetails` que quita `traceId` y fija el formato).
- [ ] T028 [US3] [BE] Crear `src/Api/Errores/ManejadorExcepcionesGlobal.cs` (`IExceptionHandler`): `Tipo = Inesperado`, `Codigo = "error_inesperado"`, mensaje genérico e `IdCorrelacion` de `IContextoEjecucion`.
- [ ] T029 [US3] [BE] Registrar en `src/Api/Program.cs` `AddProblemDetails`, `AddExceptionHandler<ManejadorExcepcionesGlobal>` y `UseExceptionHandler()`; registrar `GET /pruebas/error` (lanza la excepción de T024) solo si `!app.Environment.IsProduction()`, con `ExcludeFromDescription()`.
- [ ] T030 [US3] [BE] Ejecutar `dotnet test --filter ErroresUniformesPruebas` en verde.

## Fase 6: Historia 4 — Contrato primero verificable (P4)

**Meta**: una prueba compara los contratos YAML con el documento publicado.

**Prueba independiente**: alterar un contrato en memoria hace fallar la prueba con una lista legible de diferencias.

### Pruebas primero

- [ ] T031 [P] [US4] [BE] Escribir `src/Pruebas/Contratos/LectorContratos.cs` (localiza la raíz con `BlackHellHeim.sln`, lee `specs/*/contratos/openapi.yaml` con YamlDotNet y convierte a JSON) con pruebas de lectura y de raíz no encontrada.
- [ ] T032 [P] [US4] [BE] Escribir `src/Pruebas/Contratos/ComparadorContratos.cs` y sus pruebas: composición (duplicado distinto es error, normaliza `openapi` a `3.1`, ignora `info`, `servers`, `tags`, ejemplos y `x-*`) y proyección (rutas, métodos, parámetros, cuerpos, respuestas, esquemas).
- [ ] T033 [US4] [BE] Escribir `src/Pruebas/Api/ContratoOpenApiPruebas.cs`: el documento publicado coincide con el contrato compuesto en ambos sentidos (operaciones y esquemas), `ProblemDetails` idéntico en todas las copias y cada operación con respuesta de error. Debe fallar si algo difiere.
- [ ] T034 [US4] [BE] Agregar a `ContratoOpenApiPruebas.cs` la prueba negativa que altera un contrato en memoria y comprueba que la comparación falla con un mensaje que nombra la operación o esquema diferente.

### Implementación

- [ ] T035 [US4] [BE] Ajustar `TransformadorDocumentoBlackHellHeim.cs` y los metadatos de los endpoints (`ProducesProblem`) hasta que T033 pase, y corregir los contratos de la Fase 2 si la diferencia es del contrato.
- [ ] T036 [US4] [BE] Ejecutar `dotnet test --filter "ContratoOpenApiPruebas|ComparadorContratos|LectorContratos"` en verde.

## Fase 7: Pulido y verificación final

- [ ] T037 [P] [BE] Confirmar que `src/Pruebas/Arquitectura` sigue esperando cinco proyectos y que no se crearon proyectos nuevos ni dependencias hacia afuera.
- [ ] T038 [P] [BE] Buscar secretos, trazas o rutas internas en las respuestas de error y en el documento publicado; deben ser cero (CE-006).
- [ ] T039 [MANUAL] Decidir si `/scalar` y `/openapi/v1.json` quedan públicos en el despliegue (decisión de las historias 005/006) y anotarlo en `specs/004-openapi-scalar/investigacion.md`.
- [ ] T040 [MANUAL] Actualizar `docs/scrum/backlog.md` (fila de la HU4: Spec = `specs/004-openapi-scalar`, Estado = Hecha) tras aprobar la verificación final, y el `**Estado**` de `spec.md`.
- [ ] T041 [BE] Verificación final: `dotnet build BlackHellHeim.sln` sin advertencias nuevas y `dotnet test BlackHellHeim.sln` en verde; arrancar con `dotnet run --project src/Api --launch-profile http` y comprobar `/openapi/v1.json`, `/scalar`, `GET /tiendas/B` (400) y `/salud` (200).

## Dependencias

- Fase 1 → Fase 2 → US1 → US2 y US3 (independientes entre sí, ambas necesitan US1 para el documento) → US4 (necesita US1 y US3).
- Externas: 002, 002b y 003 deben estar implementadas. La 005 no depende de esta, pero comparte el documento público.

## Ejecución en paralelo (ejemplos)

- Fase 2: T005, T006, T007 y T008 juntas.
- US1, pruebas: T009, T010, T011 juntas.
- US3, pruebas: T023, T024, T025 juntas.

## Estrategia

MVP = US1 (Fases 1 a 3). Después US2, US3 y US4 en ese orden. Los contratos se alinean primero y las pruebas de cada historia se ven fallar antes de implementar. Si T001 falla, no se implementa nada.
