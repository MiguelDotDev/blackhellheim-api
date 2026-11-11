# Informe: implementación de las HU de los sprints 3 a 5

Fecha: 2026-11-11. Sin `git push`, sin `--amend`, sin tocar `sprint/01` ni `sprint/02`, sin `.env` con valores reales. Las excepciones a AGENTS.md (no detenerse tras cada HU) fueron acordadas para la sesión.

## Paso 0: prueba de fusión
Rama temporal desde `sprint/02`, fusionando `sprint/03`, `sprint/04` y `sprint/05` en ese orden con merge commit: **sin conflictos** en ningún archivo (incluido `docs/scrum/backlog.md`). La rama temporal se borró.

## HU implementadas

| HU | Rama | Commit | Pruebas |
|---|---|---|---|
| 13 · registro e inicio de sesión con JWT | sprint/03 | `3ec10d9` | 201 en verde (build y todas las pruebas, con SQL Server en contenedor) |
| 15 · cliente de Printful | sprint/04 | `9388eb0` | 247 en verde (suma las de HU-13) |
| 16 · dominio de diseño, producto base y variante | sprint/04 | `15173f0` | 296 en verde |

En `sprint/04` hay además un merge commit (`99cff36`) que trae `sprint/03` (HU-13), porque los endpoints de administración de las HU-15 y HU-16 exigen el rol Administrador. Al fusionar `sprint/03` y `sprint/04` en `developer` no habrá conflicto.

Cada commit incluye en el mismo commit las tareas marcadas en `tasks.md` y la fila del backlog («Hecha»).

## HU omitidas
- **HU-17** (decodificador PNG sin decidir, T002) y **HU-20** (redondeo sin decidir): no se implementaron, como se pidió.
- **HU-19**: depende de la HU-20 (precio por `ICalculadoraPrecios`) y de datos de la HU-16/17; se deja sin tocar.
- **HU-25**: no se implementa en esta sesión.

## [MANUAL] pendientes
- **HU-13** T016: definir `BHH_JWT_CLAVE` en tu `.env` local (sin ella `docker compose up` falla a propósito: la API exige `Jwt__Clave`); opcionalmente `BHH_ADMIN_CORREO` y `BHH_ADMIN_CONTRASENA`; en producción, secretos de Azure (HU-25).
- **HU-15** T003: crear cuenta y token privado de Printful (`BHH_PRINTFUL_TOKEN`); verificar con token real la forma de `prices`, los nombres de regiones de venta (`mexico`, `north_america`, `worldwide` son supuestos configurables) y el filtro de categoría (ver `specs/015-cliente-printful/investigacion.md`).
- **HU-16** T010: curar `Disenos:TerminosBloqueados` (marcas y bandas de terceros).
- No se reconstruyó ni arrancó la imagen Docker: solo se comprobó `dotnet restore --locked-mode` y el build en Release.

## Decisiones técnicas y licencias
| Decisión | Licencia |
|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.15 | MIT (verificada en el nuspec) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.15 | MIT |
| `Microsoft.IdentityModel.JsonWebTokens` 8.23.0 | MIT |
| Refresh propio con hash SHA-256, rotación atómica (`ExecuteUpdate`) y revocación de familia | BCL |
| Reintentos de Printful con `DelegatingHandler` propio (429 con `Retry-After`, tope 60 s, 3 reintentos) | sin dependencia |
| HU-15 y HU-16 sin paquetes nuevos | — |

Otras decisiones: el administrador inicial se siembra con un servicio en segundo plano de la API (no conecta si no hay credenciales configuradas); `ObtenerDisponibilidadAsync` se integró en `ListarVariantesAsync`; sin token de Printful la API arranca y los endpoints responden 502; ETag y `If-Match` con `rowversion` para los diseños; nuevos tipos de error (401, 403, 412, 423, 502, 503 con `retryAfter`, 504).

## Cambios que conviene conocer
- **Contratos pendientes**: `LectorContratos` omite los contratos con `x-pendiente: true`. Se marcó el de la HU-17 (no implementada). **Al fusionar `sprint/04` en `sprint/05`, hay que marcar igual los contratos de las HU-19 y HU-20** o la prueba de contrato fallará; `sprint/05` no se tocó.
- `sprint/05` conserva la base anterior (no contiene el código de HU-13/15/16).
- Las pruebas existentes se actualizaron donde cambió el estado esperado: lista de migraciones, `.env.example` y fábrica de pruebas (clave JWT aleatoria por ejecución).
