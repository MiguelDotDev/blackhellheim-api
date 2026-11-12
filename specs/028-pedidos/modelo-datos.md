# Modelo de datos 028
| Tabla | Campos | Restricciones |
|---|---|---|
| `Pedidos` | Id, UsuarioId, Estado (varchar 16), ClaveIdempotencia (varchar 64), HuellaCuerpo (char 64), SubtotalMxn, EnvioMxn, TotalMxn (decimal 18,2), MetodoEnvioId (varchar 60), MetodoEnvioNombre (varchar 100), DiasEnvioMin, DiasEnvioMax, DestinatarioNombre (varchar 120), Telefono (varchar 20), Calle (varchar 200), Ciudad (varchar 100), EstadoDomicilio (varchar 60), CodigoPostal (char 5), CreadoUtc, PagadoUtc?, ActualizadoUtc | FK a usuarios; único (UsuarioId, ClaveIdempotencia); índice (UsuarioId, CreadoUtc); `TotalMxn = SubtotalMxn + EnvioMxn`. |
| `LineasPedido` | Id, PedidoId, VarianteId, Cantidad (tinyint 1 a 10), PrecioUnitarioMxn (decimal 18,2) | FK a Pedidos (cascada) y a Variantes; único (PedidoId, VarianteId). |
| `MensajesSalida` | Id, Tipo (varchar 60), Carga (nvarchar max), CreadoUtc, ProcesadoUtc?, Intentos, ProximoIntentoUtc? (datetimeoffset), Error (varchar 500)? | Índice (ProcesadoUtc, CreadoUtc). |

Transiciones válidas: `PendientePago→Pagado|Cancelado`, `Pagado→EnProduccion|Reembolsado`, `EnProduccion→Enviado|Reembolsado`, `Enviado→Entregado`. Migración: `AgregarPedidos`.
