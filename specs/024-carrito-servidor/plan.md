# Plan 024: Carrito de compras (parte API)

**Spec**: [spec.md](spec.md) · **Investigación**: [research.md](research.md) · **Modelo**: [modelo-datos.md](modelo-datos.md) · **Contrato**: [contratos/openapi.yaml](contratos/openapi.yaml) · **Validación**: [quickstart.md](quickstart.md)

## Contexto técnico
.NET 10, EF Core (SQL Server), Mediator, xUnit; mismo patrón que HU-16/19/20. **Dependencias nuevas: ninguna** (principio IV).

## Revisión de la constitución
| Principio | Resultado |
|---|---|
| I Arquitectura limpia | Cumple: `Dominio/Carritos`, `Aplicacion/Carritos`, repositorio en Infraestructura, DTO en la API. |
| II Contrato primero | Cumple: contrato completo antes del código. |
| III Pruebas | Cumple (ver Pruebas). |
| IV Software libre | Cumple. |
| V Precio en backend | Cumple: ningún precio entra por la solicitud ni se guarda (RF-003). |
| VI–VII | No aplica (sin webhooks ni proveedores nuevos; el motor de precios ya es un puerto). |
| VIII Sesión | Cumple: todos los endpoints exigen sesión; sin compra de invitado. |
| IX–XI | No aplica / cumple. |

## Decisiones
1. `Carrito` (por usuaria) con `LineaCarrito`; reglas en el dominio (cantidad 1–10, ≤20 líneas, fusión con tope).
2. Casos de uso: `ObtenerCarrito`, `FijarLineaCarrito`, `VaciarCarrito`, `FusionarCarrito`; el DTO se arma con `ICalculadoraPrecios.PrepararAsync` y los datos de la variante/diseño publicado.
3. La variante se identifica por `varianteId`; hay que **agregar `varianteId` a `VarianteCatalogoDto` y a `specs/019-api-catalogo/contratos/openapi.yaml`** (compatible hacia atrás).
4. Una línea cuya variante ya no está activa o cuyo diseño no está publicado se devuelve `disponible: false` y no suma al total.
5. Errores con ProblemDetails; `503` si no hay tipo de cambio.

## Estructura
`Dominio/Carritos`, `Aplicacion/Carritos`, `Infraestructura/Carritos` (repositorio), `Api/Endpoints/CarritoEndpoints.cs`, migración `AgregarCarrito`.

## Pruebas
Dominio (límites, fusión, tope), casos de uso con repositorio y calculadora dobles (éxito, error, validación, no disponible, sin tipo de cambio), endpoints (200/400/401/503), SQL Server (único por usuaria y por línea, CHECK), contrato.

## Bloqueos y [MANUAL]
Bloqueado por HU-13, 16, 20 (implementadas) y por el cambio al contrato de HU-19 (parte de esta HU). `[MANUAL]`: coordinar con `blackhellheim-web` el carrito local y la llamada de fusión.
