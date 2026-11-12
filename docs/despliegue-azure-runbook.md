# Runbook: primer despliegue completo de BlackHellHeim en Azure (API + web)

Guía en orden, con comandos exactos. **[MANUAL]** = lo haces tú; **[SCRIPT]** = lo ejecuta un script del repo
(pero tú lo lanzas). Nada de esto se ha ejecutado todavía: al seguirla por primera vez, anota cualquier diferencia.
Todos los comandos se corren desde la raíz de `blackhellheim-api`, salvo que se indique.

## 0. Qué se crea y cuánto cuesta

| Recurso | Tamaño elegido (el más barato) | Costo mensual aprox. (USD) |
|---|---|---|
| Container Apps, entorno de consumo | sin perfil dedicado | 0 (sin cargo propio) |
| API `bhh-api` | 0.25 vCPU / 0.5 GiB, 0 a 1 réplicas (escala a cero) | ~4 si está casi siempre ociosa (tarifa *idle*), hasta ~15 con tráfico constante |
| Web `bhh-web` | 0.25 vCPU / 0.5 GiB, 1 réplica | ~4 (misma lógica) |
| Job `bhh-migraciones` | 0.25 vCPU / 0.5 GiB, ~1 min por despliegue | < 0.01 |
| Azure SQL Database | oferta gratuita, serverless, se pausa al agotar la cuota (`AutoPause`) | 0 |
| Cuenta de almacenamiento (Blob, `Standard_LRS`) | arte de las playeras (HU-17), pocos MB | centavos (< 0.10) |
| Log Analytics del entorno | ingesta por defecto | 0 (5 GB/mes gratis) |
| GHCR (imágenes) | paquetes públicos | 0 |
| **Total estimado** | | **≈ 8–10 USD/mes**; con la cuota gratuita de Container Apps (180 000 vCPU-s, 360 000 GiB-s y 2 M peticiones al mes **por suscripción**) puede quedar entre 3 y 8 |

Fuentes: [facturación de Container Apps](https://learn.microsoft.com/azure/container-apps/billing) (cuotas gratuitas y reglas *idle*) y
[oferta gratuita de Azure SQL](https://learn.microsoft.com/azure/azure-sql/database/free-tier) (100 000 vCore-s, 32 GB al mes; sin SLA).
**Las tarifas por segundo no se pudieron leer de la página de precios (se renderiza con JavaScript); las cifras usan las tarifas públicas que conozco
(activo ≈ 0.000024 USD/vCPU-s y 0.000003 USD/GiB-s; *idle* ≈ 0.000003 USD en ambos). Confírmalas en la
[calculadora](https://azure.microsoft.com/pricing/calculator/?service=container-apps) antes de depender de ellas.**

Validado contra la documentación vigente de Microsoft (nov 2026): el plan de consumo permite desde 0.25 vCPU (la CLI acepta 0.25–2.0 y el script usa 0.25 / 0.5 Gi, el mínimo);
la base gratuita se crea con `--edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless --use-free-limit true --free-limit-exhaustion-behavior AutoPause`
(hasta 10 bases gratuitas por suscripción, solo bases nuevas). Para abaratar más, `--min-replicas 0` elimina el costo *idle* a cambio de arranque en frío.

Consecuencia de la base gratuita: se pausa tras 15 min sin uso; la primera petición tarda ~1 min en despertarla (por eso la cadena lleva `Connect Timeout=90`).
Si agotas los 100 000 vCore-s del mes, la base queda inaccesible hasta el mes siguiente. Para evitarlo: `export BHH_SQL_AGOTADO=BillOverUsage` antes del paso 4 (genera cargos).

## 1. Cuenta, presupuesto y herramientas

1. **[MANUAL]** Crea la cuenta en <https://azure.microsoft.com/free> (tarjeta requerida, créditos de bienvenida). Anota el *Subscription ID*.
2. **[MANUAL] Alerta de presupuesto (hazlo antes de crear nada)**: portal → *Cost Management + Billing* → *Budgets* → *Add*:
   alcance = tu suscripción, monto mensual **15 USD**, alertas al 50 %, 80 % y 100 % (real) y 100 % (previsto) a tu correo.
3. **[MANUAL]** Instala Azure CLI (<https://learn.microsoft.com/cli/azure/install-azure-cli>), `openssl`, `curl` y Docker. Comprueba: `az version`.
4. **[MANUAL]** Instala la extensión de Container Apps: `az extension add --name containerapp --upgrade`
   (los comandos `az containerapp …` dependen de esta extensión; los `az sql`, `az ad`, `az group` y `az role` vienen con la CLI base. Validado con Azure CLI 2.90.0 y extensión containerapp 1.3.0b6. `az containerapp revision label` y `ingress traffic set --label-weight` requieren una versión reciente de la extensión.)
5. **[MANUAL]** Inicia sesión y fija la suscripción:
   ```bash
   az login
   az account set --subscription "<SUBSCRIPTION_ID>"
   az account show --query "{nombre:name, id:id}" -o table
   ```
6. **[MANUAL]** Confirma la región (por defecto `southcentralus`, la más cercana a México con Container Apps; cámbiala exportando `BHH_REGION`).
   Debe aparecer en ambas listas; si no, elige otra:
   ```bash
   az provider show -n Microsoft.App --query "resourceTypes[?resourceType=='managedEnvironments'].locations[]" -o tsv
   az provider show -n Microsoft.Sql --query "resourceTypes[?resourceType=='servers'].locations[]" -o tsv
   ```
7. **[MANUAL]** Si tu repo en GitHub no es `MiguelDotDev/blackhellheim-api`: `export BHH_REPO="propietario/repositorio"`.

## 2. Crear los recursos — **[SCRIPT]** `scripts/azure/crear-recursos.sh`

```bash
./scripts/azure/crear-recursos.sh
```
Crea, en este orden: proveedores → grupo `bhh-prod-rg` → entorno `bhh-prod-env` → servidor y base SQL (gratuita) → cuenta de almacenamiento `bhhst<8 hex>` con el contenedor `arte` → Container App `bhh-api`
(modo multi-revisión, imagen de arranque) → job `bhh-migraciones`. Es idempotente: puedes repetirlo. Imprime al final el nombre de la cuenta de almacenamiento.

**Almacenamiento (HU-17):** cuenta `Standard_LRS`/`StorageV2`, TLS 1.2 mínimo, solo HTTPS, con clave compartida habilitada (la cadena de conexión la usa el API).
El contenedor `arte` se crea con acceso público de lectura a blobs (`--public-access blob`): **es intencional**, porque las imágenes se muestran por URL directa.
Solo se pueden leer blobs si se conoce la URL; no se pueden listar ni escribir sin la clave. Costo: centavos al mes.

- **[MANUAL] Guarda la clave de SQL** que imprime entre `===` (se muestra una sola vez). Va a tu gestor de contraseñas, nunca al repo.
- Si la ejecución se interrumpe, vuelve a lanzarla. Si ya se creó el servidor y perdiste la clave, restablécela:
  `az sql server update -n <servidor> -g bhh-prod-rg --admin-password "<nueva>"`.

## 3. Cargar los secretos de producción — **[SCRIPT]** `scripts/azure/configurar-secretos.sh`

**[MANUAL]** Exporta los valores en tu terminal (no los pongas en archivos ni en el historial compartido):
```bash
read -rs BHH_SQL_CLAVE;      export BHH_SQL_CLAVE        # la clave del paso 2
export BHH_JWT_CLAVE="$(openssl rand -base64 48)"        # guárdala también en tu gestor
export BHH_ADMIN_CORREO="tu-correo-admin@ejemplo.com"
read -rs BHH_ADMIN_CLAVE;    export BHH_ADMIN_CLAVE      # contraseña fuerte del administrador inicial
./scripts/azure/configurar-secretos.sh
```
El script falla si falta una variable. Crea los secretos `cadena-conexion`, `jwt-clave`, `admin-correo` y `admin-clave` y los enlaza por `secretref`. Además, el script obtiene la cadena de la cuenta de almacenamiento con `az storage account show-connection-string` (no la imprime ni la guarda), la crea como secreto `almacenamiento` y la enlaza a `AlmacenArchivos__CadenaConexion`; también fija `AlmacenArchivos__Contenedor=arte`.
Al terminar: `unset BHH_SQL_CLAVE BHH_JWT_CLAVE BHH_ADMIN_CORREO BHH_ADMIN_CLAVE`.

## 4. OIDC entre GitHub y Azure — **[SCRIPT]** `scripts/azure/configurar-oidc.sh` + **[MANUAL]** en GitHub

```bash
./scripts/azure/configurar-oidc.sh
```
Imprime `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` y `AZURE_SUBSCRIPTION_ID` (identificadores, **no** secretos). Luego, **[MANUAL]**:

1. GitHub → repo → *Settings → Environments → New environment* → nombre exacto **`produccion`**. En *Deployment branches and tags* elige
   *Selected branches* y añade `main`. (Opcional: *Required reviewers* = tú.)
2. En ese entorno (o en *Settings → Secrets and variables → Actions → Variables*) crea tres **variables** (pestaña *Variables*, no *Secrets*):
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` con los valores impresos.
   (Con GitHub CLI: `gh variable set AZURE_CLIENT_ID --body "<valor>"`, y lo mismo con las otras dos.)
3. Repite los pasos 1–2 en el repo `blackhellheim-web` si su flujo de despliegue usa el mismo patrón. La credencial federada de ese repo
   se crea con `BHH_REPO=<propietario>/blackhellheim-web ./scripts/azure/configurar-oidc.sh` (reusa la misma app de Azure si agregas la credencial; revisa el script del repo web).

## 5. Paquetes de GHCR públicos — **[MANUAL]** (tras el primer push de imágenes)

Los paquetes nacen privados y Container Apps no puede descargarlos sin credenciales. Después de que `despliegue.yml` publique por primera vez
(el job `publicar` termina antes de que `desplegar` falle por la imagen privada, así que puedes lanzarlo ya, paso 6):

GitHub → tu perfil/organización → *Packages* → `blackhellheim-api` → *Package settings* → *Change visibility* → **Public**.
Repite con `blackhellheim-migraciones` (y con los de la web). Las imágenes no contienen secretos (la configuración se inyecta en ejecución).

## 6. Primer despliegue del API

1. Asegúrate de que `main` contiene `despliegue.yml` (llega vía PR `release → main`, con tu aprobación).
2. **Opción A (automático)**: un push a `main` con la CI en verde dispara *Despliegue*.
   **Opción B (primer intento, recomendada)**: GitHub → *Actions → Despliegue → Run workflow* sobre `main`.
3. Si el job *Migrar y desplegar* falla por no poder descargar la imagen: haz públicos los paquetes (paso 5) y relanza con *Re-run failed jobs*.
4. El flujo hace: publicar imágenes (`:<sha>`) → actualizar y ejecutar el job de migraciones (detiene todo si falla) → crear revisión nueva con 0 % de tráfico
   → probar `/salud` en la URL de la etiqueta `nueva` (`bhh-api---nueva.<dominio>`; una revisión con 0 % solo es accesible por etiqueta, según la documentación de Container Apps) → mover 100 % del tráfico → conservar las 3 últimas revisiones.

## 7. Web — **[MANUAL]** (el contenido vive en `blackhellheim-web`)

La web no se despliega desde este repo. Lo que debe cumplir (contrato de `specs/025-despliegue-azure/spec.md`):
servir por HTTPS, reenviar `/api/*` a la API **quitando** el prefijo `/api` (`/api/salud` → `/salud`) y conservar `Set-Cookie`, `Authorization` y `X-Forwarded-*`.
Crea su Container App en el mismo entorno (ejemplo; ajusta imagen y puerto a lo que defina el repo web):
```bash
az containerapp create -n bhh-web -g bhh-prod-rg --environment bhh-prod-env \
  --image ghcr.io/<propietario-en-minusculas>/blackhellheim-web:<sha> \
  --ingress external --target-port 8080 --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1
```
La URL interna del API para el proxy: `http://bhh-api` (nombre de la app dentro del entorno) o su FQDN público. Qué variable lee la web para esto lo define su repo.

## 8. Comprobaciones

```bash
API="https://$(az containerapp show -n bhh-api -g bhh-prod-rg --query properties.configuration.ingress.fqdn -o tsv)"
curl -i "$API/salud"                                     # 200 {"estado":"ok"}
curl -i "$API/salud/base"                          # 200 (la primera vez puede tardar ~1 min: la base despierta)
curl -s -o /dev/null -w "%{http_code}\n" "$API/pruebas/error"   # 404: no existe en Production
az containerapp revision list -n bhh-api -g bhh-prod-rg -o table
az containerapp logs show -n bhh-api -g bhh-prod-rg --tail 50
WEB="https://$(az containerapp show -n bhh-web -g bhh-prod-rg --query properties.configuration.ingress.fqdn -o tsv)"
curl -i "$WEB/api/salud"                                 # 200 a través de la web
```
En el navegador: la web carga con candado HTTPS y las llamadas a `/api/...` responden sin errores de CORS.

## 9. Rollback

- **[SCRIPT]** Local: `./scripts/azure/revertir.sh` (vuelve a la revisión activa anterior) o `./scripts/azure/revertir.sh bhh-api--<revisión>`.
- **[MANUAL]** Desde GitHub: *Actions → Revertir despliegue → Run workflow* (campo `revision` opcional).
- Ver revisiones: `az containerapp revision list -n bhh-api -g bhh-prod-rg --query "[].{n:name,activa:properties.active,trafico:properties.trafficWeight}" -o table`
- El rollback es solo de aplicación: **no revierte migraciones**. Por eso toda migración debe ser compatible con la versión anterior (expandir y luego contraer).
- Una revisión desactivada no se puede elegir; solo se conservan 3 activas. Para reactivar: `az containerapp revision activate -n bhh-api -g bhh-prod-rg --revision <nombre>`.

## 10. Apagar o destruir

`./scripts/azure/destruir-recursos.sh` borra **todo** el grupo (incluida la cuenta de almacenamiento y el arte subido) (pide escribir `bhh-prod-rg`). Úsalo si no vas a usar el entorno: es la forma de no pagar nada.

## 11. Errores típicos

| Síntoma | Causa probable | Solución |
|---|---|---|
| `azure/login` falla: `AADSTS70025` / "no matching federated identity" | El *subject* no coincide: el job debe tener `environment: produccion`, el repo en `BHH_REPO` debe ser el real y la rama `main` | Revisa `az ad app federated-credential list --id <AZURE_CLIENT_ID>`; el subject es `repo:<propietario>/<repo>:environment:produccion` |
| `AuthorizationFailed` al actualizar la app | El rol Contributor no está en el grupo correcto o tardó en propagarse | Espera 2 min; `az role assignment list --assignee <AZURE_CLIENT_ID> -o table` |
| Variables `AZURE_*` vacías | Las creaste como *Secrets* o en otro repo/entorno | Deben ser *Variables* del repo o del entorno `produccion` |
| La revisión no inicia: `ImagePullBackOff` / `UNAUTHORIZED` | El paquete de GHCR sigue privado, o el nombre tiene mayúsculas | Hazlo público (paso 5); el nombre de la imagen va en minúsculas |
| Revisión sana pero 502/503 en la URL pública | Puerto de ingreso ≠ 8080 | `az containerapp ingress update -n bhh-api -g bhh-prod-rg --target-port 8080` (el flujo ya lo hace) |
| `/salud` de la revisión nueva falla y no hay tráfico | Falta un secreto o la base está despertando | `az containerapp logs show -n bhh-api -g bhh-prod-rg --revision <rev> --tail 100`; confirma el paso 3 |
| Migración falla: `Login failed` / timeout | Clave SQL errónea o regla de firewall ausente / base pausada | Repite el paso 3 con la clave correcta; reintenta (la base despierta en ~1 min); `az sql server firewall-rule list -g bhh-prod-rg -s <servidor>` |
| **CORS** en el navegador | Se llama a la URL del API directamente desde otro origen. La API **no** habilita CORS (mismo origen) | Llama siempre a `/api/...` en el dominio de la web; no pongas la URL `*.azurecontainerapps.io` del API en el frontend |
| **Prefijo `/api`**: 404 en `/api/salud` | El proxy no quita `/api`, o la API recibe `/api/salud` | El proxy debe reescribir `/api/x` → `/x` (en nginx: `location /api/ { proxy_pass http://bhh-api/; }` con la barra final) |
| **Cookies**: la sesión no persiste | La cookie es `Secure` + `SameSite=Lax`: la web debe servirse por HTTPS y el proxy debe reenviar `Set-Cookie`, `X-Forwarded-Proto` y `Host` | Comprueba HTTPS en la web; la API ya procesa `X-Forwarded-*` (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`); no cambies el dominio entre la web y el API |
| **Permisos**: `The client ... does not have authorization` al ejecutar scripts | Tu usuario no es Owner/Contributor de la suscripción, o `configurar-oidc.sh` necesita permisos en Entra ID | Usa el usuario propietario de la suscripción; para crear apps de Entra se requiere rol *Application Developer* o superior |
| `MissingSubscriptionRegistration` | Proveedor sin registrar | `az provider register -n Microsoft.App --wait` (el script ya lo hace) |
| Cuota o región no disponible para SQL gratuito | Ya existe otra base gratuita con otra región en la suscripción o la región no la ofrece | Cambia `BHH_REGION` o usa la región de la primera base gratuita |
| Dos despliegues seguidos | Se serializan (`concurrency`), no se cancelan | Espera; no es un error |
