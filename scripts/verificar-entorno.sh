#!/usr/bin/env bash
# Humo automatizado del entorno local (historia 005). Requiere Docker. Comprueba salud, caída y
# recuperación de la base, esquema al día, idempotencia, secretos, imagen, reproducibilidad,
# entorno Production y documentación pública. Sale con 1 si algún paso falla.
# Usa .env o el archivo indicado en BHH_ENV_ARCHIVO. Opciones: --sin-reproducibilidad.
set -uo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"
archivo_env="${BHH_ENV_ARCHIVO:-.env}"
export BHH_ENV_ARCHIVO="$archivo_env"
con_reproducibilidad=true
[[ "${1:-}" == "--sin-reproducibilidad" ]] && con_reproducibilidad=false

fallos=0
omitidos=0
ok() { echo "  OK     $1"; }
fallo() { echo "  FALLO  $1" >&2; fallos=$((fallos + 1)); }
omitido() { echo "  OMITIDO $1"; omitidos=$((omitidos + 1)); }
paso() { echo; echo "== $1 =="; }

[[ -f "$archivo_env" ]] || { echo "ERROR: no existe $archivo_env" >&2; exit 1; }
set -a
# shellcheck disable=SC1090
. "$archivo_env"
set +a
puerto="${BHH_API_PUERTO:-5068}"
base="http://localhost:${puerto}"
compose() { docker compose --env-file "$archivo_env" "$@"; }
codigo_http() { curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$1" 2>/dev/null || echo 000; }
estado_servicio() { docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$(compose ps -aq "$1")" 2>/dev/null || echo ausente; }
esperar() { # esperar <segundos> '<condición evaluada en cada intento>'
  local limite="$1" condicion="$2"
  local hasta=$((SECONDS + limite))
  until eval "$condicion" >/dev/null 2>&1; do
    [[ $SECONDS -ge $hasta ]] && return 1
    sleep 2
  done
}
contar_tiendas() {
  compose exec -T sqlserver sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -C -h -1 -W -S localhost -U sa -d BlackHellHeim -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.Tiendas"' | tr -d '[:space:]'
}
sin_secretos() {
  local texto
  texto="$(cat)"
  ! grep -qF -- "$BHH_SQL_SA_PASSWORD" <<<"$texto" && ! grep -qiE 'Password=[^*[:space:];"]' <<<"$texto"
}

paso "1. Un solo comando deja el entorno sano"
salida="$(./scripts/entorno.sh 2>&1)"; estado=$?
[[ $estado -eq 0 ]] && ok "entorno.sh terminó en 0" || fallo "entorno.sh falló ($estado)"
[[ "$(estado_servicio sqlserver)" == "healthy" ]] && ok "sqlserver healthy" || fallo "sqlserver no está healthy"
[[ "$(estado_servicio api)" == "healthy" ]] && ok "api healthy" || fallo "api no está healthy"
[[ "$(docker inspect -f '{{.State.ExitCode}}' "$(compose ps -aq migraciones)" 2>/dev/null)" == "0" ]] \
  && ok "migraciones terminó con código 0" || fallo "migraciones no terminó con código 0"
[[ "$(codigo_http "$base/salud")" == "200" ]] && ok "GET /salud 200" || fallo "GET /salud no es 200"
[[ "$(codigo_http "$base/salud/base")" == "200" ]] && ok "GET /salud/base 200" || fallo "GET /salud/base no es 200"
sin_secretos <<<"$salida" && ok "la salida del comando no contiene secretos" || fallo "la salida del comando contiene secretos"

paso "2. Esquema al día (migraciones aplicadas = migraciones del código)"
aplicadas="$(compose exec -T sqlserver sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -C -h -1 -W -S localhost -U sa -d BlackHellHeim -Q "SET NOCOUNT ON; SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId"' | tr -d '\r' | sed '/^$/d' | sort)"
esperadas="$(find src/Infraestructura/Persistencia/Migraciones -name '*.cs' ! -name '*.Designer.cs' ! -name '*ModelSnapshot.cs' -printf '%f\n' | sed 's/\.cs$//' | sort)"
if [[ -n "$esperadas" && "$aplicadas" == "$esperadas" ]]; then
  ok "migraciones aplicadas: $(tr '\n' ' ' <<<"$aplicadas")"
else
  fallo "difieren las migraciones. Aplicadas: [$(tr '\n' ' ' <<<"$aplicadas")] Esperadas: [$(tr '\n' ' ' <<<"$esperadas")]"
fi

paso "3. Base caída: el API deja de estar sano y se recupera sin reiniciarse"
inicio_api="$(docker inspect -f '{{.State.StartedAt}}' "$(compose ps -aq api)")"
compose stop sqlserver >/dev/null 2>&1
esperar 30 '[[ "$(codigo_http "$base/salud/base")" == "503" ]]' && ok "GET /salud/base 503 con la base detenida" || fallo "GET /salud/base no dio 503 con la base detenida"
[[ "$(codigo_http "$base/salud")" == "200" ]] && ok "GET /salud sigue en 200 (vida)" || fallo "GET /salud dejó de responder"
esperar 150 '[[ "$(estado_servicio api)" == "unhealthy" ]]' && ok "el contenedor api pasó a unhealthy" || fallo "el contenedor api no pasó a unhealthy"
compose start sqlserver >/dev/null 2>&1
esperar 150 '[[ "$(estado_servicio sqlserver)" == "healthy" ]]' && ok "sqlserver volvió a healthy" || fallo "sqlserver no volvió a healthy"
esperar 150 '[[ "$(estado_servicio api)" == "healthy" ]]' && ok "api volvió a healthy" || fallo "api no volvió a healthy"
[[ "$(docker inspect -f '{{.State.StartedAt}}' "$(compose ps -aq api)")" == "$inicio_api" ]] \
  && ok "el contenedor api no se reinició" || fallo "el contenedor api se reinició"

paso "4. Idempotencia: ejecutar de nuevo no falla ni duplica semillas"
./scripts/entorno.sh >/dev/null 2>&1 && ok "segunda ejecución en 0" || fallo "la segunda ejecución falló"
[[ "$(contar_tiendas)" == "2" ]] && ok "dbo.Tiendas tiene 2 filas" || fallo "dbo.Tiendas no tiene 2 filas"

paso "5. Variable obligatoria ausente: falla nombrándola y sin mostrar valores"
mensaje="$(env -u BHH_SQL_SA_PASSWORD docker compose --env-file /dev/null config 2>&1)"; estado=$?
[[ $estado -ne 0 ]] && ok "docker compose falla sin la variable" || fallo "docker compose no falló sin la variable"
grep -q 'BHH_SQL_SA_PASSWORD' <<<"$mensaje" && ok "el mensaje nombra la variable" || fallo "el mensaje no nombra la variable"
sin_secretos <<<"$mensaje" && ok "el mensaje no contiene secretos" || fallo "el mensaje contiene secretos"
salida="$(echo no | ./scripts/entorno.sh --recrear 2>&1)"; estado=$?
[[ $estado -ne 0 ]] && [[ "$(contar_tiendas)" == "2" ]] && ok "--recrear sin confirmación no borra nada" || fallo "--recrear borró datos sin confirmación"

paso "6. Imagen: usuario sin privilegios, sin SDK y sin secretos"
imagen="blackhellheim-api:local"
[[ "$(docker image inspect -f '{{.Config.User}}' "$imagen")" =~ ^(app|1654)$ ]] && ok "la imagen corre como app" || fallo "la imagen no corre como usuario sin privilegios"
[[ -z "$(docker run --rm --entrypoint dotnet "$imagen" --list-sdks 2>/dev/null)" ]] && ok "la imagen no contiene SDK" || fallo "la imagen contiene un SDK"
contenido="$(docker run --rm --entrypoint ls "$imagen" -a /app)"
! grep -qE '^(\.env|obj|appsettings\.Development\.json)$' <<<"$contenido" && ok "sin .env, obj ni appsettings.Development.json" || fallo "la imagen contiene archivos que no debe"
{ docker history --no-trunc "$imagen"; docker image inspect "$imagen"; } | sin_secretos && ok "historial e inspección sin secretos" || fallo "el historial o la inspección de la imagen contienen secretos"

paso "7. RF-020: el contenedor corre en Production, sin endpoints de prueba"
[[ "$(compose exec -T api printenv ASPNETCORE_ENVIRONMENT | tr -d '\r')" == "Production" ]] && ok "ASPNETCORE_ENVIRONMENT=Production" || fallo "el contenedor no corre en Production"
[[ "$(codigo_http "$base/pruebas/error")" == "404" ]] && ok "GET /pruebas/error 404" || fallo "GET /pruebas/error existe en el contenedor"
curl -s --max-time 5 "$base/openapi/v1.json" 2>/dev/null | grep -q '/pruebas' && fallo "el documento publica rutas de prueba" || ok "el documento no contiene rutas de prueba"

paso "8. Documentación pública en el contenedor"
if [[ "$(codigo_http "$base/openapi/v1.json")" == "404" ]]; then
  omitido "la historia 004 no está implementada: se omite el chequeo de /scalar y /openapi/v1.json"
else
  [[ "$(curl -sL -o /dev/null -w '%{http_code}' --max-time 5 "$base/scalar")" == "200" ]] && ok "GET /scalar 200" || fallo "GET /scalar no es 200"
  [[ "$(codigo_http "$base/openapi/v1.json")" == "200" ]] && ok "GET /openapi/v1.json 200" || fallo "GET /openapi/v1.json no es 200"
fi

paso "9. Los registros no contienen secretos"
compose logs --no-color 2>&1 | sin_secretos && ok "docker compose logs sin contraseñas ni cadenas completas" || fallo "los registros contienen secretos"

if $con_reproducibilidad; then
  paso "10. Reproducibilidad: dos construcciones sin caché dan el mismo artefacto"
  temporal="$(mktemp -d)"
  trap 'rm -rf "$temporal"; docker rmi -f bhh-repro-a bhh-repro-b >/dev/null 2>&1 || true' EXIT
  for sufijo in a b; do
    docker build --no-cache --target final -t "bhh-repro-$sufijo" . >/dev/null 2>&1 || { fallo "falló la construcción $sufijo"; continue; }
    id="$(docker create "bhh-repro-$sufijo")"
    mkdir "$temporal/$sufijo"
    docker cp "$id:/app/." "$temporal/$sufijo" >/dev/null
    docker rm "$id" >/dev/null
    (cd "$temporal/$sufijo" && find . -type f -print0 | sort -z | xargs -0 sha256sum) >"$temporal/$sufijo.sha"
  done
  if [[ -s "$temporal/a.sha" ]] && cmp -s "$temporal/a.sha" "$temporal/b.sha"; then
    ok "los archivos de /app son idénticos ($(wc -l <"$temporal/a.sha") archivos)"
  else
    fallo "las dos construcciones difieren: $(diff "$temporal/a.sha" "$temporal/b.sha" | head -5 | tr '\n' ' ')"
  fi
fi

echo
echo "Resumen: $fallos fallo(s), $omitidos omitido(s)."
[[ $fallos -eq 0 ]]
