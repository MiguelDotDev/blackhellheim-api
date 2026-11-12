# Especificación 025: Primer despliegue continuo del API a Azure

**Historia**: HU-25 (Sprint 3) · **Repos**: api + web (**este documento cubre solo la parte API**) · **Estado**: Plan cerrado · **Creada**: 2026-11-11

**Entrada**: «Quiero un primer despliegue continuo a Azure con la dirección por defecto, incluyendo el proxy de /api. Criterio: un push a la rama principal publica API y web como contenedores; la web carga por HTTPS y /api responde a través de ella.»

**Bloqueado por**: HU-5 (Dockerfile, hecha) y HU-6 (CI, por auditar). **Parte faltante en `blackhellheim-web`**: su Dockerfile/servidor estático, el proxy `/api` hacia la API y su flujo de despliegue; este repo solo define el contrato que la web debe cumplir (sección «Contrato con la web»). La HU-13 es recomendable pero no obligatoria (el despliegue solo exige `/salud`).

## Escenarios de uso y pruebas

### HU 1 — Aprovisionamiento inicial reproducible (P1)
Como desarrolladora quiero crear una sola vez los recursos de Azure con comandos `az` versionados (sin Terraform) para poder repetir o destruir el entorno.
1. **Dada** una suscripción vacía y sesión `az` iniciada `[MANUAL]`, **cuando** ejecuto `scripts/azure/crear-recursos.sh`, **entonces** se crean grupo de recursos, entorno de Container Apps, Azure SQL Database, Container App del API (revisiones múltiples) y job de migraciones.
2. **Dado** que el script ya se ejecutó, **cuando** lo repito, **entonces** no falla ni duplica (idempotente).
3. **Cuando** ejecuto `scripts/azure/destruir-recursos.sh` con confirmación explícita, **entonces** se elimina el grupo de recursos completo.
4. El script no contiene secretos: los lee de variables de entorno o los pide en el momento, y falla con mensaje claro si faltan.

### HU 2 — Autenticación GitHub → Azure sin secretos con OIDC (P1)
1. **Dado** el script `scripts/azure/configurar-oidc.sh`, **cuando** se ejecuta `[MANUAL]`, **entonces** existe un registro de aplicación con una credencial federada limitada al repositorio y al entorno `produccion` (el flujo declara `environment: produccion`, cuyo subject de OIDC es por entorno, no por rama; el entorno se protege para que solo `main` despliegue), con rol mínimo (Contributor solo sobre el grupo de recursos).
2. **Dado** el flujo de despliegue, **cuando** corre, **entonces** inicia sesión con `azure/login` usando `client-id`, `tenant-id` y `subscription-id` (identificadores, no secretos) y el permiso `id-token: write`; no existe ningún secreto de contraseña de Azure.
3. **Dado** un pull request o una rama distinta de `main`, **cuando** corre el flujo, **entonces** no puede iniciar sesión en Azure.

### HU 3 — Despliegue continuo del API (P1)
1. **Dado** un push a `main`, **cuando** pasa la CI, **entonces** se construye la imagen del API (target `final`) y la de migraciones (target `migraciones`), se publican en GHCR etiquetadas con el SHA del commit y se despliega una revisión nueva del API.
2. **Dado** que la revisión nueva no pasa el chequeo de salud, **cuando** termina el flujo, **entonces** el tráfico sigue en la revisión anterior y el flujo falla.
3. **Dado** que dos pushes llegan seguidos, **entonces** los despliegues se serializan (sin cancelar uno en curso).
4. La URL pública del API es la dirección por defecto de Azure (`*.azurecontainerapps.io`) con HTTPS administrado; `GET /salud` responde 200 y no se expone el puerto de SQL.

### HU 4 — Migraciones al desplegar (P1)
1. **Dado** un despliegue, **cuando** hay migraciones pendientes, **entonces** el job `migraciones` (imagen con el `efbundle`) se ejecuta y termina con éxito **antes** de mover tráfico a la revisión nueva.
2. **Dado** que la migración falla, **entonces** el flujo se detiene, la revisión nueva no recibe tráfico y el error queda en el registro.
3. Regla de compatibilidad: toda migración debe ser compatible con la versión anterior del API (expandir y luego contraer), para que el rollback de aplicación sea seguro.
4. La cadena de conexión del job proviene del secreto de producción, nunca del flujo ni de la imagen.

### HU 5 — Secretos de producción (P1)
1. **Dado** el entorno, **entonces** la cadena de conexión, `Jwt__Clave`, `Administrador__*` y otras claves viven como secretos de Container Apps (referenciados por `secretref`) `[MANUAL]` y se cargan con `scripts/azure/configurar-secretos.sh` desde variables de entorno locales.
2. **Entonces** ningún secreto aparece en el repo, los flujos, los registros ni las imágenes (se prueba con una verificación automática de patrones y con las pruebas de `FlujosSinSecretos` existentes).
3. La contraseña administradora de SQL se genera aleatoria al crear y se guarda solo en el gestor local de la desarrolladora.

### HU 6 — Rollback (P2)
1. **Dado** una versión mala ya en producción, **cuando** ejecuto `scripts/azure/revertir.sh [revisión]` o el flujo manual `revertir.yml`, **entonces** el 100 % del tráfico vuelve a la revisión previa en menos de 2 minutos.
2. **Dada** una migración ya aplicada, **entonces** el rollback de aplicación no revierte datos; revertir esquema es un procedimiento aparte documentado (HU-43), nunca automático.
3. Se conservan al menos las 3 últimas revisiones.

### Casos límite
Primer despliegue sin ninguna revisión previa; imagen inexistente en GHCR (paquete privado); migraciones y API desplegados con SHA distintos (se usa el mismo SHA para ambas); límite de cuota gratuita de Azure SQL; cadena de conexión rotada; fallo de `azure/login` por credencial federada mal configurada; zona horaria.

## Requisitos funcionales
- **RF-001** Scripts `az` idempotentes y sin Terraform para crear/destruir recursos.
- **RF-002** OIDC con credenciales federadas por repo+rama/entorno, rol mínimo, sin secretos de Azure.
- **RF-003** Flujo `despliegue.yml` disparado por push a `main` (tras CI verde) y manualmente; permisos mínimos, acciones fijadas a SHA, entorno `produccion`.
- **RF-004** Imágenes API y migraciones en GHCR con etiqueta = SHA.
- **RF-005** Migraciones antes del tráfico; fallo detiene el despliegue.
- **RF-006** Revisión nueva con 0 % de tráfico, verificación de `/salud` y luego 100 %.
- **RF-007** Secretos solo como secretos de Container Apps.
- **RF-008** Rollback por reasignación de tráfico a revisión previa, por script y por flujo manual.
- **RF-009** La API soporta ejecución detrás del proxy (cabeceras reenviadas, HTTPS) y escucha en 8080.
- **RF-010** Documento `inicio-rapido.md` con el orden de los pasos `[MANUAL]`.
- **RF-011** Pruebas automáticas del flujo (como las de HU-6): disparadores, permisos, OIDC, SHA, ausencia de secretos.

## Contrato con la web
La web publicada en el mismo entorno debe: servir por HTTPS, reenviar `/api/*` a la API quitando el prefijo `/api` (`/api/salud` → `/salud`) y conservar `Set-Cookie`, `Authorization` y `X-Forwarded-*`. La API no habilita CORS (mismo origen).

## Criterios de éxito
- **CE-001** Desde cero, los recursos se crean en una sola ejecución del script (<20 min).
- **CE-002** Un push a `main` llega a producción sin pasos manuales en <15 min.
- **CE-003** Rollback efectivo en <2 min.
- **CE-004** Cero secretos en el repo y en registros de Actions.
- **CE-005** Costo mensual dentro de la capa gratuita o del presupuesto anotado en `inicio-rapido.md`.

## Supuestos y fuera de alcance
Dominio propio, WAF, ambientes de prueba/staging, monitoreo avanzado, Key Vault, Terraform/Bicep, Front Door y despliegue de la web (otro repo). Región por defecto `mexicocentral`/la más cercana con disponibilidad, a confirmar en el plan.
