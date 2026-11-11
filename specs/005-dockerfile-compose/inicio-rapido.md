# Inicio rápido: Imagen del API y entorno con un solo comando

Historia 005 · Guía de validación de punta a punta.

## Requisitos

- Docker Engine 24+ con Compose v2 (probado con Docker 29 y Compose 5).
- Repositorio en `sprint/01` con las historias 002b y 003 implementadas.
- No se necesita el SDK de .NET en el host para este flujo.

## 1. Configuración

```bash
cp .env.example .env
```

Reemplaza `__DEFINIR_CLAVE_LOCAL__` por una contraseña local de SQL Server (≥ 8 caracteres,
sin `;`, `=` ni comillas). `.env` está ignorado por git.

## 2. Comando único

```bash
./scripts/entorno.sh
```

Esperado: construye la imagen, `sqlserver` y `api` quedan `healthy`, `migraciones` termina
con código 0 y el script imprime `http://localhost:5068`. Repítelo: termina sin error y sin
duplicar semillas.

## 3. Comprobar salud

```bash
docker compose ps
curl -i http://localhost:5068/salud
curl -i http://localhost:5068/salud/base
```

Esperado: ambas rutas 200.

## 4. Base caída y recuperación

```bash
docker compose stop sqlserver
curl -i http://localhost:5068/salud        # 200
curl -i http://localhost:5068/salud/base   # 503
docker compose start sqlserver             # api vuelve a healthy sin reiniciarse
```

## 5. Variable faltante

```bash
env -u BHH_SQL_SA_PASSWORD docker compose --env-file /dev/null config
```

Esperado: error que nombra `BHH_SQL_SA_PASSWORD`, sin valores.

## 6. Imagen

```bash
docker image inspect blackhellheim-api:local --format '{{.Config.User}}'   # app
docker run --rm --entrypoint dotnet blackhellheim-api:local --list-sdks     # no hay SDK
```

## 7. Verificación automática

```bash
dotnet test BlackHellHeim.sln                  # pruebas estáticas y existentes
./scripts/verificar-entorno.sh                 # humo con Docker, incluye reproducibilidad
```

## 8. Recrear desde cero (destructivo)

```bash
./scripts/entorno.sh --recrear
```

## Problemas frecuentes

| Síntoma | Causa | Acción |
|---------|-------|--------|
| `port is already allocated` | Puerto 1433 o 5068 ocupado | Libera el puerto o cambia `BHH_API_PUERTO` |
| `Login failed for user 'sa'` | Contraseña distinta de la del volumen | Restaura la original o `--recrear` |
| `api` queda `unhealthy` | Base caída o migración fallida | `docker compose logs migraciones sqlserver` |
| Fallo al descargar imagen | Sin red | Descárgala con `docker pull` y repite |
