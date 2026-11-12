# Tareas 025: Despliegue continuo a Azure (API)

Etiquetas: [BE] [CI] [MANUAL]. Pruebas primero. La parte web se hace en `blackhellheim-web`.

## Fase 1: Preparación
- [ ] T001 [BE] Verificar que existen `Dockerfile` (targets `migraciones` y `final`), `.github/workflows/integracion-continua.yml` y `/salud`.
- [ ] T002 [CI] Verificar con `gh api` los SHA de `azure/login`, `docker/login-action`, `docker/build-push-action` y `docker/setup-buildx-action`; anotar versión y licencia en la tabla de dependencias de `plan.md`.
- [ ] T003 [MANUAL] Crear/validar la suscripción de Azure, instalar `az` e iniciar sesión (`az login`); elegir la región.

## Fase 2: Aprovisionamiento (HU 1)
- [ ] T004 [BE] Prueba `DespliegueAzurePruebas` sobre `scripts/azure/*.sh`: existen, `set -euo pipefail`, sin contraseñas literales, `destruir` exige confirmación.
- [ ] T005 [CI] `variables.sh` y `crear-recursos.sh` idempotente (grupo, entorno, SQL con contraseña aleatoria, app con `--revisions-mode multiple`, job de migraciones, `--min-replicas 1`, ambos con imagen de arranque pública `mcr.microsoft.com/k8se/quickstart`).
- [ ] T006 [CI] `destruir-recursos.sh` con confirmación.
- [ ] T007 [MANUAL] Ejecutar `crear-recursos.sh`; guardar la contraseña de SQL en el gestor local; hacer público el paquete GHCR tras el primer push.

## Fase 3: OIDC (HU 2)
- [ ] T008 [CI] `configurar-oidc.sh`: app registration, service principal, credencial federada del entorno `produccion` (subject `repo:<propietario>/<repo>:environment:produccion`), rol Contributor en `bhh-prod-rg`.
- [ ] T009 [MANUAL] Ejecutar el script; crear el entorno `produccion` en GitHub restringido a la rama `main` y las variables `AZURE_CLIENT_ID/TENANT_ID/SUBSCRIPTION_ID`.

## Fase 4: Flujo (HU 3, 4)
- [ ] T010 [BE] Pruebas del flujo `despliegue.yml`: disparo en `main` y manual; `id-token: write` solo en el job `desplegar`; permisos mínimos; SHA fijados; `concurrency` sin cancelar; migraciones antes de tráfico; sin `secrets.` de Azure.
- [ ] T011 [CI] Crear `despliegue.yml` según el plan (disparo `workflow_run` + `workflow_dispatch`; no se modifica `integracion-continua.yml`).
- [ ] T012 [BE] Cabeceras reenviadas y HTTPS tras el proxy en `Program.cs` (dueña única de este cambio; HU-13 no lo hace) con prueba de la cookie `Secure` simulando `X-Forwarded-Proto` (RF-009).

## Fase 5: Secretos y rollback (HU 5, 6)
- [ ] T013 [CI] `configurar-secretos.sh` (cadena, `Jwt__Clave`, `Administrador__*`) con `secretref`; falla si falta una variable.
- [ ] T014 [MANUAL] Exportar los valores en la terminal local y ejecutar el script.
- [ ] T015 [BE] Pruebas y creación de `revertir.sh` y `revertir.yml`.
- [ ] T016 [MANUAL] Ensayo: desplegar, romper a propósito la sonda, comprobar que el tráfico no cambia; ejecutar `revertir.yml`.

## Fase 6: Cierre
- [ ] T017 [BE] `inicio-rapido.md` con el orden de los pasos [MANUAL] y el contrato con la web.
- [ ] T018 [CI] `shellcheck`, build y tests en verde.
- [ ] T019 [BE] Actualizar backlog: nota «falta parte web» en la fila HU-25; commit `HU-25: despliegue continuo a Azure (API)`.

**Trazabilidad**: RF-001→T004-6; RF-002→T008; RF-003/004→T010-11; RF-005/006→T010-11,T016; RF-007→T013; RF-008→T015; RF-009→T012; RF-010→T017; RF-011→T004,T010,T015.
