#!/usr/bin/env bash
# Crea el registro de aplicación con credencial federada para GitHub Actions (OIDC, sin secretos).
# Rol mínimo: Contributor solo sobre el grupo de recursos. Idempotente. Imprime los 3 identificadores
# (no secretos) que se guardan como variables del repositorio en GitHub.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"
source ./variables.sh
requerir_az

app="bhh-github-despliegue"
id_app="$(az ad app list --display-name "$app" --query '[0].appId' -o tsv)"
[[ -n "$id_app" ]] || id_app="$(az ad app create --display-name "$app" --query appId -o tsv)"
az ad sp show --id "$id_app" -o none 2>/dev/null || az ad sp create --id "$id_app" -o none

nombre_cred="github-${BHH_ENTORNO_GITHUB}"
if [[ -z "$(az ad app federated-credential list --id "$id_app" --query "[?name=='$nombre_cred'].name" -o tsv)" ]]; then
  az ad app federated-credential create --id "$id_app" --parameters "{
    \"name\": \"$nombre_cred\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${BHH_REPO}:environment:${BHH_ENTORNO_GITHUB}\",
    \"audiences\": [\"api://AzureADTokenExchange\"]}" -o none
fi

suscripcion="$(az account show --query id -o tsv)"
az role assignment create --assignee "$id_app" --role Contributor \
  --scope "/subscriptions/$suscripcion/resourceGroups/$BHH_RG" -o none

echo "AZURE_CLIENT_ID=$id_app"
echo "AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)"
echo "AZURE_SUBSCRIPTION_ID=$suscripcion"
