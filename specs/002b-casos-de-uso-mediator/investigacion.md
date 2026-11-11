# Investigación (Fase 0): Casos de uso con CQRS, mediador y pipeline

Historia 002b · Fecha: 2026-11-11

La especificación no dejó `NEEDS CLARIFICATION`: el alcance, las historias y los criterios
están cerrados. Esta fase documenta las decisiones técnicas con su justificación, sus
alternativas y la verificación de licencias que exige el principio IV.

## D1. Paquete del mediador

**Decisión**: usar `Mediator.Abstractions` 3.0.2 en `Aplicacion` y
`Mediator.SourceGenerator` 3.0.2 en `Api`, ambos de Martin Othamar, con generación de
código fuente. El generador se marca como dependencia de desarrollo:

```xml
<PackageReference Include="Mediator.SourceGenerator" Version="3.0.2">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

**Justificación**:
- Es un paquete **MIT** (LICENSE incluido en ambos paquetes, verificado).
- Genera el `IMediator`, el registro de manejadores y la extensión `AddMediator`; no hay
  reflexión en tiempo de ejecución y los errores de configuración aparecen al compilar.
- Se verificó su funcionamiento con .NET 10 en un proyecto de prueba: `AddMediator` con
  `ServiceLifetime.Scoped`, un `IQueryHandler<,>`, un `IPipelineBehavior<,>` genérico
  abierto y `IMediator.Send` respondieron correctamente.
- La versión 3.0.2 es estable. La 3.1.0-rc.1 añade destino `net10.0`, pero es una
  versión candidata; se prefiere la estable, cuyo ensamblado `net8.0` es compatible con
  `net10.0`.

**Alternativas descartadas**:
- **MediatR 13+**: licencia comercial; prohibido por el principio IV.
- **Wolverine**: libre, pero trae un modelo de mensajería y persistencia que excede esta
  historia.
- **Mediador propio**: más código que mantener y sin las ventajas de la generación de
  código; la constitución pide Mediator.

## D2. Estrategia de validación

**Decisión**: validar **sin dependencias externas** mediante un contrato propio
`IValidadorSolicitud<in TSolicitud>` que devuelve una lista de `ErrorCampo`. El
`ValidacionBehavior<,>` resuelve todos los validadores registrados para el tipo de
solicitud, acumula los errores y lanza `ExcepcionValidacion` antes de invocar al
manejador. Los validadores se escriben a mano.

**Justificación**:
- Cero dependencias nuevas: se elimina el riesgo de licencia y se mantiene pequeña la
  superficie de paquetes.
- El contrato es explícito, fácil de probar y suficiente para las reglas actuales
  (campo obligatorio y formato).
- La acumulación de errores cubre el caso límite de varios errores en una solicitud.

**Alternativas descartadas**:
- **FluentValidation 12.x** (Apache 2.0, libre y verificada): buena opción, pero añade un
  paquete y un modelo de validación que todavía no se necesita. Queda como alternativa si
  el número de reglas crece.
- **DataAnnotations** (MIT, parte de .NET): está orientado a la validación de modelos de
  MVC y no encaja tan bien con solicitudes CQRS ni con la acumulación de errores propia.

## D3. Forma del error uniforme

**Decisión**: `ErrorUniforme` con `Tipo`, `Codigo`, `Mensaje`, `Campos` e `IdCorrelacion`.
Los tipos son `Validacion`, `NoEncontrado`, `Conflicto`, `Cancelado` e `Inesperado`; no
incluyen códigos HTTP. La API traduce el tipo a ProblemDetails:

| Tipo | HTTP | Código |
|------|------|--------|
| `Validacion` | 400 | `solicitud_invalida` |
| `NoEncontrado` | 404 | `recurso_no_encontrado` |
| `Conflicto` | 409 | `conflicto` |
| `Cancelado` | 408 | `operacion_cancelada` |
| `Inesperado` | 500 | `error_inesperado` |

**Justificación**: mantiene a `Aplicacion` libre de HTTP; la API conserva la decisión de
transporte. ProblemDetails es el formato que exige el principio II y permite añadir
extensiones (`codigo`, `idCorrelacion`, `errores`) sin romper el estándar.

**Alternativas descartadas**:
- Devolver códigos HTTP desde `Aplicacion`: acopla el dominio y los casos de uso al
  transporte.
- Un `Resultado<T>` para todos los errores: obliga a comprobar el resultado en cada
  manejador y en cada comportamiento; las excepciones uniformes son más directas para el
  rechazo de validación y los fallos inesperados.

## D4. Correlación

**Decisión**: `IContextoEjecucion` con `IdCorrelacion` mutable, registrado con ámbito. La
API lo llena desde la cabecera `X-Correlation-ID` o genera un `Guid` si falta o no es
válida; `CorrelacionBehavior<,>` garantiza que nunca quede vacío, incluso cuando se
despacha sin pasar por HTTP. La API devuelve siempre la cabecera y los errores incluyen
`idCorrelacion` en el cuerpo.

**Justificación**: el identificador debe existir antes de cualquier comportamiento para
que el registro y los errores lo compartan. La cabecera permite rastrear una petición
entre cliente y servidor.

**Alternativas descartadas**:
- Generarlo solo en el registro: los errores no lo tendrían.
- Usar `HttpContext.TraceIdentifier`: acopla `Aplicacion` a ASP.NET Core.

## D5. Registro de ejecución

**Decisión**: puerto `IRegistroEjecucion` en `Aplicacion` con `RegistroCasoDeUso` (tipo de
solicitud, éxito, duración, correlación, momento, código de error y tipo de excepción).
El adaptador `RegistroEjecucionLogger` vive en `Api` y escribe con `ILogger`. Un fallo del
registro se captura y se ignora: no altera la respuesta.

**Justificación**: `Aplicacion` no depende de `Microsoft.Extensions.Logging`; el puerto se
puede sustituir por un doble en las pruebas. El registro no incluye la carga útil de la
solicitud ni mensajes de excepción, por lo que no filtra datos sensibles.

**Alternativas descartadas**:
- Inyectar `ILogger<T>` directamente en `Aplicacion`: añade un paquete y acopla la capa a
  una biblioteca concreta.
- Guardar el mensaje y la traza de la excepción: riesgo de filtrar datos sensibles.

## D6. Consulta de ejemplo

**Decisión**: `ConsultarTiendaQuery(string Codigo)` devuelve
`ResultadoCasoDeUso<TiendaDto>`. El manejador usa el puerto `IRepositorioTiendas`, cuyo
adaptador `RepositorioTiendasEnMemoria` (en `Infraestructura`) devuelve dos tiendas de
ejemplo: `BHH` (BlackHellHeim, MXN) y `DEM` (Tienda de demostración, MXN). Si no existe,
el manejador devuelve un resultado no encontrado; no lanza una excepción.

**Justificación**:
- Cruza API → Aplicación → Dominio y añade `Infraestructura` solo como adaptador de datos.
- No usa base de datos ni proveedores externos (RF-014).
- El resultado no encontrado es un caso esperado, no un error excepcional.

**Alternativas descartadas**:
- Catálogo estático en `Dominio`: oculta el puerto y mezcla datos de ejemplo con el
  modelo.
- EF Core o SQL Server: fuera de alcance.

## D7. Tiempo de vida del mediador

**Decisión**: `ServiceLifetime.Scoped` para el mediador, manejadores, comportamientos,
validadores y adaptadores.

**Justificación**: el contexto de correlación, el repositorio y el registro se resuelven
por petición. Con `Singleton` no se podrían inyectar servicios con ámbito.

**Alternativas descartadas**: `Singleton` (mejor rendimiento, pero incompatible con el
contexto de petición) y `Transient` (más asignaciones sin beneficio).

## D8. Orden del pipeline

**Decisión**: registrar en este orden, que el generador anida del primero al último:

1. `CorrelacionBehavior<,>`
2. `ErroresBehavior<,>`
3. `RegistroBehavior<,>`
4. `ValidacionBehavior<,>`
5. Manejador

**Justificación**: el generador de Mediator 3.0.2 envuelve los comportamientos desde el
final hacia el principio, por lo que el primero registrado es el más externo (verificado
en el código del generador). Así la correlación existe antes de cualquier registro o
error; el registro ve el fallo original; la validación rechaza antes del manejador.

**Alternativas descartadas**: registro con `options.PipelineBehaviors` (también válido,
pero menos explícito para el orden y para las pruebas) y validación como
`MessagePreProcessor` (limita la acumulación de errores y el formato uniforme).

## D9. Licencias verificadas (principio IV)

| Paquete | Versión | Licencia | Verificación |
|---------|---------|----------|--------------|
| `Mediator.Abstractions` | 3.0.2 | MIT | LICENSE dentro del paquete y repositorio `martinothamar/Mediator` |
| `Mediator.SourceGenerator` | 3.0.2 | MIT | LICENSE dentro del paquete y mismo repositorio |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.0 (transitiva) | MIT | Paquete de .NET |
| `xunit`, `xunit.runner.visualstudio` | 2.9.3 / 4.0.1 | Apache 2.0 | Ya presente en la solución |
| `Microsoft.NET.Test.Sdk` | 18.11.1 | MIT | Ya presente en la solución |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.15 | MIT | Ya presente en la solución |
| .NET 10 SDK / ASP.NET Core | 10.0 | MIT | Repositorios `dotnet` y `aspnetcore` |

Evaluadas y **no usadas**: FluentValidation 12.x (Apache 2.0) y DataAnnotations (MIT).

Ningún paquete comercial; sin MediatR 13+, AutoMapper ni FluentAssertions.
