# Modelo de datos 030
| Tabla | Campos | Restricciones |
|---|---|---|
| `BorradoresPedido` | Id, PedidoId, PrintfulOrderId (varchar 60), Estado (`Creado`/`Confirmado`/`Fallido`, varchar 12), Motivo (varchar 300)?, CreadoUtc, ActualizadoUtc | FK a Pedidos (cascada); único (PedidoId); único (PrintfulOrderId) filtrado a no nulos. |
| `MensajesSalida` (de HU-28) | sin cambios de esquema aquí | `ProximoIntentoUtc` lo define HU-28 en su migración; el despachador reclama con (Tipo, ProcesadoUtc nulo, ProximoIntentoUtc ≤ ahora). Falla definitiva = `ProcesadoUtc` con `Error` no nulo. |

Migración: `AgregarBorradoresPedido`.
