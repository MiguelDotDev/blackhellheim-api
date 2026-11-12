# Plan 030: Borrador del pedido en Printful

**Spec**: [spec.md](spec.md) · **Investigación**: [research.md](research.md) · **Modelo**: [modelo-datos.md](modelo-datos.md) · **Validación**: [quickstart.md](quickstart.md) · **Contrato**: no hay endpoints nuevos (trabajo interno en segundo plano), por eso no hay `contratos/`.

## Contexto técnico
.NET 10, EF Core, Mediator, `BackgroundService`, xUnit; reutiliza el outbox de HU-28 y el cliente de HU-15. **Dependencias nuevas: ninguna.**

## Revisión de la constitución
| Principio | Resultado |
|---|---|
| I | Cumple: `Dominio/Pedidos` (borrador), `Aplicacion/Pedidos`, adaptador en Infraestructura, despachador en la API. |
| II | Cumple por exclusión: sin endpoints nuevos no hay contrato nuevo. |
| III | Cumple (ver Pruebas). |
| IV | Cumple. |
| V | Cumple: no se calcula precio; se usan los montos congelados del pedido. |
| VI | Cumple: consumo idempotente del outbox. |
| VII | Cumple: Printful solo por `IProveedorImpresion` ampliado. |
| VIII | Cumple: el token ya existe como secreto; la bandera real está apagada por omisión. |
| IX–XI | No aplica / cumple. |

## Decisiones
1. Ampliar `IProveedorImpresion` con `CrearPedidoBorradorAsync`, `ObtenerPedidoPorExternalIdAsync` y `ConfirmarPedidoAsync` (actualizando adaptador y dobles).
2. Caso de uso `CrearBorradorPrintful(mensajeId)`: verifica estado del pedido (`Pagado`), arma la solicitud (arte público, variantes de Printful, domicilio, envío), crea o reutiliza el borrador, registra `BorradorPedido`, confirma solo con la bandera y pasa el pedido a `EnProduccion`.
3. `ServicioDespachoSalida : BackgroundService` con reclamo condicional y reintentos con espera creciente (5 intentos); falla definitiva marca `Error` y `ProcesadoUtc`.
4. Opciones `Printful__ConfirmarPedidosReales` (false) y `Salida__IntervaloSegundos` (15) validadas al arrancar.

## Estructura
`Dominio/Pedidos/BorradorPedido`, `Aplicacion/Pedidos/CrearBorradorPrintful`, `Infraestructura/Printful` y `Infraestructura/Pedidos`, `Api/Comun/RegistroSalida.cs`, migración `AgregarBorradoresPedido`.

## Pruebas
Caso de uso con Printful y repositorio dobles (éxito sin confirmar, confirmar con bandera, repetido, falla temporal reintentable, falla definitiva, pedido cancelado, diseño sin arte), despachador con reloj falso (espera creciente, tope de intentos), SQL Server (10 despachadores concurrentes, CE-002), adaptador con JSON de ejemplo.

## Bloqueos y [MANUAL]
Bloqueado por HU-28 y HU-29 (no implementadas). `[MANUAL]`: token y verificación de Printful, URL pública del arte, decisión de encender la bandera real.
