# Plan 025: Despliegue continuo a Azure (API)

**Spec**: [spec.md](spec.md) · **Constitución**: IV, VIII, VII (sin cambios de puertos).

## Resumen
Azure Container Apps (consumo) con imágenes en GHCR; el flujo `despliegue.yml` inicia sesión con OIDC, publica las imágenes del Dockerfile existente, ejecuta el job de migraciones, crea una revisión sin tráfico, valida `/salud` y mueve el 100 % del tráfico. Aprovisionamiento con scripts `az` en `scripts/azure/`.

## Decisiones técnicas
| # | Decisión | Motivo |
|---|---|---|
| D1 | Container Apps en lugar de App Service | Dos contenedores (API y web), HTTPS y URL por defecto, revisiones para rollback. |
| D2 | GHCR (público) en lugar de ACR | Gratis; la imagen no contiene secretos (repo público). `[MANUAL]` hacer público el paquete. |
| D3 | Azure SQL Database (oferta gratuita / serverless) | Coincide con SQL Server local. |
| D4 | OIDC con credencial federada | Sin secretos de Azure; rol Contributor limitado al grupo. |
| D5 | Job `migraciones` con el `efbundle` ya generado por el Dockerfile | Reutiliza HU-5; la conexión llega como secreto. |
| D6 | Revisiones múltiples + etiqueta de revisión | Rollback = reasignar tráfico. |
| D7 | Una sola réplica mínima | Los webhooks de HU-22/29 necesitan disponibilidad; sin escala a cero. |

## Dependencias y licencias
Sin paquetes NuGet nuevos. Herramientas del flujo: `az` CLI (MIT), `azure/login` (MIT), `docker/login-action`, `docker/build-push-action`, `docker/setup-buildx-action` (Apache-2.0), `actions/checkout` (MIT) fijadas a SHA completo (verificar con `gh api` en T002). Servicios de Azure son de pago/gratuitos pero no se agregan al código.

## Estructura
`scripts/azure/{variables.sh,crear-recursos.sh,configurar-oidc.sh,configurar-secretos.sh,revertir.sh,destruir-recursos.sh}`, `.github/workflows/{despliegue.yml,revertir.yml}`, `src/Pruebas/Entorno/DespliegueAzurePruebas.cs`, ajustes en `src/Api/Program.cs` (cabeceras reenviadas) y `specs/025-despliegue-azure/inicio-rapido.md`.

## Orden del despliegue (despliegue.yml)
1. Job `ci`: reutiliza `integracion-continua.yml` con `workflow_call` (o `needs` sobre el mismo SHA).
2. Job `publicar`: login en GHCR con `GITHUB_TOKEN` (`packages: write`), build+push de ambas imágenes con etiqueta SHA.
3. Job `desplegar` (`environment: produccion`, `id-token: write`): `azure/login` (OIDC) → actualizar imagen del job y `az containerapp job start --wait` (falla si exit ≠ 0) → `az containerapp update --revision-suffix sha-<corto>` con tráfico 0 % → sonda `/salud` por la URL de la etiqueta de revisión → `ingress traffic set --revision-weight <nueva>=100` → desactivar revisiones antiguas salvo las 3 últimas.
4. `concurrency: despliegue-produccion` con `cancel-in-progress: false`.

## Rollback
`scripts/azure/revertir.sh` y `revertir.yml` (`workflow_dispatch` con la revisión opcional; por defecto la anterior activa) ejecutan `az containerapp ingress traffic set`. Esquema: no se revierte automáticamente (HU-43).

## Secretos de producción
`configurar-secretos.sh` ejecuta `az containerapp secret set` y `az containerapp update --set-env-vars X=secretref:...` leyendo variables locales; la contraseña de SQL la genera `crear-recursos.sh` con `openssl rand` y la imprime una sola vez en la terminal local (no en archivos).

## Riesgos
Cuota/región de Azure SQL gratuita; GHCR privado por omisión; cookie `Secure` tras el proxy (mitigado por RF-009); costo de réplica mínima.

## Pruebas
xUnit sobre los YAML (como HU-6): disparadores, `id-token: write`, ausencia de `secrets.` de Azure, SHA fijados, `concurrency`, orden migraciones→tráfico. Los scripts pasan `shellcheck` y `bash -n`. Verificación real `[MANUAL]`.
