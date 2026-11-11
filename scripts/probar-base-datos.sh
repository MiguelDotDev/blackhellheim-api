#!/usr/bin/env bash
# Prueba de aceptación de la historia 003: ejecuta el comando único dos veces y comprueba
# que ambas terminan en 0 y que dbo.Tiendas tiene exactamente 2 filas semilla.
# Usa el mismo archivo de configuración que base-datos.sh (BHH_ENV_ARCHIVO, por defecto .env).
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"
archivo_env="${BHH_ENV_ARCHIVO:-.env}"

contar_tiendas() {
  set -a; . "$archivo_env"; set +a
  docker compose exec -T sqlserver sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -C -h -1 -W -S localhost -U sa -d BlackHellHeim -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.Tiendas"' \
    | tr -d '[:space:]'
}

for intento in 1 2; do
  echo "== Ejecución $intento del comando único =="
  ./scripts/base-datos.sh
  filas="$(contar_tiendas)"
  if [[ "$filas" != "2" ]]; then
    echo "ERROR: se esperaban 2 filas en dbo.Tiendas y hay '$filas'." >&2
    exit 1
  fi
  echo "dbo.Tiendas tiene 2 filas."
done
echo "Prueba de aceptación superada."
