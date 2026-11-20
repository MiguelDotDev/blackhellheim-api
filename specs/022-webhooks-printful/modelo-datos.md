# Modelo de datos 022
| Tabla | Campos | Restricciones |
|---|---|---|
| `EventosWebhook` | Id, Proveedor (varchar 20), IdExterno (varchar 100), Tipo (varchar 80), Estado (Recibido/Procesado/Fallido), Cuerpo (nvarchar max), RecibidoUtc, ProcesadoUtc?, Error (varchar 500)? | Índice único (Proveedor, IdExterno); índice (Estado, RecibidoUtc). |

Migración: `AgregarEventosWebhook`. El cuerpo se conserva para reintentos y auditoría; no contiene secretos.
