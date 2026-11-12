# Modelo operativo: Imagen de contenedor del API y entorno local

Historia 005 · No hay entidades persistentes nuevas ni cambios de esquema.

## Servicios y dependencias

| Servicio | Imagen | Rol | Depende de | Estado terminal esperado |
|----------|--------|-----|------------|--------------------------|
| `sqlserver` | `mcr.microsoft.com/mssql/server` (digest) | Base local (HU3) | — | `healthy` |
| `migraciones` | etapa `migraciones` del Dockerfile | Aplica migraciones y semillas | `sqlserver` healthy | `exited (0)` |
| `api` | etapa `final` del Dockerfile | API BlackHellHeim | `sqlserver` healthy, `migraciones` completed | `healthy` |

## Estados de salud

| Componente | Sana cuando | No sana cuando |
|------------|-------------|----------------|
| `sqlserver` | `SELECT 1` responde | no responde tras `start_period` + reintentos |
| `api` (listo) | `GET /salud/base` → 200 | 503, timeout o proceso caído |
| `api` (vivo) | `GET /salud` → 200 | proceso caído |

Transiciones del API: `starting` → `healthy`; con la base detenida `healthy` → `unhealthy`
(vivo sigue en 200); al volver la base, `unhealthy` → `healthy` sin reiniciar.

## Variables de entorno

| Variable | Origen | Obligatoria | Uso |
|----------|--------|-------------|-----|
| `BHH_SQL_SA_PASSWORD` | `.env` (HU3) | Sí | Contraseña de `sa`; entrada de `MSSQL_SA_PASSWORD` |
| `ConnectionStrings__BlackHellHeim` | `.env` (HU3) para host; Compose la reconstruye para contenedores | Sí | Conexión del API y del *bundle* |
| `BHH_API_PUERTO` | `.env` (nuevo) | No (5068) | Puerto publicado del API |
| `ASPNETCORE_ENVIRONMENT` | Imagen | No | `Production` |

`.env.example` solo contiene marcadores (`__DEFINIR_CLAVE_LOCAL__`). Restricción: la
contraseña no debe contener `;`, `=` ni comillas.

## Volumen y artefactos

- Volumen `datos-sqlserver` (HU3): persiste entre reinicios; `--recrear` lo elimina con
  confirmación.
- Imagen `blackhellheim-api` (etiqueta local `blackhellheim-api:local`) con etiquetas OCI de
  versión y revisión; sin secretos.
