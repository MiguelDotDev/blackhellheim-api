#!/usr/bin/env bash
# Elimina el grupo de recursos COMPLETO (irreversible). Exige escribir el nombre del grupo.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"
source ./variables.sh
requerir_az
read -r -p "Escribe '$BHH_RG' para confirmar el borrado total: " respuesta
[[ "$respuesta" == "$BHH_RG" ]] || fallar "Confirmación incorrecta. No se borró nada."
az group delete -n "$BHH_RG" --yes --no-wait
echo "Borrado solicitado para $BHH_RG."
