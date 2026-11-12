# Contrato operativo del entorno local

Historia 005 · No hay cambios en OpenAPI: no se agregan ni modifican endpoints.
`/salud` (002) y `/salud/base` (003) conservan su contrato.

## Comandos

| Comando | Efecto | Código de salida |
|---------|--------|------------------|
| `./scripts/entorno.sh` | Valida requisitos, construye y levanta `sqlserver`, `migraciones` y `api`; informa servicios sanos y `http://localhost:5068` | 0 sano; ≠ 0 fallo |
| `./scripts/entorno.sh --recrear` | Pide confirmación y ejecuta `docker compose down -v` antes de levantar | 0 / ≠ 0 |
| `./scripts/base-datos.sh` | Solo base + migración desde el host (HU3), sin cambios | 0 / ≠ 0 |
| `./scripts/verificar-entorno.sh` | Humo automatizado completo (requiere Docker) | 0 todas pasan |

## Errores y mensajes (sin valores secretos)

| Situación | Mensaje debe nombrar | Detalle |
|-----------|----------------------|---------|
| Docker o Compose ausente/antiguo | Qué falta y versión mínima | Antes de iniciar |
| `.env` ausente o con marcadores | `.env` y `.env.example` | Sin imprimir contenido |
| Variable obligatoria ausente | Nombre de la variable | Compose `${VAR:?}` |
| Puerto ocupado | Puerto y proceso | No deja contenedores a medias |
| Imagen no descargable | Imagen y causa de red | Distinto de fallo de la aplicación |
| Servicio no sano | Servicio, estado y últimas líneas filtradas | Sin cadenas de conexión |

## Sondas

- Vida: `GET /salud` → 200 `{"estado":"ok"}`.
- Listo: `GET /salud/base` → 200 `{"estado":"disponible"}` o 503 ProblemDetails
  (`codigo: base_no_disponible`).
- Contenedor `api`: `docker/sonda-http.sh /salud/base`, intervalo 10 s, 6 reintentos.
