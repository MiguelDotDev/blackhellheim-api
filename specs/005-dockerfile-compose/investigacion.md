# Investigación: Imagen de contenedor del API y entorno local

Historia 005 · Fecha: 2026-11-11 · No quedan `NEEDS CLARIFICATION`.

Datos de imágenes tomados de las imágenes locales (Ubuntu 24.04, .NET 10.0.13 en runtime,
SDK 10.0.402, usuario `app` UID 1654, `bash` presente, `curl`/`wget` ausentes). Los *digests*
deben revalidarse contra el registro al implementar.

## D1. Imágenes base y fijación

- **Decisión**: SDK `mcr.microsoft.com/dotnet/sdk:10.0@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb`
  y runtime `mcr.microsoft.com/dotnet/aspnet:10.0@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e`,
  más `runtime-deps:10.0@sha256:9cab4d7d886226d47b5489ef9abf0544e2f109fde11a8acc190217610af26c00` para la etapa de migraciones.
  SQL Server: `mcr.microsoft.com/mssql/server:2022-latest@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718`
  (HU3 usa la etiqueta; aquí se fija por digest).
- **Razón**: imágenes oficiales de Microsoft; el digest da referencias inmutables (RF-006).
- **Alternativas**: imagen `chiseled` (más pequeña, sin shell → obliga a otra sonda de salud;
  se deja para el despliegue); Alpine (musl, más riesgo con SQL/ICU); etiquetas sueltas
  (no reproducibles).

## D2. Usuario sin privilegios

- **Decisión**: `USER app` (UID 1654, ya existente en las imágenes de .NET 8+); puerto 8080
  (`ASPNETCORE_HTTP_PORTS=8080`), sin puertos privilegiados.
- **Alternativas**: crear usuario propio (redundante); `USER 1654` numérico (también
  válido; se prefiere `app` por legibilidad, y la prueba acepta ambos).

## D3. Migraciones en el comando único

- **Decisión**: `dotnet ef migrations bundle --self-contained -r linux-x64` (o `linux-arm64`
  según `TARGETARCH`) en la etapa de compilación; etapa `migraciones` sobre `runtime-deps`
  que solo contiene el ejecutable. Servicio Compose de un solo uso, `restart: "no"`.
- **Razón**: deja la base migrada y con semillas en el mismo `compose up` (RF-016), sin SDK
  en el host ni montajes del repositorio (la objeción de la HU3), y es idempotente.
- **Alternativas**: `scripts/base-datos.sh` desde el host (exige SDK y un segundo paso);
  migrar al arrancar la API (`Database.Migrate()` en `Program.cs`: cambia la API, riesgoso con
  varias réplicas y esconde fallos); servicio con imagen SDK y repositorio montado (artefactos
  con permisos del contenedor).
- **Verificar al implementar**: que el *bundle* tome la cadena de la variable de entorno
  (`--connection` desde el *entrypoint*). Contingencia en el plan.

## D4. Sonda de salud sin `curl`

- **Decisión**: `docker/sonda-http.sh` en bash con `/dev/tcp`; envía `GET <ruta>` a
  `127.0.0.1:8080`, lee la línea de estado y sale 0 solo con `200`. Copiada con `--chmod=555`.
- **Razón**: no instalar paquetes (descargas no deterministas, más superficie) y funcionar sin
  red (RF-019).
- **Alternativas**: instalar `curl` con `apt` (no reproducible); un modo `--sonda` dentro del
  API (cambia código y entrada de la aplicación); `wget` (ausente).

## D5. Reproducibilidad

- **Decisión**: bases por digest; `global.json` con SDK 10.0.402 y `rollForward: latestPatch`;
  `RestorePackagesWithLockFile` + `dotnet restore --locked-mode`; `Deterministic` y
  `ContinuousIntegrationBuild` en `Directory.Build.props`; versión inyectada con
  `ARG VERSION`/`REVISION` (`SourceRevisionId`); sin fecha de compilación en la imagen.
- **Alcance**: reproducibilidad **funcional** (mismos `.dll`, mismo `.deps.json`, mismas
  versiones), no bit a bit de capas Docker (marcas de tiempo). Se verifica comparando hashes
  de `/app` entre dos construcciones `--no-cache`.
- **Alternativas**: `SOURCE_DATE_EPOCH`/reproducibilidad de capas (fuera de alcance).

## D6. Salud y límite de RF-009

- **Decisión**: el `healthcheck` del API usa `GET /salud/base` (HU3). "Esquema al día" se
  garantiza por orden (`api` depende de `migraciones` completada con éxito) y se verifica en
  el humo contra `__EFMigrationsHistory`.
- **Limitación**: si alguien aplica o revierte migraciones a mano con la API en marcha,
  `/salud/base` seguirá en 200. Detectarlo exigiría cambiar `/salud/base` o crear un
  endpoint, ambos fuera de alcance (RF-021 y supuestos). Se propone, como mejora futura,
  añadir una comprobación de migraciones pendientes en una historia posterior.
- **Espera acotada**: sin lógica de reintento en el API; el reintento es del
  `healthcheck` y `--wait` termina con error pasado el margen.

## D7. Secretos y configuración

- **Decisión**: `.env` (ignorado) con los nombres de la HU3 (`BHH_SQL_SA_PASSWORD`,
  `ConnectionStrings__BlackHellHeim`). Compose sobrescribe la cadena para sus servicios con
  host `sqlserver` y `${BHH_SQL_SA_PASSWORD:?Defina BHH_SQL_SA_PASSWORD en .env}`; Compose
  imprime el mensaje con el nombre, nunca el valor. `.env` va también en `.dockerignore`.
  Se agrega `BHH_API_PUERTO` (opcional, defecto 5068).
- **Límite conocido**: variables de entorno son visibles con `docker inspect` en la máquina
  local; aceptable para una base desechable (supuesto de la spec). El despliegue usará el
  almacén de secretos de la plataforma (historia posterior).
- **Alternativas**: Docker secrets en archivo (más seguro, pero la HU3 ya definió `.env` y
  `ConnectionStrings__` exige variable); duplicar variables (prohibido por supuesto).

## D8. Entorno de ejecución del contenedor y OpenAPI

- **Decisión**: la imagen no fija `ASPNETCORE_ENVIRONMENT=Development`; usa `Production`
  (valor por defecto) y se declara explícito. `appsettings.Development.json` se excluye de la
  publicación. Los endpoints exclusivos de pruebas no existen. OpenAPI y Scalar (HU4) se
  exponen igual que en cualquier entorno, como decidió la HU4.

## D9. Licencias (Principio IV)

| Componente | Licencia | Veredicto |
|------------|----------|-----------|
| .NET SDK y runtime / imágenes `dotnet/*` | MIT; las imágenes incluyen paquetes de Ubuntu con licencias propias (GPL, etc.) | ✅ Libre, no se redistribuyen |
| `dotnet-ef` y *bundle* | MIT | ✅ |
| SQL Server 2022 Developer | EULA de Microsoft, gratuita para desarrollo y pruebas | ✅ Gratuita; no libre; ya aprobada en HU3; no se publica la imagen |
| Docker Engine, Compose v2 | Apache 2.0 | ✅ |
| Docker Desktop | Comercial para empresas grandes | ⚠️ No requerido; se usa Engine + Compose |
| Bash, `/dev/tcp` | GPL (herramienta del SO, no se enlaza) | ✅ |
| NuGet nuevos | Ninguno | ✅ |

## D10. Alternativas rechazadas globalmente

- Scripts que arrancan el API en el host con Compose solo para la base: no cumple el
  objetivo de "mismo artefacto" (RF-007).
- Compose `profiles` para separar API y base: complica el comando único.

## Actualización de digests (implementación, T006)

Revalidados con `docker pull` el 2026-11-11: SDK, `aspnet` y `runtime-deps` tenían versiones más
nuevas que las del plan; se fijan los digests vigentes de D1 (SDK `fe0fd4e9…`, `aspnet`
`bfd912c8…`, `runtime-deps` `9cab4d7d…`). SQL Server 2022 conserva `cf8f0ab2…`. Actualizar un
digest es una decisión explícita: se vuelve a ejecutar `scripts/verificar-entorno.sh`.

Decisión sobre `global.json`: se fija `version: 10.0.100` con `rollForward: latestFeature` (no
`10.0.402`/`latestPatch` como decía el plan) para que compile tanto con el SDK local de la
desarrolladora (10.0.114) como con el del contenedor; la reproducibilidad la da el digest de la
imagen del SDK.

## Decisiones tomadas al implementar

- **Constructor clásico de Docker**: esta máquina no tiene `buildx`, así que el `Dockerfile` no usa
  funciones exclusivas de BuildKit (`# syntax=`, `COPY --chmod`); los permisos se fijan con `RUN chmod`.
  Funciona igual con BuildKit.
- **Bundle de migraciones**: generado con una variable de conexión ficticia sin credenciales solo en
  ese `RUN` (resuelto R1: funciona y no llega a las imágenes; lo verifica `scripts/probar-imagen.sh`).
  El *bundle* recibe la conexión real en tiempo de ejecución con `--connection`.
- **Archivo de configuración alterno**: los scripts aceptan `BHH_ENV_ARCHIVO` (por defecto `.env`) y
  Compose se invoca con `--env-file`, para poder verificar el entorno sin tocar el `.env` de la
  desarrolladora.
- **Reproducibilidad**: dos construcciones `--no-cache` produjeron 66 archivos idénticos en `/app`
  (SHA-256), incluido `*.deps.json`.
- **Arranque sin red (C5)**: no probado; una reconstrucción con `--build` sin red puede fallar al
  restaurar NuGet si las capas no están en caché. Limitación documentada en `plan.md`.
- **arm64**: no verificado (T047 pendiente de la desarrolladora; el `Dockerfile` mapea `TARGETARCH`).
