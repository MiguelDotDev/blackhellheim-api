# Contrato de verificaciones de integración continua

Historia 006 · Fecha: 2026-11-10

Este contrato define la interfaz que la integración continua expone al repositorio y a
la protección de rama: qué flujos existen, con qué nombres, qué permisos usan, qué
entradas necesitan y qué resultado produce cada uno. Los nombres son estables: la
protección de rama y la documentación dependen de ellos.

## 1. Disparadores comunes

| Evento | Ramas | Flujos que se ejecutan |
|--------|-------|------------------------|
| `pull_request` | base `main`, `release`, `developer` o `sprint/**` | los tres |
| `push` | `main`, `release`, `developer` y `sprint/**` | los tres |

No se usan `paths-ignore` ni `paths`: todo cambio, incluida la documentación, pasa por
las verificaciones. No hay `schedule` ni `workflow_dispatch` en esta historia.

## 2. Flujos y jobs

### 2.1 `integracion-continua.yml`

| Propiedad | Valor |
|-----------|-------|
| Nombre del flujo | `Integración continua` |
| Job | `compilar-y-probar` |
| Nombre del check | `Compilación y pruebas` |
| Ejecutor | `ubuntu-latest` |
| Permisos | `contents: read` |
| Secretos | ninguno |
| Variables | ninguna |
| Tiempo máximo | 10 minutos |

Pasos:

1. `actions/checkout` con `persist-credentials: false`.
2. `actions/setup-dotnet` con `global-json-file: global.json`, `cache: true` y
   `cache-dependency-path: '**/packages.lock.json'`.
3. `dotnet restore BlackHellHeim.sln --locked-mode`.
4. `dotnet build BlackHellHeim.sln --no-restore --configuration Release`.
5. `dotnet test BlackHellHeim.sln --no-build --configuration Release`.

Pruebas con contenedores: el paso 5 incluye las pruebas de persistencia que usan
Testcontainers (SQL Server 2022). El ejecutor `ubuntu-latest` ya tiene Docker, por lo que
no se agregan pasos, `services:` ni credenciales; el contenedor y su contraseña los crea
la propia prueba. Si Docker no está disponible el paso falla, no se omite. Los scripts
`scripts/verificar-entorno.sh` (completo), `probar-base-datos.sh` y `probar-imagen.sh`
no forman parte de este flujo.

Resultado esperado: la solución completa compila y todas las pruebas pasan. Un error de
compilación o una prueba fallida termina el job con estado fallido y el pull request
muestra el check en rojo.

### 2.2 `codeql.yml`

| Propiedad | Valor |
|-----------|-------|
| Nombre del flujo | `Análisis de seguridad (CodeQL)` |
| Job | `analizar` |
| Nombre del check | `CodeQL (C#)` |
| Ejecutor | `ubuntu-latest` |
| Permisos | `contents: read`, `security-events: write` |
| Secretos | ninguno |
| Variables | ninguna |
| Tiempo máximo | 10 minutos |

Pasos:

1. `actions/checkout` con `persist-credentials: false`.
2. `actions/setup-dotnet` con `global-json-file: global.json` y caché de NuGet
   (`**/packages.lock.json`).
3. `github/codeql-action/init` con `languages: csharp` y
   `queries: security-extended`.
4. `dotnet restore BlackHellHeim.sln --locked-mode`.
5. `dotnet build BlackHellHeim.sln --no-restore --configuration Release`.
6. `github/codeql-action/analyze` con categoría `/language:csharp`.

Resultado esperado: el análisis publica el SARIF en GitHub code scanning. Sin hallazgos,
el check queda exitoso; con hallazgos, cada uno se muestra con severidad y ubicación.
En pull requests desde bifurcaciones de un repositorio público, la carga de resultados
está permitida porque el evento es `pull_request`, sin secretos del repositorio.

### 2.3 `calidad-sonar.yml`

| Propiedad | Valor |
|-----------|-------|
| Nombre del flujo | `Análisis de calidad (SonarQube Cloud)` |
| Job 1 | `detectar-token` — nombre visible `Detección de token de Sonar` |
| Job 2 | `analizar` — nombre visible `SonarQube Cloud` |
| Ejecutor | `ubuntu-latest` |
| Permisos | `contents: read` |
| Secretos | `SONAR_TOKEN` |
| Variables | `SONAR_ORGANIZATION`, `SONAR_PROJECT_KEY` |
| Tiempo máximo | 10 minutos por job |
| Docker | Requerido por las pruebas con contenedores (preinstalado en `ubuntu-latest`) |

Job `detectar-token`:

1. Lee `secrets.SONAR_TOKEN` en una variable de entorno.
2. Escribe `disponible=true` o `disponible=false` en `GITHUB_OUTPUT`.
3. Nunca imprime el valor.

Job `analizar`:

- Se ejecuta solo si `needs.detectar-token.outputs.disponible == 'true'`.
- Pasos: checkout con `fetch-depth: 0` (historial completo para el análisis de SCM);
  `actions/setup-dotnet` con `global-json-file: global.json` y caché; `actions/setup-java`
  con Temurin 21; `dotnet tool restore` (el manifiesto incluye `dotnet-ef` y
  `dotnet-sonarscanner`); `dotnet sonarscanner begin`;
  `dotnet restore --locked-mode`; `dotnet build` en `Release`;
  `dotnet test --no-build --collect:"XPlat Code Coverage"` (incluye las pruebas con
  Testcontainers, que usan el Docker del ejecutor); `dotnet sonarscanner end`.
- El token se pasa por variable de entorno tanto en `begin` como en `end`; el scanner
  no lo conserva en ningún archivo versionado.

Propiedades del scanner:

| Propiedad | Valor |
|-----------|-------|
| `sonar.host.url` | `https://sonarcloud.io` |
| `sonar.organization` | `${{ vars.SONAR_ORGANIZATION }}` |
| `sonar.projectKey` | `${{ vars.SONAR_PROJECT_KEY }}` |
| `sonar.token` | No se pasa como argumento: el scanner lee la variable de entorno `SONAR_TOKEN` (`env: SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}` en los pasos `begin` y `end`) |
| `sonar.cs.cobertura.reportsPaths` | `**/TestResults/**/coverage.cobertura.xml` |
| `sonar.qualitygate.wait` | `true` |
| `sonar.qualitygate.timeout` | `300` |
| `sonar.sourceEncoding` | `UTF-8` |

Resultado esperado: el job espera la compuerta de calidad y termina exitoso solo si se
cumple; si se rechaza, el check falla y el pull request lo muestra. Sin token, el job
`SonarQube Cloud` queda omitido (visible) y reporta éxito para la protección de rama.

## 3. Contrato de permisos y secretos

| Recurso | Dónde vive | Quién lo usa | Regla |
|---------|------------|--------------|-------|
| `GITHUB_TOKEN` | Lo genera GitHub por ejecución | Los tres flujos | Lectura por defecto; escritura de eventos de seguridad solo en CodeQL |
| `SONAR_TOKEN` | Secreto del repositorio | Solo el job `SonarQube Cloud` | Nunca se versiona, imprime ni pasa a otro job |
| `SONAR_ORGANIZATION` | Variable del repositorio | Solo el job `SonarQube Cloud` | Valor público de SonarQube Cloud |
| `SONAR_PROJECT_KEY` | Variable del repositorio | Solo el job `SonarQube Cloud` | Valor público de SonarQube Cloud |

Reglas de validación:

- Ningún archivo versionado contiene valores de `SONAR_TOKEN`.
- El token se pasa al scanner por variable de entorno en el paso que lo necesita; no se
  usa en la línea de comandos de un paso que lo pueda registrar.
- Si el token falta, el resultado es `omitido`; si está vencido o revocado, el resultado
  es fallido o `omitido` explícito, nunca exitoso.
- Si faltan las variables de organización o clave, el job falla de forma visible.

## 4. Contrato con la protección de rama

Verificaciones requeridas en `main` (los flujos también se ejecutan en `release`,
`developer` y `sprint/**`; esas ramas pueden reutilizar la misma lista de requeridas):

| Verificación requerida | Flujo | Job | Resultado cuando se omite |
|------------------------|-------|-----|---------------------------|
| `Compilación y pruebas` | `integracion-continua.yml` | `compilar-y-probar` | No aplica: siempre se ejecuta |
| `CodeQL (C#)` | `codeql.yml` | `analizar` | No aplica: siempre se ejecuta |
| `SonarQube Cloud` | `calidad-sonar.yml` | `analizar` | Omitido; GitHub lo reporta como éxito visible |

`Detección de token de Sonar` no es una verificación requerida: es un detalle interno
del flujo de calidad.

Reglas:

- Un pull request con una verificación requerida en `pendiente` o `fallida` no puede
  integrarse.
- Un job omitido por condición reporta «Success» y satisface la verificación, pero el
  pull request muestra la omisión en la lista de checks.
- Cambiar el nombre de un job exige actualizar la protección de rama y este contrato.

## 5. Matriz de requisitos a verificaciones

| Requisito | Verificación que lo cubre |
|-----------|---------------------------|
| RF-001, RF-002 | `Compilación y pruebas` en pull request y push a `main` |
| RF-003, RF-004 | Estado del check y protección de rama |
| RF-005, RF-006 | `CodeQL (C#)` y sus hallazgos publicados |
| RF-007, RF-008 | `SonarQube Cloud` y la compuerta esperada |
| RF-009, RF-010 | Revisión de archivos versionados y uso de secretos |
| RF-011, RF-012 | Job de detección y omisión visible; fallo si el token es inválido |
| RF-013 | Permisos mínimos por flujo |
| RF-014 | Nombres de check comprensibles |
| RF-015 | Ausencia de pasos de despliegue o aprovisionamiento |

## 6. Resultados esperados por escenario

| Escenario | `Compilación y pruebas` | `CodeQL (C#)` | `SonarQube Cloud` |
|-----------|-------------------------|---------------|-------------------|
| Pull request válido con token | Exitoso | Exitoso (o hallazgos visibles) | Exitoso si la compuerta se cumple |
| Pull request con error de compilación | Fallido | Fallido | Fallido |
| Pull request con prueba fallida | Fallido | Exitoso | Fallido |
| Pull request con hallazgo de seguridad | Exitoso | Exitoso con hallazgo visible | Según compuerta |
| Pull request de solo documentación | Exitoso | Exitoso | Exitoso u omitido |
| Pull request desde bifurcación sin token | Exitoso | Exitoso | Omitido visible |
| Push a `main`, `release`, `developer` o `sprint/**` | Exitoso o fallido visible | Exitoso o fallido visible | Exitoso, fallido u omitido visible |
| Token vencido o revocado | Exitoso | Exitoso | Fallido visible |
| Ejecución cancelada por un commit nuevo | No cuenta | No cuenta | No cuenta |
