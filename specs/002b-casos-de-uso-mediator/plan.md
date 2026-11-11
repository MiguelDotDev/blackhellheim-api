# Plan de implementación: Casos de uso con CQRS, mediador y pipeline

**Rama**: `sprint/01` | **Fecha**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de funcionalidad en `specs/002b-casos-de-uso-mediator/spec.md`

**Complementa**: `specs/002-estructura-base-api` (HU2 del backlog)

## Resumen

La historia 002b añade a la estructura en capas ya existente el mecanismo común de casos
de uso: CQRS con un mediador generado por código fuente, cuatro comportamientos de
pipeline (correlación, manejo uniforme de errores, registro de ejecución y validación) y
una consulta de ejemplo que atraviesa API → Aplicación → Dominio sin que la API conozca
el manejador. La validación se implementa sin dependencias externas mediante un contrato
propio `IValidadorSolicitud<T>`. Los errores se convierten a ProblemDetails en la API con
un identificador de correlación. La consulta de ejemplo se apoya en un puerto
`IRepositorioTiendas` con un adaptador en memoria en `Infraestructura`, sin base de datos
ni proveedores externos. Las pruebas existentes, incluida la de arquitectura, se
mantienen y se amplían con pruebas unitarias de cada comportamiento y de la consulta.

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (destino `net10.0`).

**Dependencias principales**:
- `Mediator.Abstractions` 3.0.2 (MIT, verificado) en `Aplicacion`: contratos de mensajes,
  manejadores y comportamientos de pipeline.
- `Mediator.SourceGenerator` 3.0.2 (MIT, verificado) en `Api`, como generador de código y
  dependencia de desarrollo (`PrivateAssets=all`): genera el `IMediator`, el registro de
  manejadores y la extensión `AddMediator`.
- Sin librería externa de validación: se usa un contrato propio
  `IValidadorSolicitud<T>` y validadores escritos a mano. Se evaluó FluentValidation 12.x
  (Apache 2.0, libre) y se descartó para no ampliar la superficie de dependencias.
- Pruebas: `xunit` 2.9.3 (Apache 2.0), `xunit.runner.visualstudio` 4.0.1 (Apache 2.0),
  `Microsoft.NET.Test.Sdk` 18.11.1 (MIT) y `Microsoft.AspNetCore.Mvc.Testing` 10.0.15
  (MIT), ya presentes en la solución.
- Sin MediatR 13+, AutoMapper ni FluentAssertions (principio IV).

**Almacenamiento**: N/A. Sin base de datos. El puerto `IRepositorioTiendas` se implementa
con un diccionario en memoria en `Infraestructura`; los datos semilla (`BHH`, `DEM`) son
ilustrativos y no representan reglas de negocio.

**Pruebas**: xUnit en `src/Pruebas`, con tres frentes:
1. Pruebas unitarias de cada comportamiento del pipeline (éxito, error y validación) y del
   manejador de la consulta, con dobles de prueba para contexto, registro, validador y
   repositorio.
2. Pruebas en proceso de la API con `WebApplicationFactory`: 200, 400 con ProblemDetails,
   404, cabecera `X-Correlation-ID` y conservación de `/salud`.
3. La prueba de arquitectura existente, que sigue vigilando la matriz de dependencias sin
   cambios en sus expectativas (cinco proyectos).

Comando único de verificación: `dotnet test BlackHellHeim.sln`.

**Plataforma destino**: API HTTP en servidor Linux; desarrollo local.

**Tipo de proyecto**: servicio web (API) con arquitectura limpia.

**Metas de rendimiento**: N/A. El mediador con generación de código evita reflexión en
tiempo de ejecución; no se fijan métricas en esta historia.

**Restricciones**:
- La regla de dependencias no cambia: `Dominio` no depende de nada; `Aplicacion` solo de
  `Dominio`; `Api` puede depender de `Aplicacion`, `Dominio` e `Infraestructura`.
- La API no referencia manejadores concretos; solo el mecanismo de despacho (`ISender`).
- Sin secretos, sin base de datos, sin Scalar, sin transacciones, sin CI ni despliegue.
- Código, pruebas, contrato y documentación en español; marca **BlackHellHeim**.

**Escala/Alcance**: 1 consulta de ejemplo, 4 comportamientos de pipeline, 1 validador,
1 puerto y 1 adaptador en memoria; alrededor de 20 pruebas nuevas.

## Verificación de la constitución

*COMPUERTA: debe pasar antes de la investigación de la Fase 0. Se vuelve a comprobar
tras el diseño de la Fase 1.*

| # | Principio | Estado | Nota |
|---|-----------|--------|------|
| I | Arquitectura limpia | ✅ | CQRS con Mediator (MIT) y pipeline; `Api` solo conoce `ISender`; los manejadores viven en `Aplicacion`; `Dominio` no depende de nada. La prueba de arquitectura lo vigila. |
| II | Contrato primero | ✅ | `contratos/openapi.yaml` define `GET /tiendas/{codigo}`, sus errores ProblemDetails y la cabecera de correlación antes de implementar. Scalar queda fuera de alcance por indicación explícita. |
| III | Reglas de negocio probadas | ✅ | Cada comportamiento y el manejador tienen pruebas de éxito, error y validación; se conservan las pruebas existentes. |
| IV | Solo software libre | ✅ | `Mediator.Abstractions` y `Mediator.SourceGenerator` 3.0.2 son MIT (LICENSE verificado en el paquete). Sin librería de validación. Licencias anotadas en `investigacion.md`. |
| V | El precio lo calcula el backend | N/A | Sin precios en esta historia. |
| VI | Pedidos idempotentes y webhooks | N/A | Sin pedidos ni webhooks. |
| VII | Puertos y adaptadores | ✅ | `IRepositorioTiendas`, `IContextoEjecucion`, `IRegistroEjecucion` e `IValidadorSolicitud<T>` son puertos de `Aplicacion`; los adaptadores viven en `Infraestructura` y `Api`. Sin proveedores externos. |
| VIII | Seguridad y secretos | ✅ | Sin secretos. El registro no guarda datos sensibles; los errores no exponen trazas ni rutas internas. |
| IX | Código de creadora | N/A | Sin códigos de creadora. |
| X | Diseños originales | N/A | Sin diseños. |
| XI | Idioma y estilo | ✅ | Especificación, plan, código, pruebas y contrato en español; marca **BlackHellHeim**. |

Recomprobación tras la Fase 1: sin cambios. El diseño mantiene las compuertas en verde y
no requiere justificar violaciones; la tabla de complejidad queda vacía. La Definición de
Hecho incluye CI y análisis estático, que corresponden a la HU6 y están fuera del alcance
explícito de esta historia; se registran como pendientes en el backlog, no como una
violación de esta compuerta.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/002b-casos-de-uso-mediator/
├── plan.md               # Este archivo (salida de /speckit.plan)
├── investigacion.md      # Salida de la Fase 0 (/speckit.plan)
├── modelo-datos.md       # Salida de la Fase 1 (/speckit.plan)
├── inicio-rapido.md      # Salida de la Fase 1 (/speckit.plan)
├── contratos/
│   └── openapi.yaml      # Contrato de GET /tiendas/{codigo}
└── tasks.md              # Salida de /speckit.tasks (no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
src/
├── Dominio/
│   └── Tiendas/
│       ├── CodigoTienda.cs          # Objeto de valor: tres letras mayúsculas
│       └── Tienda.cs                # Entidad de ejemplo: código, nombre, moneda
├── Aplicacion/
│   ├── Comun/
│   │   ├── CasosDeUso/
│   │   │   └── ResultadoCasoDeUso.cs
│   │   ├── Correlacion/
│   │   │   └── IContextoEjecucion.cs
│   │   ├── Errores/
│   │   │   ├── ErrorCampo.cs
│   │   │   ├── ErrorUniforme.cs
│   │   │   ├── ExcepcionCasoDeUso.cs
│   │   │   └── ExcepcionValidacion.cs
│   │   ├── Registro/
│   │   │   ├── IRegistroEjecucion.cs
│   │   │   └── RegistroCasoDeUso.cs
│   │   ├── Validacion/
│   │   │   └── IValidadorSolicitud.cs
│   │   └── Comportamientos/
│   │       ├── CorrelacionBehavior.cs
│   │       ├── ErroresBehavior.cs
│   │       ├── RegistroBehavior.cs
│   │       └── ValidacionBehavior.cs
│   └── Tiendas/
│       ├── IRepositorioTiendas.cs
│       └── ConsultarTienda/
│           ├── ConsultarTiendaQuery.cs
│           ├── TiendaDto.cs
│           ├── ValidadorConsultarTienda.cs
│           └── ManejadorConsultarTienda.cs
├── Infraestructura/
│   └── Tiendas/
│       └── RepositorioTiendasEnMemoria.cs
├── Api/
│   ├── Comun/
│   │   ├── ContextoEjecucionHttp.cs
│   │   ├── MiddlewareCorrelacionYErrores.cs
│   │   ├── RegistroEjecucionLogger.cs
│   │   └── RegistroServicios.cs
│   ├── Endpoints/
│   │   └── TiendasEndpoints.cs
│   └── Program.cs
└── Pruebas/
    ├── Api/
    │   ├── SaludPruebas.cs              # Existente, sin cambios
    │   └── ConsultarTiendaPruebas.cs
    ├── Aplicacion/
    │   ├── Comportamientos/
    │   │   ├── CorrelacionBehaviorPruebas.cs
    │   │   ├── ErroresBehaviorPruebas.cs
    │   │   ├── RegistroBehaviorPruebas.cs
    │   │   └── ValidacionBehaviorPruebas.cs
    │   └── Tiendas/
    │       └── ManejadorConsultarTiendaPruebas.cs
    ├── Arquitectura/                    # Existente, sin cambios
    ├── Dobles/
    │   ├── ContextoEjecucionFalso.cs
    │   ├── RegistroEjecucionEnMemoria.cs
    │   ├── RepositorioTiendasFalso.cs
    │   └── ValidadorSolicitudFalso.cs
    └── Dominio/
        └── CodigoTiendaPruebas.cs
```

**Decisión de estructura**: se conservan los cinco proyectos y la matriz de dependencias
de `specs/002-estructura-base-api`. La única referencia nueva es `Api → Infraestructura`,
ya permitida por la matriz, para registrar el adaptador en memoria de
`IRepositorioTiendas`. Los puertos viven en `Aplicacion`; los adaptadores HTTP
(correlación, registro con `ILogger` y ProblemDetails) viven en `Api` porque dependen del
contexto de la petición; el adaptador de datos de ejemplo vive en `Infraestructura`.

## Fases de diseño

### Fase 0: investigación

Salida: `investigacion.md`. Resuelve las decisiones D1–D9: paquete del mediador y
licencia, estrategia de validación sin dependencias, forma del error uniforme, correlación,
registro, consulta de ejemplo, tiempo de vida del mediador, orden del pipeline y tabla de
licencias. No quedan `NEEDS CLARIFICATION`.

### Fase 1: diseño y contratos

Salidas:
- `modelo-datos.md`: objetos de valor, entidad de ejemplo, mensajes, resultados, errores,
  registros, puertos y reglas de validación.
- `contratos/openapi.yaml`: `GET /tiendas/{codigo}` con 200, 400, 404 y 500
  ProblemDetails, más la cabecera `X-Correlation-ID`.
- `inicio-rapido.md`: comandos de compilación, pruebas y llamadas `curl` con resultados
  esperados.

### Fase 2: tareas

`tasks.md` se genera con `/speckit.tasks`; no lo crea este comando.

## Orden del pipeline

El orden de registro define el anidamiento: el primero registrado es el más externo
(verificado en el generador de Mediator 3.0.2).

1. `CorrelacionBehavior<,>`: garantiza un identificador de correlación no vacío.
2. `ErroresBehavior<,>`: convierte errores no controlados y cancelaciones en
   `ExcepcionCasoDeUso` con `ErrorUniforme`; deja pasar las excepciones ya uniformes.
3. `RegistroBehavior<,>`: mide la duración y registra el resultado, sin alterar la
   respuesta ni propagar fallos del registro.
4. `ValidacionBehavior<,>`: ejecuta los validadores y lanza `ExcepcionValidacion` antes de
   invocar al manejador.
5. Manejador del caso de uso.

## Decisiones de despacho y composición

- `services.AddMediator(opciones => opciones.ServiceLifetime = ServiceLifetime.Scoped)` en
  `Api`; el tiempo de vida con ámbito permite inyectar el contexto de petición, el
  repositorio y los validadores con ámbito.
- Los comportamientos se registran como genéricos abiertos y con ámbito, en el orden
  anterior:
  `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CorrelacionBehavior<,>))`, etc.
- Los manejadores los registra el generador; la API solo inyecta `ISender` y envía la
  consulta.
- La consulta de ejemplo no usa base de datos: `RepositorioTiendasEnMemoria` devuelve
  `BHH` y `DEM`.

## Seguimiento de complejidad

Sin violaciones que justificar. La tabla queda vacía.
