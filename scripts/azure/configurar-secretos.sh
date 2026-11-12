#!/usr/bin/env bash
# Carga los secretos de producción como secretos de Container Apps (secretref). Lee los valores de
# variables de entorno de tu terminal; nunca los imprime ni los guarda. Falla si falta alguno.
#   BHH_SQL_CLAVE  BHH_JWT_CLAVE  BHH_ADMIN_CORREO  BHH_ADMIN_CLAVE
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"
source ./variables.sh
requerir_az

for v in BHH_SQL_CLAVE BHH_JWT_CLAVE BHH_ADMIN_CORREO BHH_ADMIN_CLAVE; do
  [[ -n "${!v:-}" ]] || fallar "Falta la variable de entorno $v."
done

# Connect Timeout alto: la base gratuita se pausa por inactividad y tarda en reanudar.
cadena="Server=tcp:${BHH_SQL_SERVIDOR}.database.windows.net,1433;Database=${BHH_SQL_DB};User ID=${BHH_SQL_ADMIN};Password=${BHH_SQL_CLAVE};Encrypt=True;TrustServerCertificate=False;Connect Timeout=90"

# La cadena de Blob se obtiene aquí mismo: no se imprime ni se guarda.
almacen="$(az storage account show-connection-string -n "$BHH_STORAGE" -g "$BHH_RG" --query connectionString -o tsv)"
[[ -n "$almacen" ]] || fallar "No se pudo obtener la cadena de conexión de $BHH_STORAGE."

az containerapp secret set -n "$BHH_APP_API" -g "$BHH_RG" -o none --secrets \
  "almacenamiento=$almacen" \
  "cadena-conexion=$cadena" "jwt-clave=$BHH_JWT_CLAVE" \
  "admin-correo=$BHH_ADMIN_CORREO" "admin-clave=$BHH_ADMIN_CLAVE"
az containerapp update -n "$BHH_APP_API" -g "$BHH_RG" -o none --set-env-vars \
  ConnectionStrings__BlackHellHeim=secretref:cadena-conexion Jwt__Clave=secretref:jwt-clave \
  Administrador__Correo=secretref:admin-correo Administrador__Contrasena=secretref:admin-clave \
  AlmacenArchivos__CadenaConexion=secretref:almacenamiento "AlmacenArchivos__Contenedor=$BHH_CONTENEDOR_ARTE"

az containerapp job secret set -n "$BHH_JOB_MIGRACIONES" -g "$BHH_RG" -o none --secrets "cadena-conexion=$cadena"
az containerapp job update -n "$BHH_JOB_MIGRACIONES" -g "$BHH_RG" -o none \
  --set-env-vars ConnectionStrings__BlackHellHeim=secretref:cadena-conexion
echo "Secretos cargados."
