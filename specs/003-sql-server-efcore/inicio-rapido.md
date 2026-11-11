# Inicio rápido: Base de datos local con migraciones y datos semilla

Historia 003 · Fecha: 2026-11-11

Guía para comprobar de punta a punta la base local, las migraciones, las semillas, la
configuración sin secretos y la salud separada del API.

## Requisitos

- SDK de .NET 10.
- Docker con Compose (probado con Docker 29 y Compose 5).
- Repositorio en la rama `sprint/01`.
- La historia 002b implementada, porque aporta `Tienda`, `CodigoTienda`, la correlación y el
  formato de errores. Sin ella, esta guía no compila.

## 1. Preparar la configuración local

```bash
cp .env.example .env
```

Edita `.env` y reemplaza los dos marcadores `__DEFINIR_CLAVE_LOCAL__` por una contraseña
local que cumpla la política de SQL Server (mínimo 8 caracteres, mayúsculas, minúsculas,
dígitos y símbolos). No subas `.env` al repositorio: ya está en `.gitignore`.

Resultado esperado:

- `.env` contiene una contraseña real solo en tu máquina.
- `.env.example` sigue versionado únicamente con marcadores.

## 2. Comando único

```bash
./scripts/base-datos.sh
```

El script valida `.env`, levanta `sqlserver` con `docker compose up -d --wait`, restaura la
herramienta local `dotnet-ef` y aplica las migraciones pendientes. Resultado esperado:

```text
Contenedor sqlserver saludable.
Migraciones aplicadas en la base BlackHellHeim.
Base de datos lista. Cadenas de conexión tomadas de .env.
```

Vuelve a ejecutarlo: debe terminar con el mismo resultado, sin errores y sin duplicar
semillas.

## 3. Comprobar esquema y semillas

```bash
set -a; . ./.env; set +a
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P "$BHH_SQL_SA_PASSWORD" -d BlackHellHeim \
  -Q "SELECT Codigo, Nombre, Moneda FROM dbo.Tiendas ORDER BY Codigo;"
```

Resultado esperado:

```text
Codigo Nombre                Moneda
------ --------------------- ------
BHH    BlackHellHeim         MXN
DEM    Tienda de demostración MXN

(2 rows affected)
```

Comprueba también el historial:

```bash
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P "$BHH_SQL_SA_PASSWORD" -d BlackHellHeim \
  -Q "SELECT MigrationId FROM dbo.__EFMigrationsHistory;"
```

Debe aparecer una sola fila con la migración `MigracionInicial`.

## 4. Revertir y volver a aplicar

```bash
set -a; . ./.env; set +a
dotnet tool restore
dotnet ef database update 0 -p src/Infraestructura -s src/Infraestructura
```

Resultado esperado: la tabla `dbo.Tiendas` y las semillas desaparecen; el historial queda
vacío. Vuelve a aplicar:

```bash
dotnet ef database update -p src/Infraestructura -s src/Infraestructura
```

Resultado esperado: la tabla, las dos semillas y el historial vuelven a estar completos.
Repite el paso 3 para confirmarlo.

## 5. Arrancar la API

```bash
set -a; . ./.env; set +a
dotnet run --project src/Api --launch-profile http
```

La API escucha en `http://localhost:5068`.

### Salud del API (sin base)

```bash
curl -i http://localhost:5068/salud
```

Resultado esperado:

```http
HTTP/1.1 200 OK
Content-Type: application/json

{"estado":"ok"}
```

### Salud de la base (disponible)

```bash
curl -i http://localhost:5068/salud/base
```

Resultado esperado:

```http
HTTP/1.1 200 OK
Content-Type: application/json

{"estado":"disponible"}
```

## 6. Salud de la base (no disponible)

Detén la base y repite las dos llamadas:

```bash
docker compose stop sqlserver
curl -i http://localhost:5068/salud
curl -i http://localhost:5068/salud/base
```

Resultado esperado:

- `/salud` sigue respondiendo `200` con `{"estado":"ok"}`.
- `/salud/base` responde `503` con `application/problem+json`, `codigo: base_no_disponible`
  e `idCorrelacion`, en menos de 5 segundos, sin mostrar la cadena de conexión ni detalles
  internos.

Vuelve a levantar la base con `./scripts/base-datos.sh` y confirma que `/salud/base`
regresa a `200`.

## 7. Configuración faltante o inválida

```bash
env -u ConnectionStrings__BlackHellHeim dotnet run --project src/Api --launch-profile http
```

Resultado esperado: la API falla al arrancar con un mensaje que nombra
`ConnectionStrings__BlackHellHeim` y remite a `.env.example`, sin imprimir el valor. Prueba
también con una contraseña incorrecta en `.env`: `/salud/base` responde `503` sin revelar el
valor.

## 8. Persistencia entre reinicios

```bash
docker compose restart sqlserver
```

Repite el paso 3: las dos semillas siguen ahí. Para recrear la base desde cero de forma
explícita y destructiva:

```bash
docker compose down -v
./scripts/base-datos.sh
```

Resultado esperado: el volumen se elimina, la base se recrea, la migración se aplica y las
semillas vuelven a quedar en dos filas.

## 9. Pruebas automáticas

```bash
dotnet build BlackHellHeim.sln
dotnet test BlackHellHeim.sln
```

Resultado esperado:

- Compilación sin errores ni advertencias nuevas.
- Todas las pruebas en verde, incluidas las de arquitectura y las de persistencia.
- Las pruebas de persistencia levantan un contenedor SQL Server desechable con
  Testcontainers; requieren Docker. Si Docker no está disponible, fallan con un mensaje
  explícito, no en silencio.

## Problemas frecuentes

| Síntoma | Causa probable | Acción |
|---------|----------------|--------|
| `port is already allocated` en 1433 | Otro proceso o contenedor usa el puerto | Detén el otro proceso o cambia el puerto en `.env` y en la cadena |
| `Login failed for user 'sa'` | Contraseña de `.env` distinta de la del volumen | Usa la contraseña original o recrea el volumen con `docker compose down -v` |
| `No se encontró la variable ConnectionStrings__BlackHellHeim` | `.env` incompleto o no cargado | Revisa `.env` y ejecuta el script, que lo carga |
| El script falla al migrar | Base aún iniciando o historial incompatible | Espera al `healthcheck` y revisa el mensaje; no borres datos sin decidirlo |
| Las pruebas de persistencia fallan al iniciar el contenedor | Docker no disponible o sin red para la imagen | Inicia Docker o descarga la imagen con `docker pull mcr.microsoft.com/mssql/server:2022-latest` |
