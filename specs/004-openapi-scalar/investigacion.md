# Investigación: Contrato OpenAPI publicado y documentación interactiva

Historia 004 · Fase 0 · Fecha: 2026-11-11

Este documento resuelve las incógnitas del contexto técnico del plan. No quedan
`NEEDS CLARIFICATION`. Las versiones y licencias se verificaron contra NuGet y los paquetes
descargados; el comportamiento del generador se comprobó con un proyecto de prueba en .NET
10 con `Microsoft.AspNetCore.OpenApi` 10.0.15 y `Scalar.AspNetCore` 2.20.0.

## D1. Generación del documento OpenAPI

**Decisión**: generar el documento en tiempo de ejecución con las funciones nativas de
ASP.NET Core de .NET 10 (`AddOpenApi("v1")` y `MapOpenApi("/openapi/{documentName}.json")`)
y publicarlo en `/openapi/v1.json`.

**Justificación**: es la función nativa que pidió la historia, no agrega un generador de
terceros y el documento refleja siempre los endpoints registrados. La ruta pública devuelve
el documento real que consume el frontend, así que no puede quedar una copia estática
obsoleta. En la prueba de compatibilidad el documento salió con `openapi: 3.1.1`, las rutas
`/salud`, los esquemas nombrados y `$ref` a `ProblemDetails` cuando se declara la respuesta.

**Alternativas consideradas**:
- Generación en tiempo de compilación con `Microsoft.Extensions.ApiDescription.Server`
  (MIT): produce un archivo que puede desincronizarse de la ruta publicada y agrega un paso
  de compilación; se puede evaluar en la HU6 para la CI, no aquí.
- Swashbuckle o NSwag: generadores de terceros, más configuración y no son la función
  nativa de .NET 10.
- Servir los contratos YAML tal cual: no refleja lo que el API implementa y rompe la
  verificación de coincidencia.

## D2. Página de documentación interactiva

**Decisión**: `Scalar.AspNetCore` 2.20.0 (MIT) con `MapScalarApiReference()` en la ruta por
defecto `/scalar`, apuntando al documento `v1`, con título **BlackHellHeim API**, activos
locales y el agente de IA desactivado.

**Justificación**: Scalar es la herramienta que nombran el backlog y la historia; el paquete
declara `net10.0` y usa activos locales por defecto, así que la página no depende de un CDN
para cargar. La prueba de compatibilidad confirmó que `/scalar` responde 302 hacia
`/scalar/` y que `/scalar/` devuelve 200 `text/html`. Desactivar el agente evita enviar datos
a un servicio externo y mantiene la historia sin dependencias de red.

**Alternativas consideradas**:
- Swagger UI: funcional, pero no es la herramienta pedida y agrega otro paquete.
- Redoc: buena lectura, pero la edición gratuita no ofrece la misma interacción para
  ejecutar operaciones.
- Servir una página propia: más código y mantenimiento sin ventaja.

## D3. Manejo global de excepciones y formato uniforme

**Decisión**: `AddProblemDetails()` + `UseExceptionHandler()` + un
`ManejadorExcepcionesGlobal : IExceptionHandler` que escribe con `IProblemDetailsService`.
El manejador construye un `ErrorUniforme` inesperado, reutiliza `ProblemaDeError` para
traducirlo y conserva el `IdCorrelacion` de `IContextoEjecucion` (002b). Se quita `traceId`
con `ProblemDetailsOptions.CustomizeProblemDetails`.

**Justificación**: `IProblemDetailsService` es el servicio estándar y escribe
`application/problem+json`; en la prueba de compatibilidad el manejador con
`WriteAsJsonAsync` producía `application/json` y el servicio estándar produjo el tipo
correcto. Centralizar la traducción `ErrorUniforme → ProblemDetails` garantiza que la
validación de 002b y el error inesperado tengan exactamente el mismo esquema. El servicio
estándar agrega `traceId` por su cuenta; como el contrato no lo declara y ya existe
`idCorrelacion`, se elimina para que la respuesta coincida con el contrato y no duplique
identificadores.

**Alternativas consideradas**:
- Middleware propio que escriba JSON: repite el formato, se desvía del estándar y es fácil
  que el tipo de contenido quede mal.
- Dejar el `traceId`: agrega un campo no contratado y confunde la correlación.
- Usar `HttpValidationProblemDetails` para la validación: esquema distinto al de
  `ProblemDetails` y rompe la uniformidad.

## D4. Contrato primero y verificación de coincidencia

**Decisión**: los archivos `specs/NNN-*/contratos/openapi.yaml` son la fuente de verdad. Una
prueba de xUnit los lee con YamlDotNet, los convierte a JSON, los compone (rutas y esquemas) y
compara una proyección semántica con el documento publicado en `/openapi/v1.json`. La prueba
falla con una lista legible de operaciones o esquemas faltantes, sobrantes o distintos, en
ambos sentidos.

**Justificación**: validar solo que el documento sea OpenAPI válido no detecta la deriva;
comparar el archivo completo contra el JSON generado sería frágil por `info`, `servers`,
`tags`, descripciones y ejemplos. La proyección compara lo que importa para un cliente:
rutas, métodos, parámetros, cuerpos, respuestas, tipos de contenido y esquemas. La prueba
negativa (alterar un contrato en memoria) demuestra que la verificación realmente falla.

**Alternativas consideradas**:
- Comparación textual del YAML contra el JSON: falla por formato, orden y metadatos.
- Validación con un lector de OpenAPI solamente: no compara contra los contratos.
- Revisión manual: no es verificable ni repetible.
- Generar el contrato desde el código y compararlo con un archivo estático: invierte la
  fuente de verdad y convierte el contrato en un artefacto derivado.

## D5. Esquema canónico de ProblemDetails y respuesta 500 global

**Decisión**: el contrato de 004 define el esquema canónico `ProblemDetails` (tipo, título,
estado, detalle, instancia, código, `idCorrelacion` y `errores`). Un transformador de
documento reemplaza el esquema generado por el canónico y agrega la respuesta `500` con
`application/problem+json` y `$ref` a `ProblemDetails` a todas las operaciones.

**Justificación**: el esquema que genera ASP.NET Core para `ProblemDetails` no incluye
`codigo`, `idCorrelacion` ni `errores`; el transformador los agrega una sola vez y evita
repetir `ProducesProblem(500)` en cada endpoint. La respuesta 500 es posible en cualquier
operación, así que documentarla globalmente es correcto y mantiene el contrato completo. La
prueba de compatibilidad confirmó que el transformador produce el `$ref` y el esquema
esperados, y que `MapOpenApi` y `MapScalarApiReference` no se documentan a sí mismos.

**Alternativas consideradas**:
- `ProducesProblem(500)` en cada endpoint: repetitivo y fácil de olvidar.
- Dejar el esquema estándar sin `codigo` ni `idCorrelacion`: no coincide con las respuestas
  reales ni con los contratos existentes.
- Documentar la respuesta 500 solo en algunas operaciones: deja huecos en el contrato.

## D6. Alineación de los contratos existentes

**Decisión**: actualizar los contratos de 002, 002b y 003 para que coincidan con el
generador: `openapi: 3.1.1`, esquema nombrado `EstadoSalud` en `/salud`, respuesta `500` en
todas las operaciones y `ProblemDetails` idéntico al canónico. Se sustituyen los `const` por
`examples` donde el generador no produce `const`.

**Justificación**: el generador de .NET 10 publica `3.1.1` (no `3.1.0`), nombra los esquemas
a partir de los tipos (`EstadoSalud`, `EstadoBaseDatos`, `Tienda`) y no agrega `const` a las
propiedades. Sin esta alineación, la comparación fallaría por diferencias de forma que no
cambian el comportamiento. Los contratos siguen siendo autónomos y legibles, y la prueba
exige que las copias de `ProblemDetails` sean idénticas.

**Alternativas consideradas**:
- Ignorar estas diferencias en la comparación: debilita la verificación y deja pasar cambios
  reales.
- Cambiar el código para producir `3.1.0` y `const`: el generador nativo no lo permite sin
  transformadores artificiales.
- Mantener los contratos desalineados: incumple el criterio de coincidencia.

## D7. Estrategia de pruebas

**Decisión**: xUnit con `WebApplicationFactory<Program>` para el documento, la página y los
errores; YamlDotNet 18.1.0 (MIT) para leer los contratos; un endpoint de prueba que lanza una
excepción, registrado solo cuando el entorno es `Pruebas` y excluido del documento, para
probar el 500 de punta a punta. No se requiere Docker ni base de datos; si 003 valida la
cadena de conexión, la fábrica inyecta una cadena ficticia.

**Justificación**: `WebApplicationFactory` prueba la ruta publicada real, no una copia del
documento. El endpoint de prueba permite verificar el manejador global sin agregar una ruta
de negocio ni exponerla en el contrato. YamlDotNet es la forma estándar de leer YAML en .NET
y su licencia es MIT. La prueba de compatibilidad confirmó que el endpoint de prueba
excluido no aparece en el documento y que el manejador global devuelve el problema uniforme.

**Alternativas consideradas**:
- Probar solo el `IExceptionHandler` con un `DefaultHttpContext`: no verifica el cableado del
  middleware.
- Agregar un endpoint de diagnóstico permanente: amplía la superficie pública y el alcance.
- Usar una base real para las pruebas: innecesario para esta historia y más lento.

## D8. Visibilidad del documento y de la página

**Decisión**: publicar `/openapi/v1.json` y `/scalar` en todos los entornos, sin
autenticación, durante esta historia.

**Justificación**: la historia pide una ruta pública y el despliegue a producción está fuera
de alcance; todavía no existe un entorno desplegado. Restringir por entorno ahora
contradiría el criterio de aceptación y complicaría las pruebas. La decisión de limitar la
documentación en producción queda para las historias 005 y 006, junto con la configuración
de despliegue.

**Alternativas consideradas**:
- Solo en desarrollo (`if (app.Environment.IsDevelopment())`): es la recomendación de Scalar,
  pero incumple la ruta pública pedida en esta historia.
- Exigir autenticación: fuera de alcance y contradice «ruta pública».
- Ocultar la página en producción: se puede hacer más adelante sin cambiar el contrato.

## D9. Licencias

Todas las dependencias nuevas son MIT; se verificó la expresión de licencia en el `.nuspec`
descargado de NuGet. No se agregan paquetes comerciales.

| Dependencia | Versión | Licencia | Verificación | Uso |
|-------------|---------|----------|--------------|-----|
| `Microsoft.AspNetCore.OpenApi` | 10.0.15 | MIT | `.nuspec` de NuGet | Generación nativa del documento |
| `Scalar.AspNetCore` | 2.20.0 | MIT | `.nuspec` de NuGet | Página interactiva |
| `Microsoft.OpenApi` | 2.13.0 | MIT | `.nuspec` de NuGet | Tipos y transformadores (transitiva) |
| `YamlDotNet` | 18.1.0 | MIT | `.nuspec` de NuGet | Lectura de contratos en pruebas |
| `xunit` | 2.9.3 | Apache 2.0 | Ya presente en el repositorio | Pruebas |
| `Microsoft.NET.Test.Sdk` | 18.11.1 | MIT | Ya presente en el repositorio | Pruebas |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.15 | MIT | Ya presente en el repositorio | Pruebas de la API |

Sin Swashbuckle, NSwag, Swagger UI, MediatR 13+, AutoMapper, FluentAssertions ni
dependencias de licencia comercial.

## Hallazgos de la prueba de compatibilidad

Se creó un proyecto desechable en `/tmp/opencode` con .NET 10, `Microsoft.AspNetCore.OpenApi`
10.0.15 y `Scalar.AspNetCore` 2.20.0. Resultados que sustentan el plan:

- El documento se publica en `/openapi/v1.json` con `openapi: 3.1.1`.
- `MapOpenApi()` y `MapScalarApiReference()` no agregan rutas al documento.
- `/scalar` responde 302 hacia `/scalar/`; `/scalar/` responde 200 `text/html`.
- Un transformador de documento puede fijar `info`, reemplazar el esquema `ProblemDetails` y
  agregar la respuesta `500` a todas las operaciones.
- `IProblemDetailsService` escribe `application/problem+json`; `WriteAsJsonAsync` escribía
  `application/json`.
- El servicio estándar agrega `traceId`; se elimina con `CustomizeProblemDetails`.
- Un endpoint excluido con `ExcludeFromDescription()` no aparece en el documento.
- El esquema generado para un `record EstadoSalud(string Estado)` es un `$ref` a
  `EstadoSalud` con `estado` requerido y tipo cadena, sin `const`.
