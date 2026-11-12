# Modelo de datos: Casos de uso con CQRS, mediador y pipeline

Historia 002b · Fecha: 2026-11-11

Esta historia no crea tablas ni esquema de base de datos. El modelo describe los tipos de
`Dominio`, `Aplicacion` y los mensajes que cruzan el pipeline; el adaptador de ejemplo
guarda las tiendas en memoria.

## Dominio

### `CodigoTienda` (objeto de valor)

| Campo | Tipo | Regla |
|-------|------|-------|
| `Valor` | `string` | Exactamente tres letras ASCII de la A a la Z, sin espacios |

- Se construye con `CodigoTienda.Crear(string)`, que recorta espacios, convierte a
  mayúsculas y valida la regla.
- Un valor inválido lanza `ArgumentException` en el dominio; la validación del pipeline
  lo rechaza antes de llegar al manejador.
- Igualdad por valor.

### `Tienda` (entidad de ejemplo)

| Campo | Tipo | Regla |
|-------|------|-------|
| `Codigo` | `CodigoTienda` | Obligatorio |
| `Nombre` | `string` | No vacío |
| `Moneda` | `string` | Tres letras mayúsculas; `MXN` para las tiendas de ejemplo |

- Es una entidad de solo lectura para esta historia; no tiene reglas de negocio del
  catálogo ni de precios.
- Los datos semilla son ilustrativos: `BHH` (BlackHellHeim) y `DEM` (Tienda de
  demostración).

## Aplicación

### `ConsultarTiendaQuery`

| Campo | Tipo | Validación |
|-------|------|------------|
| `Codigo` | `string` | Obligatorio; exactamente tres letras mayúsculas (`^[A-Z]{3}$`) |

- Implementa `IQuery<ResultadoCasoDeUso<TiendaDto>>` del paquete Mediator.
- Es de solo lectura; no modifica estado.

### `TiendaDto`

| Campo | Tipo |
|-------|------|
| `Codigo` | `string` |
| `Nombre` | `string` |
| `Moneda` | `string` |

- Es el modelo de lectura que devuelve la API; nunca se expone la entidad `Tienda`.

### `ResultadoCasoDeUso<T>`

| Campo | Tipo | Regla |
|-------|------|-------|
| `EsExito` | `bool` | Verdadero si hay valor |
| `Valor` | `T?` | Presente solo en éxito |
| `Error` | `ErrorUniforme?` | Presente solo en fallo |

- Constructores estáticos: `Exito(valor)` y `Fallo(error)`.
- Un resultado no encontrado es un fallo con `Error.Tipo = NoEncontrado`; no es una
  excepción.

### `ErrorUniforme`

| Campo | Tipo | Regla |
|-------|------|-------|
| `Tipo` | `TipoError` | `Validacion`, `NoEncontrado`, `Conflicto`, `Cancelado`, `Inesperado` |
| `Codigo` | `string` | Código estable en español, por ejemplo `solicitud_invalida` |
| `Mensaje` | `string` | Mensaje apto para el cliente, sin detalles internos |
| `Campos` | `IReadOnlyDictionary<string, string[]>?` | Errores por campo; solo en validación |
| `IdCorrelacion` | `string` | Identificador de la ejecución |

- No contiene trazas, rutas internas ni datos sensibles.

### `ErrorCampo`

| Campo | Tipo |
|-------|------|
| `Campo` | `string` |
| `Mensaje` | `string` |

### `RegistroCasoDeUso`

| Campo | Tipo | Regla |
|-------|------|-------|
| `TipoSolicitud` | `string` | Nombre del tipo de la solicitud |
| `Exito` | `bool` | Resultado de la ejecución |
| `DuracionMs` | `long` | Duración medida |
| `IdCorrelacion` | `string` | Identificador de la ejecución |
| `Momento` | `DateTimeOffset` | Instante de finalización |
| `CodigoError` | `string?` | Código uniforme cuando falla |
| `TipoExcepcion` | `string?` | Nombre del tipo de excepción, sin mensaje ni traza |

- No incluye la carga útil de la solicitud ni valores sensibles.

## Puertos

| Puerto | Capa | Método | Adaptador |
|--------|------|--------|-----------|
| `IContextoEjecucion` | Aplicación | `string IdCorrelacion { get; set; }` | `ContextoEjecucionHttp` (Api) |
| `IRegistroEjecucion` | Aplicación | `Registrar(RegistroCasoDeUso)` | `RegistroEjecucionLogger` (Api) |
| `IValidadorSolicitud<in TSolicitud>` | Aplicación | `IReadOnlyList<ErrorCampo> Validar(TSolicitud)` | Validadores de Aplicación |
| `IRepositorioTiendas` | Aplicación | `Task<Tienda?> ObtenerPorCodigoAsync(CodigoTienda, CancellationToken)` | `RepositorioTiendasEnMemoria` (Infraestructura) |

## Mensajes y comportamientos

| Tipo | Rol |
|------|-----|
| `ConsultarTiendaQuery` | Consulta CQRS de ejemplo |
| `ManejadorConsultarTienda` | `IQueryHandler<ConsultarTiendaQuery, ResultadoCasoDeUso<TiendaDto>>` |
| `ValidadorConsultarTienda` | `IValidadorSolicitud<ConsultarTiendaQuery>` |
| `CorrelacionBehavior<,>` | Garantiza `IdCorrelacion` |
| `ErroresBehavior<,>` | Convierte fallos no controlados en `ExcepcionCasoDeUso` |
| `RegistroBehavior<,>` | Mide y registra cada ejecución |
| `ValidacionBehavior<,>` | Ejecuta validadores antes del manejador |

## Estados y transiciones

No aplica: la consulta es de solo lectura y no hay máquina de estados. Los únicos estados
observables son el éxito (200), el error de validación (400), el no encontrado (404) y el
error inesperado (500).

## Relaciones

```text
Api ──envía──> ConsultarTiendaQuery ──pipeline──> ManejadorConsultarTienda
                                                      │
                                                      ├──> IRepositorioTiendas ──> Tienda
                                                      └──> ResultadoCasoDeUso<TiendaDto>
```

- `Api` no conoce `ManejadorConsultarTienda` ni `Tienda`; solo `ISender` y `TiendaDto`.
- `Aplicacion` conoce `Dominio`; `Dominio` no conoce a nadie.
- `Infraestructura` implementa el puerto y puede depender de `Aplicacion` y `Dominio`.
