# Modelo del contrato y de los errores: OpenAPI publicado y documentación interactiva

Historia 004 · Fecha: 2026-11-11

Esta historia no crea tablas ni esquema de base de datos. El modelo describe el documento
publicado, los contratos que lo originan, el esquema uniforme de errores y las reglas de
comparación que mantienen el contrato primero.

## Rutas públicas de la historia

| Ruta | Método | Respuesta | Descripción |
|------|--------|-----------|-------------|
| `/openapi/v1.json` | GET | 200 `application/json` | Documento OpenAPI generado desde los endpoints registrados |
| `/scalar` | GET | 302 hacia `/scalar/` | Redirección de la página de documentación |
| `/scalar/` | GET | 200 `text/html` | Página interactiva de Scalar con el documento `v1` |

- Ninguna de las dos rutas requiere autenticación en esta historia.
- `MapOpenApi` y `MapScalarApiReference` no se documentan a sí mismos: el documento no
  contiene `/openapi` ni `/scalar`.
- El documento se genera en cada solicitud; no hay una copia estática versionada.

## Documento publicado

| Campo | Valor |
|-------|-------|
| Versión OpenAPI | `3.1.1` (generada por .NET 10) |
| Título | `BlackHellHeim API` |
| Versión de la API | La del ensamblado o la definida en el transformador |
| Servidores | El del entorno de ejecución (se ignora en la comparación) |
| Operaciones | Las implementadas: `/salud`, `/tiendas/{codigo}` y `/salud/base` |
| Respuesta de error global | `500` con `application/problem+json` y `$ref` a `ProblemDetails` |

## Contratos que componen el documento

| Archivo | Aporta | Cambios de esta historia |
|---------|--------|--------------------------|
| `specs/002-estructura-base-api/contratos/openapi.yaml` | `/salud` y `EstadoSalud` | `3.1.1`, esquema nombrado, `500` y `ProblemDetails` |
| `specs/002b-casos-de-uso-mediator/contratos/openapi.yaml` | `/tiendas/{codigo}`, `Tienda` y errores 400/404/408/500 | Se verifica que `ProblemDetails` sea idéntico al canónico |
| `specs/003-sql-server-efcore/contratos/openapi.yaml` | `/salud/base`, `EstadoBaseDatos` y 503 | `3.1.1`, `ProblemDetails` con `errores` y `500` |
| `specs/004-openapi-scalar/contratos/openapi.yaml` | Esquema canónico `ProblemDetails` y metadatos de publicación | Nuevo |

### Reglas de composición

1. Se leen todos los `specs/*/contratos/openapi.yaml`.
2. Las rutas y operaciones se unen; un duplicado con contenido distinto es un error.
3. Los esquemas de `components.schemas` se unen; un duplicado con contenido distinto es un
   error.
4. Se ignoran `info`, `servers`, `tags`, descripciones, resúmenes, ejemplos y extensiones
   `x-*` en la comparación.
5. La versión `openapi` se normaliza a `3.1` para aceptar `3.1.0` y `3.1.1`.
6. Cada contrato sigue siendo autónomo; las copias de `ProblemDetails` deben ser idénticas.

## Esquema canónico `ProblemDetails`

| Campo | Tipo | Requerido | Regla |
|-------|------|-----------|-------|
| `type` | cadena (`uri`) | Sí | URI que identifica el tipo de problema |
| `title` | cadena | Sí | Título legible del problema |
| `status` | entero (`int32`) | Sí | Código HTTP |
| `detail` | cadena | No | Detalle apto para el cliente |
| `instance` | cadena (`uri-reference`) | No | Ruta de la solicitud |
| `codigo` | cadena | Sí | Código estable del error |
| `idCorrelacion` | cadena | Sí | Identificador de correlación de la ejecución |
| `errores` | objeto de arreglos de cadenas | No | Errores por campo; solo en validación |

- No incluye `traceId`, trazas, rutas internas, cadenas de conexión ni valores de
  configuración.
- `errores` usa propiedades adicionales cuyos valores son arreglos de cadenas; por ejemplo,
  `{"codigo": ["El código debe tener exactamente tres letras mayúsculas."]}`.

## Esquema `EstadoSalud`

| Campo | Tipo | Requerido | Regla |
|-------|------|-----------|-------|
| `estado` | cadena | Sí | Valor esperado `ok`; se documenta con `examples`, no con `const` |

- Es el esquema de la respuesta 200 de `/salud`.
- Reemplaza el esquema en línea del contrato anterior para que el generador produzca un
  `$ref` con nombre.

## Mapeo uniforme de errores

| Situación | Código HTTP | `codigo` | Campos adicionales | Origen |
|-----------|-------------|----------|--------------------|--------|
| Validación | 400 | `solicitud_invalida` | `errores` por campo | 002b |
| No encontrado | 404 | `recurso_no_encontrado` | — | 002b |
| Conflicto | 409 | `conflicto` | — | 002b |
| Cancelado | 408 | `operacion_cancelada` | — | 002b |
| Base no disponible | 503 | `base_no_disponible` | — | 003 |
| Error inesperado | 500 | `error_inesperado` | — | 004 (manejador global) |

- Todos usan `application/problem+json` y el mismo esquema `ProblemDetails`.
- Todos incluyen `idCorrelacion` en el cuerpo y en la cabecera `X-Correlation-ID`.
- El manejador global construye un `ErrorUniforme` inesperado y lo traduce con
  `ProblemaDeError`, el mismo traductor que usa el mapeo de 002b.

## Proyección de comparación contrato ↔ documento

| Se compara | Se ignora |
|------------|-----------|
| Rutas y métodos | `info`, `servers`, `tags` |
| Parámetros: nombre, ubicación, requerido y esquema | Descripciones, resúmenes y ejemplos |
| Cuerpos de solicitud: requerido, tipos de contenido y esquema | Extensiones `x-*` |
| Respuestas: estado, tipos de contenido y `$ref` o esquema | Orden de propiedades |
| Esquemas: tipo, formato, patrón, requeridos, propiedades, elementos y propiedades adicionales | `const`, `default`, `nullable`, `title` |

- La comparación es en ambos sentidos: lo contratado debe estar publicado y lo publicado debe
  estar contratado.
- Un esquema o una operación duplicada con contenido distinto se reporta como conflicto.
- El mensaje de fallo lista las rutas, los métodos y los esquemas afectados.

## Ciclo de vida del contrato

```text
Contrato actualizado ──▶ Código implementado ──▶ Prueba de contrato
        ▲                                              │
        │                                              ▼
        └──────────── corrección ◀──── diferencia detectada
```

- Sin diferencia: la prueba pasa y el documento publicado se considera conforme.
- Diferencia: la prueba falla, indica la operación o el esquema y no se publica como
  acordado hasta corregir el contrato o la implementación.
- El contrato se actualiza antes del código; la prueba es la garantía de que la fuente de
  verdad y el documento publicado no se separan.

## Visibilidad y seguridad

- El documento y la página son públicos en esta historia; no hay autenticación.
- El documento no incluye secretos, cadenas de conexión ni datos de configuración.
- Las respuestas de error no exponen trazas ni detalles internos.
- Restringir la documentación por entorno o por red queda para las historias de despliegue.
