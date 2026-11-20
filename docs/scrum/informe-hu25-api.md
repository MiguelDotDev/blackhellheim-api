# Informe HU-25 (parte API, tareas automáticas)

Rama `sprint/03`. Al redactar este informe no se ejecutó nada; el 13 nov 2026 se ejecutaron después las tareas [MANUAL] T003, T007, T009 y T014 (región final `westus3`, API desplegado, `/salud` responde; ver runbook). HU-25 **no** está en «Hecha» en el backlog (faltan tareas [MANUAL] y la parte web).

## Lectura tomada
Solo `specs/025-despliegue-azure`, parte API: scripts `az` (no ejecutados), flujos, pruebas y runbook.

## Commits
| Hash | Contenido |
|---|---|
| `515df0f` | `scripts/azure/*`, `despliegue.yml`, `revertir.yml`, `DespliegueAzurePruebas`, `CabecerasReenviadasPruebas` |
| `1ef21b9` | `docs/despliegue-azure-runbook.md`, `tasks.md` actualizado |
| (este informe) | `docs/scrum/informe-hu25-api.md` |

## Verificación local
`dotnet build` 0 errores (1 advertencia xUnit1013 previa, no de esta HU) · `dotnet test` **215 en verde** · `docker build --target final` y `--target migraciones` correctos · `bash -n` en los 6 scripts.
**No** se pudo: `shellcheck`, `actionlint` ni `az` (no instalados), ni validar los comandos `az` contra Azure real.

## Decisiones y desviaciones del plan
- Imágenes con la CLI `docker` (login/build/push) en vez de `docker/*-action`: menos acciones que fijar; `azure/login` v3.1.0 fijada a SHA.
- `github.token` en lugar de `secrets.GITHUB_TOKEN` (la prueba existente solo admite `SONAR_TOKEN` en `secrets.`).
- T012: bastó `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`; sin cambios en `Program.cs`.
- Variable del administrador: `Administrador__Contrasena` (la spec decía `Administrador__*`; el código lee `Correo` y `Contrasena`).
- Región por defecto `southcentralus` (no verificada; el runbook da el comando de comprobación).
- SQL: oferta gratuita con `AutoPause`; costo total estimado ≈ 8–10 USD/mes (3–8 con cuota gratuita). Las **tarifas por segundo no se pudieron leer** de la página oficial: confirmar en la calculadora.
- Parámetros verificados en la documentación oficial: `az containerapp job create/update/start/execution`, `az containerapp create`, `az sql db create --use-free-limit`. **No verificados** (de memoria): `ingress traffic set/show`, `revision show/list/deactivate`, `job secret set`, `env create`, `ad app federated-credential`, y que la URL FQDN de revisión (`properties.fqdn`) responda con 0 % de tráfico.

## Pendientes [MANUAL]
T003 cuenta/`az login`/región · T007 ejecutar `crear-recursos.sh` y hacer públicos los paquetes GHCR · T009 `configurar-oidc.sh`, entorno `produccion` y variables `AZURE_*` · T014 `configurar-secretos.sh` · T016 ensayo de fallo y rollback · T019 backlog (HU-25 queda «Tareas listas», falta parte web) · `shellcheck`. Orden completo en `docs/despliegue-azure-runbook.md`.

## Push (lo haces tú)
```bash
git push origin sprint/03
```
