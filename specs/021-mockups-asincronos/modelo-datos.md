# Modelo de datos 021
| Tabla | Campos | Restricciones |
|---|---|---|
| `TareasMockup` | Id, DisenoId, PrintfulTaskId (varchar 60), Estado (`Pendiente`/`Completada`/`Fallida`, varchar 12), Motivo (varchar 300)?, CreadaUtc, FinalizadaUtc? | FK Disenos; único (PrintfulTaskId); índice único filtrado sobre DisenoId donde Estado = `Pendiente`; índice (Estado, CreadaUtc) para el polling. |
| `ImagenesMockup` | Id, TareaId, Color (varchar 60), Posicion (varchar 40), Url (varchar 500), NombreBlob (varchar 200) | FK TareasMockup (cascada); único (TareaId, Color, Posicion). |

Transiciones: `Pendiente` → `Completada` | `Fallida` (final). Migración: `AgregarMockups`.
