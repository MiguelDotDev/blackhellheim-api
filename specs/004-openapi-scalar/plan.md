# Plan de implementación: Contrato OpenAPI publicado y documentación interactiva

**Rama**: `sprint/01` | **Fecha**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de funcionalidad en `specs/004-openapi-scalar/spec.md`

**Historia del backlog**: HU4 (componente BE)

## Resumen

La historia 004 publica el contrato OpenAPI del API en una ruta pública
(`/openapi/v1.json`) generada con las funciones nativas de ASP.NET Core de .NET 10, agrega
la página interactiva de Scalar en `/scalar` y centraliza los errores en un formato uniforme
de ProblemDetails con un manejador global de excepciones. El contrato primero se mantiene
con los archivos de `specs/NNN-*/contratos/openapi.yaml` como fuente de verdad: una prueba
de xUnit compone esos contratos, los compara con el documento realmente publicado y falla
con un mensaje legible ante cualquier diferencia. La prueba de contrato cubre `/salud`,
`/tiendas/{codigo}` y `/salud/base`, y exige que cada operación declare su respuesta de
error uniforme.

**Prerrequisito**: las historias 002, 002b y 003 deben estar implementadas. 002 aporta la
estructura y `/salud`; 002b aporta `/tiendas/{codigo}`, `ErrorUniforme`, la correlación y el
mapeo a ProblemDetails; 003 aporta `/salud/base`. Si alguna no está, esta historia se detiene
y se avisa: la prueba de contrato fallaría porque el documento publicado no tendría esas
operaciones, y publicar un contrato que el API no cumple contradice el principio II.

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (destino `net10.0`).

**Dependencias principales**:
- `Microsoft.AspNetCore.OpenApi` 10.0.15 (MIT, verificado) en `Api`: generación nativa del
  documento con `AddOpenApi()` y `MapOpenApi()`.
- `Scalar.AspNetCore` 2.20.0 (MIT, verificado) en `Api`: página interactiva con
  `MapScalarApiReference()`. El paquete declara `net10.0` y usa activos locales por defecto.
- `Microsoft.OpenApi` 2.13.0 (MIT, verificado), dependencia transitiva de
  `Microsoft.AspNetCore.OpenApi`: tipos del documento y transformadores.
- Pruebas: `YamlDotNet` 18.1.0 (MIT, verificado) para leer los contratos YAML; `xunit`
  2.9.3 (Apache 2.0), `Microsoft.NET.Test.Sdk` 18.11.1 (MIT) y
  `Microsoft.AspNetCore.Mvc.Testing` 10.0.15 (MIT), ya presentes.
- Sin Swashbuckle, NSwag, Swagger UI, MediatR 13+, AutoMapper, FluentAssertions ni paquetes
  comerciales.

**Almacenamiento**: no aplica. Esta historia no crea tablas ni toca la base de datos; solo
consume los endpoints existentes. Las pruebas usan una cadena de conexión ficticia para que
la API arranque sin una base real.

**Pruebas**: xUnit en `src/Pruebas`, con cuatro frentes:
1. Documento publicado: ruta, tipo de contenido, versión OpenAPI 3.1, operaciones y esquemas.
2. Contrato primero: composición de los contratos y comparación semántica con el documento
   publicado, con diferencias legibles.
3. Página interactiva: `/scalar` redirige a `/scalar/` y sirve HTML con el documento
   enlazado.
4. Errores uniformes: validación (400) y error inesperado (500) con el mismo formato,
   `application/problem+json`, correlación y sin detalles internos.

Comando único de verificación: `dotnet test BlackHellHeim.sln`.

**Plataforma destino**: desarrollo local en Linux/macOS con .NET 10; la API se ejecuta en el
host con `dotnet run`. No requiere Docker para esta historia.

**Tipo de proyecto**: servicio web (API) con arquitectura limpia.

**Metas de rendimiento**: el documento y la página deben responder en menos de 3 segundos en
local (CE-004). No hay metas de concurrencia ni de volumen.

**Restricciones**:
- Sin autenticación, sin nuevos endpoints de negocio, sin CI y sin despliegue.
- Sin secretos en el repositorio; el documento y la página son públicos y no exponen datos
  internos.
- La regla de dependencias no cambia; no se crean proyectos nuevos.
- Código, contratos, pruebas y documentación en español; marca **BlackHellHeim**.

**Escala/Alcance**: 1 documento OpenAPI, 1 página Scalar, 1 manejador global de excepciones,
1 transformador de documento que ajusta `info`, el esquema y las operaciones, 4 contratos
alineados y alrededor de 10 pruebas nuevas.

## Verificación de la constitución

*COMPUERTA: debe pasar antes de la investigación de la Fase 0. Se vuelve a comprobar
tras el diseño de la Fase 1.*

| # | Principio | Estado | Nota |
|---|-----------|--------|------|
| I | Arquitectura limpia | ✅ | OpenAPI, Scalar y el manejo de errores viven en `Api`; los contratos en `specs`; no se crean proyectos ni se rompe la matriz. |
| II | Contrato primero | ✅ | Los contratos se actualizan antes del código y una prueba verifica que el documento publicado coincide. Los errores usan ProblemDetails. |
| III | Reglas de negocio probadas | ✅ | Se prueban el documento, el contrato, la página, la validación y el error inesperado. Las pruebas existentes se mantienen. |
| IV | Solo software libre | ✅ | OpenAPI de ASP.NET Core, Scalar, Microsoft.OpenApi y YamlDotNet son MIT (verificado). Sin paquetes comerciales. |
| V | El precio lo calcula el backend | N/A | Sin precios en esta historia. |
| VI | Pedidos idempotentes y webhooks | N/A | Sin pedidos ni webhooks. |
| VII | Puertos y adaptadores | ✅ | No se agregan proveedores externos; el manejo de errores reutiliza los puertos de 002b. |
| VIII | Seguridad y secretos | ✅ | Documento y página públicos, sin secretos; las respuestas de error no exponen trazas, rutas internas ni configuración. |
| IX | Código de creadora | N/A | Sin códigos de creadora. |
| X | Diseños originales | N/A | Sin diseños. |
| XI | Idioma y estilo | ✅ | Contratos, código, pruebas y documentación en español; marca **BlackHellHeim**. |

Recomprobación tras la Fase 1: sin cambios. La tabla de complejidad queda vacía. La CI y el
análisis estático de la Definición de Hecho corresponden a la HU6 y están fuera del alcance
de esta historia.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/004-openapi-scalar/
├── plan.md               # Este archivo (salida de /speckit.plan)
├── investigacion.md      # Salida de la Fase 0 (/speckit.plan)
├── modelo-datos.md       # Salida de la Fase 1 (/speckit.plan)
├── inicio-rapido.md      # Salida de la Fase 1 (/speckit.plan)
├── contratos/
│   └── openapi.yaml      # Metadatos de publicación y esquema canónico de ProblemDetails
└── tasks.md              # Salida de /speckit.tasks (no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
src/Api/
├── OpenApi/
│   ├── TransformadorDocumentoBlackHellHeim.cs   # Info, ProblemDetails y 500 global
│   └── ProblemaDeError.cs                       # ErrorUniforme → ProblemDetails
├── Errores/
│   ├── ManejadorExcepcionesGlobal.cs            # IExceptionHandler
│   └── OpcionesProblemDetails.cs                # Quita traceId y fija el formato
├── Endpoints/
│   └── SaludEndpoints.cs                        # /salud con EstadoSalud y metadatos
└── Program.cs                                   # AddOpenApi, Scalar, ProblemDetails
src/Pruebas/
├── Api/
│   ├── OpenApiPublicadoPruebas.cs
│   ├── ContratoOpenApiPruebas.cs
│   ├── DocumentacionScalarPruebas.cs
│   └── ErroresUniformesPruebas.cs
└── Contratos/
    ├── LectorContratos.cs                       # YAML → JSON y composición
    └── ComparadorContratos.cs                   # Proyección y diferencias legibles
specs/002-estructura-base-api/contratos/openapi.yaml   # Se alinea
specs/002b-casos-de-uso-mediator/contratos/openapi.yaml # Se alinea
specs/003-sql-server-efcore/contratos/openapi.yaml      # Se alinea
```

**Decisión de estructura**: se conservan los cinco proyectos. `Api` es el punto de
composición y el único que conoce OpenAPI, Scalar y el manejador global. Las pruebas viven en
`Pruebas`, que ya referencia `Api`. Los contratos siguen en `specs` y no se copian al código:
la prueba los lee desde la raíz del repositorio.

## Diseño de la publicación y de los errores

### Documento OpenAPI

- `builder.Services.AddOpenApi("v1", opciones => ...)` registra el documento `v1`.
- `app.MapOpenApi("/openapi/{documentName}.json")` publica el documento en
  `/openapi/v1.json`, sin autenticación.
- Un `TransformadorDocumentoBlackHellHeim` (document transformer) hace tres cosas:
  1. fija `info.title = "BlackHellHeim API"` y `info.version` con la versión de la API;
  2. reemplaza `components.schemas.ProblemDetails` por el esquema canónico de los contratos
     (tipo, título, estado, detalle, instancia, código, correlación y errores por campo);
  3. recorre todas las operaciones y agrega la respuesta `500` con
     `application/problem+json` y `$ref` a `ProblemDetails`, de modo que ninguna operación
     quede sin error uniforme.
- El transformador no agrega rutas de infraestructura: `MapOpenApi` y
  `MapScalarApiReference` no aparecen en el documento (verificado en la prueba de
  compatibilidad), así que no hay recursión ni HTML documentado.
- Los endpoints de negocio declaran sus respuestas específicas (`400`, `404`, `408`, `503`)
  con `ProducesProblem` en el código de sus historias. La prueba de contrato exige que esas
  respuestas coincidan con los contratos.
- El documento se genera en cada solicitud a partir de los endpoints registrados; no se
  versiona una copia estática que pueda quedar obsoleta.
- Una prueba parsea el documento publicado con `Microsoft.OpenApi` para comprobar que no
  tiene errores de lectura ni referencias rotas; eso respalda que una herramienta estándar
  de generación de clientes o de mocks pueda consumirlo. No se agrega un generador de
  clientes al repositorio.

### Página de Scalar

- `app.MapScalarApiReference()` publica la página en `/scalar`; la ruta redirige a
  `/scalar/` y devuelve HTML con los activos locales del paquete (sin CDN).
- La página apunta al documento `v1` por la ruta por defecto de Scalar, que coincide con
  `/openapi/v1.json`.
- Se configura el título **BlackHellHeim API** y se desactiva el agente de IA de Scalar, que
  no forma parte del alcance y no debe enviar datos a servicios externos.
- No se agrega autenticación: la página es pública en esta historia. Restringirla por
  entorno o por red es una decisión de despliegue de las historias 005 y 006.

### Errores uniformes

- `builder.Services.AddProblemDetails()` registra el servicio estándar de ProblemDetails.
- `app.UseExceptionHandler()` y un `ManejadorExcepcionesGlobal : IExceptionHandler` capturan
  cualquier excepción no controlada y escriben un problema uniforme con
  `application/problem+json`.
- El manejador construye un `ErrorUniforme` con `Tipo = Inesperado`,
  `Codigo = "error_inesperado"`, un mensaje genérico y el `IdCorrelacion` de
  `IContextoEjecucion` (002b). Si la correlación aún no existe, la genera.
- `ProblemaDeError` centraliza la traducción `ErrorUniforme → ProblemDetails` y la usan
  tanto el mapeo de errores de 002b como el manejador global. Así la validación (400), el no
  encontrado (404), el conflicto (409), la cancelación (408), la base no disponible (503) y
  el error inesperado (500) comparten el mismo esquema.
- `OpcionesProblemDetails` configura `ProblemDetailsOptions.CustomizeProblemDetails` para
  quitar `traceId`, que el servicio estándar agrega por su cuenta y no está en el contrato.
  La correlación viaja en `idCorrelacion` y en la cabecera `X-Correlation-ID`.
- Las respuestas de error no incluyen trazas, rutas internas, cadenas de conexión ni
  valores de configuración.

### Contrato primero y prueba de coincidencia

Los contratos de `specs/NNN-*/contratos/openapi.yaml` son la fuente de verdad. La prueba
`ContratoOpenApiPruebas`:

1. Localiza la raíz del repositorio subiendo desde `AppContext.BaseDirectory` hasta encontrar
   `BlackHellHeim.sln` y `specs`.
2. Lee todos los `specs/*/contratos/openapi.yaml` con YamlDotNet y los convierte a JSON.
3. Compone un contrato esperado:
   - une las rutas y operaciones; un duplicado con contenido distinto es un error;
   - une `components.schemas`; un esquema duplicado con contenido distinto es un error;
   - ignora `info`, `servers`, `tags`, descripciones, resúmenes, ejemplos y extensiones
     `x-*`, que no afectan la forma del contrato;
   - normaliza `openapi` a `3.1` (el generador de .NET 10 publica `3.1.1`).
4. Obtiene el documento publicado con `WebApplicationFactory<Program>` en
   `/openapi/v1.json` y lo convierte a la misma proyección.
5. Compara rutas, métodos, parámetros, cuerpos de solicitud, respuestas (estado, tipos de
   contenido y `$ref`/esquema) y esquemas de componentes (tipo, formato, patrón, requeridos,
   propiedades, elementos y propiedades adicionales).
6. Falla con una lista legible de operaciones o esquemas faltantes, sobrantes o distintos.

La prueba también verifica que toda operación publicada esté en algún contrato y que toda
operación contratada esté publicada, en ambos sentidos. No se limita a validar que el
documento sea OpenAPI válido: detecta la deriva entre contrato e implementación.

### Alineación de contratos existentes

Para que la comparación sea posible y útil, esta historia ajusta los contratos sin cambiar
el comportamiento acordado:

- `openapi: 3.1.0` → `3.1.1` en los cuatro contratos, para coincidir con el generador.
- `specs/002`: `/salud` pasa de un esquema en línea a `$ref: EstadoSalud` y se agrega el
  esquema `EstadoSalud` (`estado` requerido, tipo cadena, ejemplo `ok`); se agrega la
  respuesta `500` con `ProblemDetails` y la copia canónica de `ProblemDetails` para que el
  archivo siga siendo autónomo.
- `specs/002b`: se conserva `ProblemDetails` y se verifica que sea idéntico al canónico.
- `specs/003`: `ProblemDetails` se completa con `errores` para que sea idéntico al canónico;
  se agrega la respuesta `500` a `/salud/base`.
- `specs/004`: define el esquema canónico `ProblemDetails`, documenta las rutas de
  publicación como extensiones `x-documento-openapi: /openapi/v1.json` y
  `x-documentacion-interactiva: /scalar`, y deja `paths: {}` porque no agrega operaciones de
  negocio.
- Se eliminan los `const` de los esquemas de respuesta que el generador no produce; se usan
  `examples` para conservar el valor esperado.

Cada contrato sigue siendo autónomo y legible; la prueba exige que las copias compartidas de
`ProblemDetails` sean idénticas.

### Pruebas

- `OpenApiPublicadoPruebas`: `GET /openapi/v1.json` responde 200, `application/json`,
  `openapi` 3.1, título **BlackHellHeim API**, contiene `/salud`, `/tiendas/{codigo}` y
  `/salud/base`, cada operación tiene `500` con `ProblemDetails`, y no contiene `/openapi`
  ni `/scalar`. El documento se parsea con `Microsoft.OpenApi` (ya transitivo) sin errores y
  sin referencias rotas, de modo que una herramienta estándar puede consumirlo.
- `ContratoOpenApiPruebas`: composición, comparación y diferencias legibles; incluye una
  prueba negativa que altera un contrato en memoria y comprueba que la comparación falla.
- `DocumentacionScalarPruebas`: `GET /scalar` redirige a `/scalar/`; `GET /scalar/` responde
  200 `text/html`, menciona **BlackHellHeim API** y referencia el documento; responde en
  menos de 3 segundos.
- `ErroresUniformesPruebas`:
  - validación: `GET /tiendas/B` → 400 `application/problem+json`, `codigo`,
    `idCorrelacion` y `errores.codigo`;
  - inesperado: un endpoint de prueba que lanza una excepción, registrado solo cuando el
    entorno es `Pruebas` y excluido del documento, → 500 `application/problem+json`, mismo
    esquema, `codigo: error_inesperado`, sin `traceId`, sin traza y sin detalles internos;
  - cabecera `X-Correlation-ID` conservada en ambos casos.
- `SaludPruebas` existente se mantiene; si 003 valida la cadena de conexión al arrancar, la
  fábrica de pruebas inyecta una cadena ficticia.

## Fases de diseño

### Fase 0: investigación

Salida: `investigacion.md`. Decisiones D1–D9: generación nativa en tiempo de ejecución,
Scalar y sus activos locales, manejador global de excepciones, verificación de contrato,
esquema canónico de ProblemDetails, alineación de versiones y esquemas, estrategia de
pruebas, visibilidad pública y tabla de licencias. No quedan `NEEDS CLARIFICATION`.

### Fase 1: diseño y contratos

Salidas:
- `modelo-datos.md`: documento publicado, página, contratos, esquema `ProblemDetails`,
  tabla de mapeo de errores, proyección de comparación y ciclo de vida del contrato.
- `contratos/openapi.yaml`: metadatos de publicación y esquema canónico `ProblemDetails`.
- `inicio-rapido.md`: compilación, arranque, documento, Scalar, error de validación, error
  inesperado, verificación de contrato y prueba negativa.

### Fase 2: tareas

`tasks.md` se genera con `/speckit.tasks`; no lo crea este comando.

## Seguimiento de complejidad

Sin violaciones que justificar. La tabla queda vacía.
