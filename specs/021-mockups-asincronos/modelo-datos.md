# Modelo de datos 021
| Tabla | Campos | Restricciones |
|---|---|---|
| `TareasMockup` | Id, DisenoId, PrintfulTaskId (varchar 60), Estado, Motivo (varchar 300)?, CreadaUtc, FinalizadaUtc? | FK Disenos; índice único (PrintfulTaskId); una sola `Pendiente` por diseño (índice filtrado). |
| `ImagenesMockup` | Id, TareaId, Color (varchar 60), Posicion (varchar 40), Url (varchar 500), NombreBlob | FK TareasMockup; único (TareaId, Color, Posicion). |

Migración: `AgregarMockups`.
