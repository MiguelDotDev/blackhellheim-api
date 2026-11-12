# Modelo de datos 024
| Tabla | Campos | Restricciones |
|---|---|---|
| `Carritos` | Id, UsuarioId, ActualizadoUtc | FK a usuarios (cascada); único (UsuarioId). |
| `LineasCarrito` | Id, CarritoId, VarianteId, Cantidad (tinyint) | FK a Carritos (cascada) y a Variantes; único (CarritoId, VarianteId); `Cantidad` entre 1 y 10 (restricción CHECK). |

Migración: `AgregarCarrito`. Nada de precios en la base: se calculan al consultar (principio V).
