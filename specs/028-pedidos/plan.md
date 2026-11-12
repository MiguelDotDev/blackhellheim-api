# Plan 028: Pedidos con estados e idempotencia

**Spec**: [spec.md](spec.md) · **Investigación**: [research.md](research.md) · **Modelo**: [modelo-datos.md](modelo-datos.md) · **Contrato**: [contratos/openapi.yaml](contratos/openapi.yaml) · **Validación**: [quickstart.md](quickstart.md)

## Contexto técnico
.NET 10, EF Core, Mediator, xUnit; patrón de HU-16/17/22. **Dependencias nuevas: ninguna.**

## Revisión de la constitución
| Principio | Resultado |
|---|---|
| I | Cumple: `Dominio/Pedidos` (estados, congelación), `Aplicacion/Pedidos`, repositorio en Infraestructura, DTO en la API. |
| II | Cumple: contrato completo antes del código. |
| III | Cumple (ver Pruebas). |
| IV | Cumple. |
| V | Cumple: precios y envío del servidor, congelados al pagar. |
| VI | Cumple: outbox transaccional; idempotencia por clave. |
| VII | Cumple: la recotización pasa por el puerto de Printful (HU-26). |
| VIII | Cumple: solo con sesión, sin compra de invitado. |
| IX | Cumple: el campo de código de creadora llega con HU-31; esta HU no lo adelanta. |
| X–XI | No aplica / cumple. |

## Decisiones
1. `Pedido` (agregado con líneas) y `MensajeSalida`; transiciones solo por métodos del dominio.
2. `CrearPedido`: valida, recalcula líneas y envío, calcula la huella, inserta pedido y mensaje `pedido.creado` en una transacción; ante la violación del índice único relee y devuelve el existente (misma huella) o 409 (otra huella).
3. `MarcarPedidoPagado` (caso de uso interno para HU-29): transición a `Pagado`, congela montos, escribe `pedido.pagado`; idempotente.
4. `GET /pedidos/{id}` solo de la dueña (ajeno → 404).
5. Si HU-24 está implementada, vaciar el carrito del servidor tras la primera creación; si no, se omite (no bloquea).

## Estructura
`Dominio/Pedidos`, `Aplicacion/Pedidos`, `Infraestructura/Pedidos` (repositorio), `Api/Endpoints/PedidosEndpoints.cs`, migración `AgregarPedidos`.

## Pruebas
Dominio (todas las transiciones válidas e inválidas, congelación), casos de uso con dobles (éxito, repetido, clave con otro cuerpo, validaciones, sin tipo de cambio), endpoints (201/200/400/401/404/409/503), SQL Server (20 creaciones simultáneas, único, outbox transaccional), contrato.

## Bloqueos y [MANUAL]
Depende de HU-24 y HU-26 (specs listas, sin implementar) para el carrito y la cotización; el contrato recibe líneas y método de envío sin depender de ellas. `[MANUAL]`: datos mínimos del domicilio con la titular.
