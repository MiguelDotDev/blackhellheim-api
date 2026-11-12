# Modelo de datos 029
| Tabla | Campos | Restricciones |
|---|---|---|
| `IntentosPago` | Id, PedidoId, SesionStripeId (varchar 100), Url (varchar 500), Estado (`Creado`/`Pendiente`/`Pagado`/`Fallido`/`Expirado`, varchar 12), Metodo (varchar 30)?, MontoCentavos (bigint), CreadoUtc, ExpiraUtc, ActualizadoUtc | FK a Pedidos (cascada); único (SesionStripeId); índice (PedidoId, Estado). |

Transiciones: `Creado→Pendiente|Pagado|Fallido|Expirado`, `Pendiente→Pagado|Fallido|Expirado`; `Pagado`, `Fallido` y `Expirado` son finales. Migración: `AgregarIntentosPago`. Reutiliza `EventosWebhook` (HU-22) sin cambios de esquema.
