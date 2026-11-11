# Tareas: Imagen de contenedor del API y entorno local con un solo comando

**Entrada**: `specs/005-dockerfile-compose/` (spec.md, plan.md, investigacion.md, modelo-datos.md, contratos/entorno.md, inicio-rapido.md)

**Prerrequisitos**: plan.md y spec.md. **Las historias 002b y 003 deben estar implementadas** (hoy no existen `compose.yaml`, `.env.example`, `scripts/base-datos.sh`, `ContextoBlackHellHeim`, `FabricaContextoDiseno` ni `/salud/base`).

**Pruebas**: obligatorias y primero (constitución III). Cada prueba se escribe y se ve fallar antes de la implementación que la hace pasar. Nada se difiere.

**Formato**: `- [ ] T### [P?] [US?] [ETIQUETA] Descripción con ruta`. Etiquetas: `[BE]` código/pruebas del API, `[DB]` base de datos, `[CI]` automatización/contenedores, `[MANUAL]` solo la desarrolladora. `[P]` = paralelizable.

## Fase 1: Preparación

- [ ] T001 [BE] Verificar prerrequisitos: confirmar que existen `compose.yaml`, `.env.example`, `scripts/base-datos.sh`, `src/Infraestructura/Persistencia/FabricaContextoDiseno.cs` y la ruta `/salud/base` en `src/Api/`; si falta alguno, DETENER y avisar (no reimplementar la HU3).
- [ ] T002 [CI] Crear `global.json` en la raíz fijando el SDK `10.0.402` con `rollForward: latestPatch`.
- [ ] T003 [CI] Crear `Directory.Build.props` en la raíz con `RestorePackagesWithLockFile=true`, `Deterministic=true` y `ContinuousIntegrationBuild=true`.
- [ ] T004 [CI] Generar los `packages.lock.json` con `dotnet restore` en `src/Dominio`, `src/Aplicacion`, `src/Infraestructura`, `src/Api` y `src/Pruebas`, y confirmar que `dotnet build BlackHellHeim.sln` sigue sin advertencias nuevas.
- [ ] T005 [CI] Crear `.dockerignore` en la raíz excluyendo `.git`, `.env`, `**/bin`, `**/obj`, `specs/`, `docs/`, `.claude/`, `.specify/`.
- [ ] T006 [CI] Verificar con `docker pull` los digests de `mcr.microsoft.com/dotnet/sdk:10.0`, `aspnet:10.0`, `runtime-deps:10.0` y `mcr.microsoft.com/mssql/server:2022-latest`, y anotar los digests vigentes (incluido `runtime-deps`) en `specs/005-dockerfile-compose/investigacion.md` D1.

## Fase 2: Base común de pruebas (bloquea las historias)

- [ ] T007 [BE] Crear `src/Pruebas/Entorno/RaizRepositorio.cs` que localice la raíz del repositorio (busca `BlackHellHeim.sln`) y exponga lectura de archivos versionados.
- [ ] T008 [P] [BE] Crear `src/Pruebas/Entorno/ArchivosVersionados.cs` que liste con `git ls-files` los archivos versionados de texto, para reutilizar en las pruebas de secretos.

## Fase 3: Historia 1 — API y base sanos con un solo comando (P1)

**Meta**: un único comando levanta `sqlserver`, `migraciones` y `api`, todos sanos.

**Prueba independiente**: `./scripts/entorno.sh` en un entorno limpio termina en 0 y `docker compose ps` muestra `sqlserver` y `api` en `healthy`.

### Pruebas primero

- [ ] T009 [P] [US1] [BE] Escribir `src/Pruebas/Entorno/ComposePruebas.cs`: `compose.yaml` define `sqlserver`, `migraciones` y `api`; `api` depende de `sqlserver` con `service_healthy` y de `migraciones` con `service_completed_successfully`; `sqlserver` y `api` tienen `healthcheck`; `migraciones` tiene `restart: "no"` (éxito y error). Debe fallar.
- [ ] T010 [P] [US1] [BE] Escribir `src/Pruebas/Entorno/DockerfilePruebas.cs` con las pruebas de existencia y estructura: etapas `restauracion`, `compilacion`, `migraciones` y `final`; `final` no usa la imagen SDK ni `COPY . .`. Debe fallar.
- [ ] T011 [P] [US1] [CI] Escribir el esqueleto de `scripts/verificar-entorno.sh` con el paso 1 (`up --build --wait`, servicios en estado esperado y `GET /salud` y `GET /salud/base` en 200) y salida distinta de cero ante fallo. Debe fallar.

### Implementación

- [ ] T012 [US1] [CI] Crear `docker/sonda-http.sh` (bash, `/dev/tcp`, `GET <ruta>` a `127.0.0.1:8080`, sale 0 solo con 200, 1 en cualquier otro caso o tiempo agotado de 3 s) con bits de ejecución.
- [ ] T013 [US1] [CI] Crear `Dockerfile` con la etapa `restauracion` (`FROM` por digest del SDK, copia de `global.json`, `Directory.Build.props`, `.sln`, `.csproj` y `packages.lock.json`, y `dotnet restore --locked-mode`).
- [ ] T014 [US1] [CI] Agregar al `Dockerfile` la etapa `compilacion`: copia del código, `dotnet publish src/Api -c Release --no-restore -p:UseAppHost=false -o /app/publicar` y eliminación de `appsettings.Development.json` de la publicación.
- [ ] T015 [US1] [CI] Agregar al `Dockerfile` la generación del *bundle* con `dotnet ef migrations bundle --self-contained -r linux-${arch}` (mapeo desde `TARGETARCH`, `dotnet-ef` 10.0.15 por `.config/dotnet-tools.json` de la HU3) en `/app/migraciones/efbundle`.
- [ ] T016 [US1] [CI] Agregar al `Dockerfile` la etapa `migraciones` sobre `runtime-deps:10.0` por digest, con `USER app` y *entrypoint* en shell que invoque `./efbundle --connection "$ConnectionStrings__BlackHellHeim"` sin imprimir la cadena.
- [ ] T017 [US1] [CI] Agregar al `Dockerfile` la etapa `final` sobre `aspnet:10.0` por digest: copia solo `/app/publicar` y `docker/sonda-http.sh` (`--chmod=555`), `USER app`, `EXPOSE 8080`, `ENV ASPNETCORE_ENVIRONMENT=Production` y etiquetas OCI (`source`, `version`, `revision`, `licenses`) con `ARG VERSION` y `ARG REVISION`.
- [ ] T018 [US1] [DB] Modificar `compose.yaml`: fijar la imagen de `sqlserver` por digest y agregar su `healthcheck` (`/opt/mssql-tools18/bin/sqlcmd -C -Q "SELECT 1"` con `SQLCMDPASSWORD`, intervalo 10 s, `timeout` 5 s, 12 reintentos, `start_period` 20 s).
- [ ] T019 [US1] [CI] Agregar a `compose.yaml` el servicio `migraciones` (`build.target: migraciones`, `restart: "no"`, `depends_on.sqlserver: service_healthy`, cadena con host `sqlserver` y `${BHH_SQL_SA_PASSWORD:?Defina BHH_SQL_SA_PASSWORD en .env}`).
- [ ] T020 [US1] [CI] Agregar a `compose.yaml` el servicio `api` (`build.target: final`, imagen `blackhellheim-api:local`, `depends_on` según T009, puerto `${BHH_API_PUERTO:-5068}:8080`, `healthcheck` con `docker/sonda-http.sh /salud/base`, intervalo 10 s, 6 reintentos, `start_period` 20 s, `restart: unless-stopped`).
- [ ] T021 [US1] [CI] Agregar `BHH_API_PUERTO` con valor por defecto como marcador en `.env.example`, sin valores reales.
- [ ] T022 [US1] [CI] Crear `scripts/entorno.sh`: valida Docker y Compose v2, `.env` existente sin marcadores, puertos 1433 y `BHH_API_PUERTO` libres, ejecuta `docker compose up --build --wait --remove-orphans`, informa servicios sanos y `http://localhost:${BHH_API_PUERTO:-5068}`; ante fallo muestra `docker compose ps` y registros filtrados (sin cadenas de conexión) y sale distinto de cero.
- [ ] T023 [US1] [MANUAL] Crear `.env` local a partir de `.env.example` con una contraseña propia (≥ 8 caracteres, sin `;`, `=` ni comillas).
- [ ] T024 [US1] [CI] Ejecutar `./scripts/entorno.sh` en limpio y verificar T009, T010 y T011 en verde y que repetir el comando termina sin error con `SELECT COUNT(*) FROM dbo.Tiendas` = 2.

**Punto de control**: US1 funciona por sí sola.

## Fase 4: Historia 2 — El API espera a la base antes de declararse sana (P2)

**Meta**: el API no es sano sin base y se recupera solo.

**Prueba independiente**: con `docker compose stop sqlserver`, `/salud/base` da 503, `/salud` da 200 y `api` queda `unhealthy`; al arrancar la base vuelve a `healthy` sin reiniciar.

### Pruebas primero

- [ ] T025 [P] [US2] [CI] Agregar a `scripts/verificar-entorno.sh` el paso 3: detener `sqlserver`, comprobar `/salud/base` 503 y `/salud` 200, `api` `unhealthy`, reanudar y comprobar `healthy` sin reinicio del contenedor `api`. Debe fallar si la sonda no usa `/salud/base`.
- [ ] T026 [P] [US2] [BE] Agregar a `src/Pruebas/Entorno/ComposePruebas.cs` la prueba de que el `healthcheck` del servicio `api` usa `/salud/base` y que `start_period` + reintentos × intervalo acota la espera a ≤ 120 s (error y validación).
- [ ] T027 [P] [US2] [CI] Agregar a `scripts/verificar-entorno.sh` el paso de esquema al día: comparar `__EFMigrationsHistory` con las migraciones del *bundle* (`efbundle --help`/lista) y fallar si difieren.

### Implementación

- [ ] T028 [US2] [CI] Ajustar los parámetros de `healthcheck` en `compose.yaml` si T025 o T026 lo exigen, y documentar la limitación de RF-009 en `specs/005-dockerfile-compose/investigacion.md` D6 si cambia.
- [ ] T029 [US2] [CI] Ejecutar `./scripts/verificar-entorno.sh` y confirmar T025, T026 y T027 en verde.

## Fase 5: Historia 3 — Credenciales solo por variables de entorno (P3)

**Meta**: cero secretos versionados; variable faltante falla con mensaje claro.

**Prueba independiente**: `git grep` de contraseñas sin coincidencias, `docker compose config` sin `BHH_SQL_SA_PASSWORD` falla nombrándola.

### Pruebas primero

- [ ] T030 [P] [US3] [BE] Escribir `src/Pruebas/Entorno/ConfiguracionSinSecretosPruebas.cs`: `.env.example` solo contiene marcadores `__DEFINIR_CLAVE_LOCAL__` y las variables esperadas; `.env` está en `.gitignore` y `.dockerignore`; no hay `.env` versionado (éxito y error).
- [ ] T031 [P] [US3] [BE] Agregar a `ConfiguracionSinSecretosPruebas.cs` la búsqueda en `git ls-files` de contraseñas, cadenas de conexión con `Password=` distintas del marcador y claves; cero coincidencias (validación).
- [ ] T032 [P] [US3] [BE] Agregar a `ConfiguracionSinSecretosPruebas.cs` que `compose.yaml` usa `${BHH_SQL_SA_PASSWORD:?` y no contiene valores literales de contraseña.
- [ ] T033 [P] [US3] [CI] Agregar a `scripts/verificar-entorno.sh` los pasos 5 y 8: variable obligatoria ausente falla nombrándola sin valor; `docker compose logs` y la salida del comando no contienen la contraseña ni la cadena completa.

### Implementación

- [ ] T034 [US3] [CI] Completar `.gitignore` y `.dockerignore` si T030 lo exige, y revisar `.env.example` para que cumpla T030.
- [ ] T035 [US3] [CI] Verificar que `scripts/entorno.sh` y los mensajes de error nunca imprimen valores (usar solo nombres de variables).
- [ ] T036 [US3] [CI] Ejecutar `dotnet test` filtrando `ConfiguracionSinSecretosPruebas` y `./scripts/verificar-entorno.sh` (pasos 5 y 8) en verde.

## Fase 6: Historia 4 — Imagen reproducible y mismo artefacto (P4)

**Meta**: dos construcciones iguales en lo funcional; imagen sin secretos, SDK ni usuario root.

**Prueba independiente**: dos `docker build --no-cache` producen los mismos hashes de `/app`; `Config.User` = `app`.

### Pruebas primero

- [ ] T037 [P] [US4] [BE] Agregar a `src/Pruebas/Entorno/DockerfilePruebas.cs`: todas las líneas `FROM` externas usan `@sha256:`; `restore` con `--locked-mode`; la última `USER` no es `root` (acepta `app` o `1654`) (éxito y error).
- [ ] T038 [P] [US4] [CI] Agregar a `scripts/verificar-entorno.sh` el paso 6: `Config.User` de la imagen, ausencia de `dotnet --list-sdks`, de `.env`, `appsettings.Development.json` y `obj/`, y que la contraseña no aparece en `docker history` ni `docker image inspect`.
- [ ] T039 [P] [US4] [CI] Agregar a `scripts/verificar-entorno.sh` el paso 7: dos construcciones `--no-cache`, extracción de `/app` con `docker create`/`docker cp` y comparación de SHA-256 de los archivos y versiones de `.deps.json`.

### Implementación

- [ ] T040 [US4] [CI] Ajustar el `Dockerfile` según T037, T038 y T039 (digests, `--locked-mode`, `USER`, limpieza, versión inyectada sin fecha de compilación).
- [ ] T041 [US4] [CI] Ejecutar `./scripts/verificar-entorno.sh` completo y confirmar T038 y T039 en verde.
- [ ] T042 [US4] [MANUAL] Probar la construcción en una máquina `linux/arm64` si dispones de una; si no, dejar constancia en `specs/005-dockerfile-compose/investigacion.md` de que arm64 no se verificó.

## Fase 7: Pulido y verificación final

- [ ] T043 [P] [CI] Agregar a `scripts/entorno.sh` la opción `--recrear` con confirmación explícita antes de `docker compose down -v` (RF-017) y una prueba en `scripts/verificar-entorno.sh` que confirme que sin confirmación no borra.
- [ ] T044 [P] [CI] Comprobar manejo de errores en `scripts/entorno.sh`: Docker ausente, puerto ocupado e imagen no descargable (con red desactivada) producen mensajes claros y no dejan recursos a medias.
- [ ] T045 [P] [BE] Confirmar que `src/Pruebas/Arquitectura` sigue esperando cinco proyectos y que `/salud` y `/salud/base` conservan su contrato sin cambios.
- [ ] T046 [MANUAL] Actualizar `docs/scrum/backlog.md` (fila de la HU5: Spec = `specs/005-dockerfile-compose`, Estado = Hecha) tras aprobar la verificación final, y actualizar `**Estado**` en `spec.md`.
- [ ] T047 [BE] Verificación final: `dotnet build BlackHellHeim.sln` sin advertencias nuevas, `dotnet test BlackHellHeim.sln` en verde, `docker compose up --build --wait` con `api` y `sqlserver` sanos y `./scripts/verificar-entorno.sh` en verde.

## Dependencias

- Fase 1 → Fase 2 → Fase 3 (US1) → Fases 4 a 6 (cada una necesita el Compose y el Dockerfile de US1) → Fase 7.
- US2, US3 y US4 son independientes entre sí tras US1 (archivos distintos salvo `compose.yaml`, `Dockerfile` y `verificar-entorno.sh`, que se editan en serie).
- Externas: la spec 003 (Compose, `.env.example`, `/salud/base`, `dotnet-ef`) y la 002b deben estar implementadas.

## Ejecución en paralelo (ejemplos)

- Fase 3, pruebas: T009, T010, T011 juntas.
- Fase 5, pruebas: T030, T031, T032, T033 juntas.
- Fase 6, pruebas: T037, T038, T039 juntas.

## Estrategia

MVP = US1 (Fases 1 a 3). Luego US2, US3 y US4 en orden de prioridad; los pasos de `verificar-entorno.sh` se agregan por historia para que cada incremento se pueda probar solo. Si T001 falla, no se implementa nada.
