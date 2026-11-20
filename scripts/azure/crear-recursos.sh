#!/usr/bin/env bash
# Crea (o completa) los recursos de Azure de BlackHellHeim. Idempotente; no ejecutar sin revisar.
# Requiere sesión `az login`. La contraseña de SQL se genera al crear el servidor y se imprime
# UNA sola vez en esta terminal: guárdala en tu gestor local. Nunca se escribe en archivos.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"
source ./variables.sh
requerir_az

for p in Microsoft.App Microsoft.OperationalInsights Microsoft.Sql Microsoft.Storage; do az provider register -n "$p" --wait; done

az group create -n "$BHH_RG" -l "$BHH_REGION" -o none

az containerapp env show -n "$BHH_ENTORNO" -g "$BHH_RG" -o none 2>/dev/null \
  || az containerapp env create -n "$BHH_ENTORNO" -g "$BHH_RG" -l "$BHH_REGION" -o none

if ! az sql server show -n "$BHH_SQL_SERVIDOR" -g "$BHH_RG" -o none 2>/dev/null; then
  # Sufijo "Aa1" garantiza mayúscula, minúscula y dígito (requisito de complejidad de SQL).
  clave="$(openssl rand -base64 24 | tr -d '/+=')Aa1"
  az sql server create -n "$BHH_SQL_SERVIDOR" -g "$BHH_RG" -l "$BHH_REGION" \
    --admin-user "$BHH_SQL_ADMIN" --admin-password "$clave" -o none
  echo "=== GUARDA ESTA CLAVE DE SQL (no se vuelve a mostrar): $clave ===" >&2
  unset clave
fi
# 0.0.0.0 permite el acceso desde servicios de Azure (las IP de salida de Container Apps varían).
az sql server firewall-rule create -n AllowAzureServices -g "$BHH_RG" -s "$BHH_SQL_SERVIDOR" \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none

# Oferta gratuita: se pausa al agotar la cuota mensual (sin cargos). Para no pausar, exportar
# BHH_SQL_AGOTADO=BillOverUsage (genera cargos).
az sql db show -n "$BHH_SQL_DB" -s "$BHH_SQL_SERVIDOR" -g "$BHH_RG" -o none 2>/dev/null \
  || az sql db create -n "$BHH_SQL_DB" -s "$BHH_SQL_SERVIDOR" -g "$BHH_RG" \
       --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless \
       --use-free-limit true --free-limit-exhaustion-behavior "${BHH_SQL_AGOTADO:-AutoPause}" -o none

# Almacenamiento del arte (HU-17). El acceso público de lectura a blobs es intencional: las imágenes se muestran por URL.
az storage account show -n "$BHH_STORAGE" -g "$BHH_RG" -o none 2>/dev/null \
  || az storage account create -n "$BHH_STORAGE" -g "$BHH_RG" -l "$BHH_REGION" \
       --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 --https-only true \
       --allow-blob-public-access true --allow-shared-key-access true -o none
# create es idempotente (si ya existe no falla). Modo key: el Owner no tiene rol de datos por defecto.
az storage container create -n "$BHH_CONTENEDOR_ARTE" --account-name "$BHH_STORAGE" \
  --auth-mode key --public-access blob -o none

# La imagen de arranque escucha en el 80; el primer despliegue la reemplaza y mueve el puerto a 8080.
az containerapp show -n "$BHH_APP_API" -g "$BHH_RG" -o none 2>/dev/null \
  || az containerapp create -n "$BHH_APP_API" -g "$BHH_RG" --environment "$BHH_ENTORNO" \
       --image "$BHH_IMAGEN_ARRANQUE" --ingress external --target-port 80 \
       --revisions-mode multiple --cpu "$BHH_CPU" --memory "$BHH_MEMORIA" \
       --min-replicas 0 --max-replicas 1 \
       --env-vars ASPNETCORE_FORWARDEDHEADERS_ENABLED=true -o none

az containerapp job show -n "$BHH_JOB_MIGRACIONES" -g "$BHH_RG" -o none 2>/dev/null \
  || az containerapp job create -n "$BHH_JOB_MIGRACIONES" -g "$BHH_RG" --environment "$BHH_ENTORNO" \
       --trigger-type Manual --replica-timeout 600 --replica-retry-limit 0 \
       --replica-completion-count 1 --parallelism 1 \
       --image "$BHH_IMAGEN_ARRANQUE" --cpu "$BHH_CPU" --memory "$BHH_MEMORIA" -o none

echo "Listo. URL del API: https://$(az containerapp show -n "$BHH_APP_API" -g "$BHH_RG" --query properties.configuration.ingress.fqdn -o tsv)"
echo "Cuenta de almacenamiento: $BHH_STORAGE · Contenedor: $BHH_CONTENEDOR_ARTE"
echo "Servidor SQL: $BHH_SQL_SERVIDOR.database.windows.net · Base: $BHH_SQL_DB"
