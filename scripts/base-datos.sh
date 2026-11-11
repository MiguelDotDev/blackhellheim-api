#!/usr/bin/env bash
# Comando único de la base de datos local (historia 003): levanta SQL Server, espera a que
# esté sano y aplica las migraciones pendientes (incluidas las semillas). Es idempotente.
# La configuración sale de .env (o del archivo indicado en BHH_ENV_ARCHIVO); nunca se imprime.
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"
archivo_env="${BHH_ENV_ARCHIVO:-.env}"

fallar() {
  echo "ERROR: $1" >&2
  exit 1
}

[[ -f "$archivo_env" ]] || fallar "No existe $archivo_env. Copia .env.example a .env y define la contraseña."
if grep -q '__DEFINIR_CLAVE_LOCAL__' "$archivo_env"; then
  fallar "$archivo_env conserva el marcador __DEFINIR_CLAVE_LOCAL__. Define una contraseña local."
fi
command -v docker >/dev/null 2>&1 || fallar "Docker no está instalado o no está en el PATH."
docker compose version >/dev/null 2>&1 || fallar "Falta el plugin Docker Compose v2."
command -v dotnet >/dev/null 2>&1 || fallar "Falta el SDK de .NET 10."

set -a
# shellcheck disable=SC1090
. "$archivo_env"
set +a
[[ -n "${BHH_SQL_SA_PASSWORD:-}" ]] || fallar "Falta la variable BHH_SQL_SA_PASSWORD en $archivo_env."
[[ -n "${ConnectionStrings__BlackHellHeim:-}" ]] || fallar "Falta la variable ConnectionStrings__BlackHellHeim en $archivo_env."

echo "Levantando SQL Server y esperando a que esté sano..."
docker compose up -d --wait sqlserver >/dev/null || fallar "SQL Server no quedó sano (revisa: docker compose logs sqlserver)."
echo "Contenedor sqlserver saludable."

dotnet tool restore >/dev/null || fallar "No se pudo restaurar la herramienta dotnet-ef."
dotnet ef database update -p src/Infraestructura -s src/Infraestructura >/dev/null \
  || fallar "Falló la aplicación de migraciones. La base no se borra: revisa el historial antes de decidir."
echo "Migraciones aplicadas en la base BlackHellHeim."
echo "Base de datos lista. Configuración tomada de $archivo_env."
