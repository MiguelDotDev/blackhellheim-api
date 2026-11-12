#!/usr/bin/env bash
# Nombres y tamaños de los recursos de Azure (HU-25). Se hace `source`; no contiene secretos.
# Cada valor se puede sobrescribir exportando la variable antes de ejecutar los scripts.
set -euo pipefail

: "${BHH_REGION:=southcentralus}"   # región cercana a México; confirmar disponibilidad (ver runbook)
: "${BHH_RG:=bhh-prod-rg}"
: "${BHH_ENTORNO:=bhh-prod-env}"
: "${BHH_APP_API:=bhh-api}"
: "${BHH_JOB_MIGRACIONES:=bhh-migraciones}"
: "${BHH_SQL_DB:=blackhellheim}"
: "${BHH_SQL_ADMIN:=bhhadmin}"
: "${BHH_CONTENEDOR_ARTE:=arte}"   # contenedor Blob del arte (HU-17)
: "${BHH_REPO:=MiguelDotDev/blackhellheim-api}"   # propietario/repositorio de GitHub
: "${BHH_ENTORNO_GITHUB:=produccion}"
# Tamaños mínimos permitidos del plan de consumo (0.25 vCPU / 0.5 GiB).
: "${BHH_CPU:=0.25}"
: "${BHH_MEMORIA:=0.5Gi}"
: "${BHH_IMAGEN_ARRANQUE:=mcr.microsoft.com/k8se/quickstart:latest}"

fallar() { echo "ERROR: $1" >&2; exit 1; }

requerir_az() {
  command -v az >/dev/null 2>&1 || fallar "Falta Azure CLI (az)."
  az account show >/dev/null 2>&1 || fallar "No hay sesión de Azure: ejecuta 'az login'."
}

# El nombre del servidor SQL debe ser único en todo Azure: se deriva de la suscripción.
if [[ -z "${BHH_SQL_SERVIDOR:-}" ]]; then
  BHH_SQL_SERVIDOR="bhh-sql-$(az account show --query id -o tsv 2>/dev/null | sha256sum | cut -c1-8)"
fi

# Nombre de la cuenta de almacenamiento: único en Azure, solo minúsculas y números (3-24).
if [[ -z "${BHH_STORAGE:-}" ]]; then
  BHH_STORAGE="bhhst$(az account show --query id -o tsv 2>/dev/null | sha256sum | cut -c1-8)"
fi
