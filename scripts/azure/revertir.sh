#!/usr/bin/env bash
# Rollback de aplicación: reasigna el 100 % del tráfico a una revisión anterior. No toca el esquema.
#   revertir.sh [nombre-de-revisión]   (sin argumento: la revisión activa más reciente que no sea la actual)
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"
source ./variables.sh
requerir_az

actual="$(az containerapp ingress traffic show -n "$BHH_APP_API" -g "$BHH_RG" --query "[?weight==\`100\`].revisionName | [0]" -o tsv)"
destino="${1:-}"
if [[ -z "$destino" ]]; then
  destino="$(az containerapp revision list -n "$BHH_APP_API" -g "$BHH_RG" \
    --query "reverse(sort_by([?properties.active], &properties.createdTime))[?name!='$actual'].name | [0]" -o tsv)"
fi
[[ -n "$destino" ]] || fallar "No hay una revisión anterior activa a la que volver."

az containerapp ingress traffic set -n "$BHH_APP_API" -g "$BHH_RG" --revision-weight "$destino=100" -o none
url="https://$(az containerapp show -n "$BHH_APP_API" -g "$BHH_RG" --query properties.configuration.ingress.fqdn -o tsv)"
curl -fsS --retry 5 --retry-delay 5 --max-time 15 "$url/salud" >/dev/null || fallar "Tráfico movido a $destino pero /salud no responde."
echo "Tráfico 100 % en $destino (antes: ${actual:-ninguna}). /salud responde."
