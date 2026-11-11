#!/usr/bin/env bash
# Comando único del entorno local (historia 005): construye la imagen del API, levanta SQL Server,
# aplica las migraciones y deja el API sano. Es idempotente. Nunca imprime secretos.
#   ./scripts/entorno.sh            levanta el entorno
#   ./scripts/entorno.sh --recrear  borra el volumen de la base (destructivo, pide confirmación) y levanta
# La configuración sale de .env (o del archivo indicado en BHH_ENV_ARCHIVO).
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"
archivo_env="${BHH_ENV_ARCHIVO:-.env}"
recrear=false
[[ "${1:-}" == "--recrear" ]] && recrear=true

fallar() { echo "ERROR: $1" >&2; exit 1; }

# --- Requisitos ---------------------------------------------------------------------------
command -v docker >/dev/null 2>&1 || fallar "Docker no está instalado o no está en el PATH."
docker compose version >/dev/null 2>&1 || fallar "Falta el plugin Docker Compose v2 (docker compose)."
docker info >/dev/null 2>&1 || fallar "El demonio de Docker no responde. Inícialo e inténtalo de nuevo."

[[ -f "$archivo_env" ]] || fallar "No existe $archivo_env. Copia .env.example a .env y define la contraseña."
if grep -q '__DEFINIR_CLAVE_LOCAL__' "$archivo_env"; then
  fallar "$archivo_env conserva el marcador __DEFINIR_CLAVE_LOCAL__. Define una contraseña local."
fi
set -a
# shellcheck disable=SC1090
. "$archivo_env"
set +a
[[ -n "${BHH_SQL_SA_PASSWORD:-}" ]] || fallar "Falta la variable BHH_SQL_SA_PASSWORD en $archivo_env."
if [[ "$BHH_SQL_SA_PASSWORD" =~ [\;=\"\'] ]]; then
  fallar "BHH_SQL_SA_PASSWORD no debe contener ';', '=' ni comillas (rompen la cadena de conexión)."
fi
puerto_api="${BHH_API_PUERTO:-5068}"

compose() { docker compose --env-file "$archivo_env" "$@"; }

# Oculta la contraseña y las cadenas de conexión de cualquier salida mostrada al usuario.
filtrar() {
  while IFS= read -r linea; do
    linea="${linea//${BHH_SQL_SA_PASSWORD}/***}"
    printf '%s\n' "$linea" | sed -E 's/(Password=)[^;"[:space:]]*/\1***/Ig'
  done
}

# --- Puertos ------------------------------------------------------------------------------
puerto_ajeno() {
  local puerto="$1" servicio="$2" interno="$3"
  ss -ltn 2>/dev/null | awk '{print $4}' | grep -qE "[:.]${puerto}\$" || return 1
  # Ocupado: es nuestro si el servicio del proyecto ya lo publica.
  compose port "$servicio" "$interno" 2>/dev/null | grep -qE ":${puerto}\$" && return 1
  return 0
}
puerto_ajeno 1433 sqlserver 1433 && fallar "El puerto 1433 está ocupado por otro proceso. Libéralo e inténtalo de nuevo."
puerto_ajeno "$puerto_api" api 8080 && fallar "El puerto $puerto_api está ocupado por otro proceso. Libéralo o cambia BHH_API_PUERTO en $archivo_env."

# --- Recreación explícita y destructiva -----------------------------------------------------
if $recrear; then
  echo "ATENCIÓN: se borrará el volumen de la base de datos local (datos-sqlserver)."
  read -r -p "Escribe RECREAR para confirmar: " respuesta || respuesta=""
  [[ "$respuesta" == "RECREAR" ]] || fallar "Recreación cancelada: no se borró nada."
  compose down -v --remove-orphans
fi

# --- Arranque -----------------------------------------------------------------------------
echo "Construyendo la imagen y levantando el entorno..."
if ! compose up --build --wait --remove-orphans > >(filtrar) 2> >(filtrar >&2); then
  echo "El entorno no quedó sano." >&2
  compose ps 2>&1 | filtrar >&2 || true
  compose logs --tail 20 2>&1 | filtrar >&2 || true
  fallar "Revisa los mensajes anteriores (los secretos están ocultos)."
fi

echo
compose ps --format 'table {{.Service}}\t{{.Status}}'
echo
echo "Entorno listo: SQL Server y API sanos. Migraciones y semillas aplicadas."
echo "API: http://localhost:${puerto_api}  (salud: /salud, base: /salud/base, documentación: /scalar)"
