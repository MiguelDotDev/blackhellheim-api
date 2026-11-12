# Tareas: Integración continua del API

**Entrada**: `specs/006-integracion-continua/` (spec.md, plan.md, investigacion.md, modelo-datos.md, contratos/verificaciones-ci.md, inicio-rapido.md)

**Prerrequisitos**: plan.md y spec.md. Las historias 002, 003, 004 y 005 ya están implementadas: existen `global.json`, `Directory.Build.props`, `packages.lock.json` por proyecto, `.config/dotnet-tools.json` (solo `dotnet-ef`), `Dockerfile`, `compose.yaml`, `scripts/` y pruebas con Testcontainers. `.github/workflows/` aún no existe.

**Pruebas**: obligatorias y primero (constitución III). Las reglas de los flujos (disparadores, permisos, SHA fijados, ausencia de secretos) se validan con pruebas xUnit en `src/Pruebas/Entorno/` que leen los YAML con YamlDotNet (ya referenciado); cada prueba se escribe y se ve fallar antes del flujo que la hace pasar. Lo que solo puede comprobarse en GitHub o SonarQube Cloud queda como tarea `[MANUAL]`.

**Formato**: `- [ ] T### [P?] [US?] [ETIQUETA] Descripción con ruta`. Etiquetas: `[BE]` pruebas/código del API, `[CI]` automatización y configuración de flujos, `[MANUAL]` solo la desarrolladora (cuentas, secretos y ajustes de GitHub o SonarQube Cloud). `[P]` = paralelizable.

**Fuera de las tareas**: el `git push` y la apertura de pull requests los hace la desarrolladora al terminar (flujo de `AGENTS.md`); no son tareas de esta lista.

## Fase 1: Preparación

- [X] T001 [BE] Verificar prerrequisitos: existen `global.json`, `Directory.Build.props`, `.config/dotnet-tools.json`, los cinco `packages.lock.json` de `src/*`, `Dockerfile` y `scripts/probar-imagen.sh`, y NO existe `.github/workflows/`; si falta algo, DETENER y avisar.
- [X] T002 [CI] Agregar `coverlet.collector` 10.1.0 (MIT) como `PackageReference` en `src/Pruebas/BlackHellHeim.Pruebas.csproj` (con `IncludeAssets` `runtime; build; native; contentfiles; analyzers; buildtransitive` y `PrivateAssets` `all`) y regenerar `src/Pruebas/packages.lock.json` con `dotnet restore src/Pruebas/BlackHellHeim.Pruebas.csproj --force-evaluate`; confirmar con `git diff --stat` que los otros cuatro `packages.lock.json` no cambiaron.
- [X] T003 [CI] Validar que el lock regenerado es consistente: `dotnet restore BlackHellHeim.sln --locked-mode`, `dotnet build BlackHellHeim.sln --no-restore --configuration Release` y `dotnet test BlackHellHeim.sln --no-build --configuration Release` terminan en 0 (requiere Docker en marcha).
- [X] T004 [CI] Agregar `dotnet-sonarscanner` 12.1.0 al `.config/dotnet-tools.json` existente sin quitar `dotnet-ef` (`"rollForward": false`, como la entrada actual) y comprobar con `dotnet tool restore` que `dotnet sonarscanner --version` y `dotnet ef --version` responden.
- [X] T005 [CI] Revalidar el build del `Dockerfile` con `--locked-mode` tras T002 y T004 (el `Dockerfile` copia `.config/dotnet-tools.json` y los `packages.lock.json` y ejecuta `dotnet restore ... --locked-mode` y `dotnet tool restore`): ejecutar `./scripts/probar-imagen.sh` o, como mínimo, `docker build --target migraciones .` y `docker build --target final .`; deben terminar en 0 sin editar el `Dockerfile`. Si falla por un lock desactualizado, regenerarlo y repetir T003.
- [X] T006 [CI] Verificar con `gh api repos/<propietario>/<acción>/git/ref/tags/<versión>` (o `git ls-remote`) que los SHA de `investigacion.md` D2 siguen correspondiendo a `actions/checkout` v7.0.1, `actions/setup-dotnet` v6.1.0, `actions/setup-java` v6.2.0 y `github/codeql-action` v4.39.3; corregir D2 si alguno cambió.

## Fase 2: Base común de pruebas (bloquea las historias)

- [X] T007 [BE] Crear `src/Pruebas/Entorno/FlujosCi.cs` con un ayudante que localice `.github/workflows/*.yml` con `RaizRepositorio` (`src/Pruebas/Entorno/RaizRepositorio.cs`) y los cargue con YamlDotNet a un árbol navegable (disparadores, `permissions`, `jobs`, pasos `uses`/`run`); sin pruebas propias, lo reutilizan T008, T013, T016 y T023.

## Fase 3: Historia 1 — Compilación y pruebas en cada cambio (P1) 🎯 MVP

**Meta**: cada pull request y push a `main`, `release`, `developer` y `sprint/**` compila la solución completa y ejecuta todas las pruebas, incluidas las de Testcontainers, y el resultado queda visible como error o éxito.

**Prueba independiente**: abrir un pull request con un cambio válido y ver `Compilación y pruebas` en verde; romper algo a propósito y verlo en rojo.

### Pruebas primero

- [X] T008 [US1] [BE] Escribir `src/Pruebas/Entorno/IntegracionContinuaPruebas.cs` sobre `.github/workflows/integracion-continua.yml`: existe; `pull_request` y `push` con `branches` exactamente `main`, `release`, `developer` y `sprint/**`; sin `paths`, `paths-ignore`, `schedule` ni `workflow_dispatch`; nombre `Integración continua`; job `compilar-y-probar` con `name: Compilación y pruebas`, `runs-on: ubuntu-latest` y `timeout-minutes: 10`; `permissions` solo `contents: read`; `concurrency` con `cancel-in-progress: true`; `actions/checkout` con `persist-credentials: false`; `actions/setup-dotnet` con `global-json-file: global.json`, `cache: true` y `cache-dependency-path: '**/packages.lock.json'`; pasos en orden `dotnet restore BlackHellHeim.sln --locked-mode`, `dotnet build BlackHellHeim.sln --no-restore --configuration Release` y `dotnet test BlackHellHeim.sln --no-build --configuration Release`; sin `services:` ni referencias a `secrets.`; sin pasos de despliegue (RF-015). Debe fallar.

### Implementación

- [X] T009 [US1] [CI] Crear `.github/workflows/integracion-continua.yml` según `contratos/verificaciones-ci.md` §1 y §2.1, con las acciones fijadas a SHA completo y la versión legible en comentario (`investigacion.md` D2), comentarios y nombres en español, y un comentario que explique que las pruebas de Testcontainers usan el Docker del ejecutor y que `scripts/verificar-entorno.sh` no corre aquí.
- [X] T010 [US1] [BE] Ejecutar `dotnet test BlackHellHeim.sln --filter "FullyQualifiedName~IntegracionContinuaPruebas"` y confirmar que T008 pasa.
- [X] T011 [US1] [MANUAL] Habilitar GitHub Actions en el repositorio (Settings → Actions → General → permitir acciones; permisos del flujo en **Read repository contents**) según `inicio-rapido.md` Paso 5.
- [X] T012 [US1] [MANUAL] Tras el push a `sprint/NN`, abrir un pull request real y comprobar que `Compilación y pruebas` aparece y termina en verde, que la duración en `gh run list` es de 10 minutos o menos (incluida la descarga de la imagen de SQL Server) y que en un pull request de solo documentación el check también se ejecuta (escenarios 1 y 10 de `inicio-rapido.md`).

**Punto de control**: US1 funciona y se valida por separado; es el MVP.

## Fase 4: Historia 2 — Flujo sin secretos expuestos (P2)

**Meta**: ningún flujo ni archivo versionado contiene credenciales; los pull requests desde bifurcaciones compilan y prueban sin secretos.

**Prueba independiente**: las pruebas de T013 pasan y la revisión del historial no encuentra valores sensibles.

### Pruebas primero

- [X] T013 [P] [US2] [BE] Escribir `src/Pruebas/Entorno/FlujosSinSecretosPruebas.cs` recorriendo todos los `.github/workflows/*.yml`: ningún `uses:` sin SHA de 40 caracteres hexadecimales; ningún `pull_request_target`; `permissions` declarado a nivel de flujo en cada archivo y nunca `write-all`; `secrets.` solo como `secrets.SONAR_TOKEN` y solo en `calidad-sonar.yml`; ningún valor literal con aspecto de token (por ejemplo cadenas de 40 hexadecimales en `SONAR_TOKEN`) ni `sonar.token`/`/d:sonar.token` en la línea de comandos de ningún `run:`; ningún `run:` que imprima (`echo`, `printenv`, `env`, `set -x`) variables de secretos. Además exige que existan los tres archivos `integracion-continua.yml`, `codeql.yml` y `calidad-sonar.yml`, para que falle (no pase en vacío) hasta que los tres estén creados; se vuelve a ejecutar completa en T027. Debe fallar.

### Implementación

- [X] T014 [US2] [CI] Revisar que `.env` sigue ignorado y que `.env.example` solo contiene marcadores; ejecutar `git grep -nEi "(sonar_token|api[_-]?key|secret|password)\s*[:=]\s*['\"]?[A-Za-z0-9+/_-]{16,}" -- . ':!specs' ':!src/Pruebas'` y `git log -p --all -S"SONAR_TOKEN" -- .` y confirmar que no aparece ningún valor; anotar el resultado en `specs/006-integracion-continua/inicio-rapido.md` solo si se encontró algo que corregir.
- [ ] T015 [US2] [MANUAL] Abrir un pull request desde una bifurcación (o simularlo sin acceso a secretos) y comprobar que `Compilación y pruebas` y `CodeQL (C#)` corren y que `SonarQube Cloud` se omite de forma visible; revisar los registros de una ejecución y confirmar que ningún valor sensible aparece (escenarios 6 y 8 de `inicio-rapido.md`). **Aplazada: SonarQube Cloud sin configurar; se completa en un sprint posterior**.

## Fase 5: Historia 3 — Análisis de seguridad con CodeQL (P3)

**Meta**: CodeQL analiza C# en cada pull request y push y publica los hallazgos con severidad y ubicación, con permisos mínimos.

**Prueba independiente**: `CodeQL (C#)` aparece en el pull request y publica resultados en Security → Code scanning.

### Pruebas primero

- [X] T016 [US3] [BE] Escribir `src/Pruebas/Entorno/CodeQlPruebas.cs` sobre `.github/workflows/codeql.yml`: existe; mismos disparadores y ramas que T008; job `analizar` con `name: CodeQL (C#)`, `timeout-minutes: 10`; `permissions` de flujo `contents: read` y el job añade solo `security-events: write`; `github/codeql-action/init` con `languages: csharp` y `queries: security-extended`; `restore --locked-mode` y `build` entre `init` y `analyze`; `github/codeql-action/analyze` con `category: /language:csharp`; no ejecuta `dotnet test` ni requiere Docker. Debe fallar.

### Implementación

- [X] T017 [US3] [CI] Crear `.github/workflows/codeql.yml` según `contratos/verificaciones-ci.md` §2.2 y `investigacion.md` D3, con acciones fijadas a SHA y comentarios en español.
- [X] T018 [US3] [BE] Ejecutar `dotnet test BlackHellHeim.sln --filter "FullyQualifiedName~CodeQlPruebas"` y confirmar que T016 pasa.
- [X] T019 [US3] [MANUAL] Desactivar la configuración predeterminada de CodeQL (Settings → Code security → Code scanning) para que el flujo avanzado sea el único (`inicio-rapido.md` Paso 3).
- [X] T020 [US3] [MANUAL] En el pull request real comprobar que `CodeQL (C#)` termina en verde y que sus resultados aparecen en Security → Code scanning; opcionalmente introducir un patrón vulnerable en una rama de prueba y ver el hallazgo con severidad y ubicación (escenario 4).

## Fase 6: Historia 4 — Análisis de calidad con SonarQube Cloud (P4)

**Meta**: SonarQube Cloud analiza cada cambio con cobertura y publica la compuerta de calidad; sin token, el análisis se omite de forma visible sin romper la compilación ni las pruebas.

**Prueba independiente**: `SonarQube Cloud` aparece en el pull request con la compuerta evaluada; en una bifurcación sin token queda omitido.

### Configuración externa

- [ ] T021 [US4] [MANUAL] Crear la cuenta/organización en SonarQube Cloud, importar el repositorio `blackhellheim-api`, anotar **Organization key** y **Project key**, desactivar **Automatic Analysis** y revisar la compuerta **Sonar way** (`inicio-rapido.md` Paso 1). **Aplazada: SonarQube Cloud sin configurar; se completa en un sprint posterior**.
- [ ] T022 [US4] [MANUAL] Generar el token de análisis de proyecto en SonarQube Cloud y guardarlo con `gh secret set SONAR_TOKEN` (nunca en un archivo del repositorio); crear las variables `SONAR_ORGANIZATION` y `SONAR_PROJECT_KEY` con `gh variable set` (`inicio-rapido.md` Paso 2). **Aplazada: SonarQube Cloud sin configurar; se completa en un sprint posterior**.

### Pruebas primero

- [X] T023 [US4] [BE] Escribir `src/Pruebas/Entorno/CalidadSonarPruebas.cs` sobre `.github/workflows/calidad-sonar.yml`: existe; mismos disparadores y ramas que T008; permisos `contents: read` solamente; job `detectar-token` (`name: Detección de token de Sonar`) que lee `secrets.SONAR_TOKEN` en una variable de entorno y escribe `disponible` en `GITHUB_OUTPUT` sin imprimirlo; job `analizar` (`name: SonarQube Cloud`) con `needs: detectar-token` y `if: needs.detectar-token.outputs.disponible == 'true'`, `timeout-minutes: 10`; checkout con `fetch-depth: 0` y `persist-credentials: false`; `setup-java` con Temurin 21; pasos `dotnet tool restore`, `dotnet sonarscanner begin` con `sonar.host.url=https://sonarcloud.io`, `sonar.organization`/`sonar.projectKey` desde `vars.*`, el token solo vía `env: SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}` en los pasos `begin` y `end` (nunca como argumento de línea de comandos), `sonar.cs.cobertura.reportsPaths=**/TestResults/**/coverage.cobertura.xml`, `sonar.qualitygate.wait=true`, `sonar.qualitygate.timeout=300` y `sonar.sourceEncoding=UTF-8`; después `dotnet restore --locked-mode`, `dotnet build --configuration Release`, `dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"` y `dotnet sonarscanner end`. Debe fallar.

### Implementación

- [X] T024 [US4] [CI] Crear `.github/workflows/calidad-sonar.yml` según `contratos/verificaciones-ci.md` §2.3 y `investigacion.md` D4, D5 y D8, con acciones fijadas a SHA y el token pasado solo por variable de entorno.
- [X] T025 [US4] [BE] Ejecutar `dotnet test BlackHellHeim.sln --filter "FullyQualifiedName~CalidadSonarPruebas"` y confirmar que T023 pasa.
- [ ] T026 [US4] [MANUAL] En el pull request real comprobar que `SonarQube Cloud` evalúa la compuerta y termina en verde, que la cobertura se publica y que, sin `SONAR_TOKEN`, el job queda omitido de forma visible (escenarios 1, 5 y 8 de `inicio-rapido.md`). **Aplazada: SonarQube Cloud sin configurar; se completa en un sprint posterior**.

## Fase 7: Cierre y verificación transversal

- [X] T027 [BE] Ejecutar `dotnet restore BlackHellHeim.sln --locked-mode`, `dotnet build BlackHellHeim.sln --configuration Release --no-restore` y `dotnet test BlackHellHeim.sln --no-build --configuration Release` completos (con Docker) y confirmar que T013 (`FlujosSinSecretosPruebas`) pasa con los tres flujos presentes.
- [X] T028 [CI] Repetir `./scripts/probar-imagen.sh` (o los dos `docker build --target ...`) para confirmar que ningún cambio de esta historia rompió el `Dockerfile` con `--locked-mode`.
- [X] T029 [CI] Actualizar `specs/006-integracion-continua/inicio-rapido.md` y `investigacion.md` solo si la implementación difiere de lo planeado (nombres de checks, versiones, tiempos medidos) y mantener `contratos/verificaciones-ci.md` en sincronía con los nombres reales de los jobs.
- [X] T030 [MANUAL] Prueba negativa: en una rama de prueba romper algo a propósito (un error de compilación y, por separado, una aserción de prueba) y confirmar que `Compilación y pruebas` queda en rojo en el pull request y que el detalle muestra el fallo (escenarios 2 y 3 de `inicio-rapido.md`); que el cambio no pueda fusionarse se comprueba en T032, cuando la protección ya esté activa; luego descartar la rama de prueba.
- [X] T031 [MANUAL] Activar la protección de `main` (Settings → Branches/Rules) exigiendo pull request y las verificaciones `Compilación y pruebas`, `CodeQL (C#)` y `SonarQube Cloud`, rama actualizada, sin force push ni eliminación (`inicio-rapido.md` Paso 4); opcionalmente repetir la regla en `release` y `developer`.
- [X] T032 [MANUAL] Con la protección de `main` activa, abrir (sin fusionar) un pull request de prueba hacia `main` con una verificación en rojo y confirmar que el botón de fusión queda bloqueado. La ejecución de los tres flujos en el push a `main` (escenario 7) se verifica cuando el sprint se fusione a `main` por el flujo de `AGENTS.md`; no bloquea T033.
- [ ] T033 [BE] Cuando T012, T020, T026, T030 y T032 estén hechas (el push a `main` del escenario 7 queda para después de la aprobación del sprint), actualizar la fila de HU-6 en `docs/scrum/backlog.md` (Spec y Estado = Hecha) y hacer commit `HU-6: Integración continua del API`; después detenerse y avisar con el mensaje que pide `AGENTS.md`.

## Dependencias y orden de ejecución

- Fase 1 (T001–T006) → Fase 2 (T007) → historias.
- T002 → T003 → T005; T004 → T005 (el `Dockerfile` restaura la herramienta y los locks).
- US1 es el MVP y no depende de las demás. US2, US3 y US4 dependen de T007; T013 necesita los tres flujos para validar todo (se completa en T027).
- US4 depende de T002 (cobertura) y T004 (scanner); sus tareas `[MANUAL]` T021 y T022 deben estar hechas antes de T026.
- T031 depende de que los tres checks hayan corrido al menos una vez (T012, T020, T026) para poder seleccionarlos por nombre.
- Los `git push` y los pull requests los hace la desarrolladora entre tareas; no se listan.

## Oportunidades de paralelismo

- T013 es independiente y puede escribirse en paralelo con T008.
- T016 y T023 son pruebas en archivos distintos y pueden escribirse a la vez; T017 y T024 también.
- T021 y T022 (SonarQube Cloud) pueden hacerse mientras se implementan T009 a T018.

## Estrategia de implementación

1. **MVP**: Fases 1 y 2, luego US1 (T008–T012). Con eso ya hay compilación y pruebas en cada cambio.
2. Añadir US2 (T013–T015) para blindar el repositorio público.
3. Añadir US3 (CodeQL) y US4 (Sonar) en ese orden; Sonar es la última por depender de la cuenta externa.
4. Cerrar con la Fase 7: pruebas locales, prueba negativa, protección de ramas y backlog.

## Resumen

- Tareas totales: 33 (`[MANUAL]`: T011, T012, T015, T019–T022, T026, T030–T032).
- Preparación: 6 · Base común: 1 · US1: 5 · US2: 3 · US3: 5 · US4: 6 · Cierre: 7.
