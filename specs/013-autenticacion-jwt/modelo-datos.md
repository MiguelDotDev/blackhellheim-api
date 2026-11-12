# Modelo de datos 013

Identity con claves `Guid`, tablas con prefijo en español mediante configuración EF (esquema `dbo`).

| Entidad | Campos | Notas |
|---|---|---|
| Usuario (`AspNetUsers`) | Id (Guid), Email, EmailNormalizado, HashContrasena, FechaCreacionUtc, contadores de bloqueo | Extiende `IdentityUser<Guid>` solo en Infraestructura. |
| Rol (`AspNetRoles`) | Id, Nombre | Semilla: `Cliente`, `Administrador` (migración, Ids fijos). |
| TokenRefresco (`TokensRefresco`) | Id, UsuarioId (FK), FamiliaId (Guid), HashToken (SHA-256, único), CreadoUtc, ExpiraUtc, UsadoUtc?, RevocadoUtc?, ReemplazadoPorId? | Dominio puro; índice en `HashToken` y `FamiliaId`. |

**Reglas**: refresh activo = sin `UsadoUtc`, sin `RevocadoUtc`, `ExpiraUtc` > ahora. Renovar marca `UsadoUtc` y crea hijo con la misma `FamiliaId` (actualización condicional atómica para la carrera de RF/HU3.4). Presentar uno con `UsadoUtc` o `RevocadoUtc` ⇒ revocar toda la familia.

**Migración**: `AgregarIdentidad` (tablas Identity + `TokensRefresco` + semilla de roles).
