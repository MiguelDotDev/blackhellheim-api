# Inicio rápido (Fase 1): Integración continua del API

Historia 006 · Fecha: 2026-11-10

Cómo dejar operativa la integración continua y validar de extremo a extremo que la
historia funciona. Los pasos manuales se hacen una sola vez en GitHub y SonarQube
Cloud; el resto queda versionado en `.github/workflows/`. Los detalles de
implementación viven en `tasks.md` (por generarse con `/speckit.tasks`).

## Requisitos previos

- Repositorio público en GitHub con la rama `main`.
- Permisos de administración sobre el repositorio para configurar secretos, variables,
  code scanning y protección de rama.
- Cuenta de GitHub con acceso a SonarQube Cloud.
- `gh` CLI autenticado (opcional, para los comandos de ejemplo) o acceso a la interfaz
  web de GitHub.
- SDK de .NET 10 local para validar antes de subir (`dotnet --version` reporta 10.x).

## Paso 1. Crear la cuenta y el proyecto en SonarQube Cloud

1. Entrar a <https://sonarcloud.io> e iniciar sesión con GitHub.
2. Autorizar la GitHub App de SonarQube Cloud sobre la cuenta u organización dueña del
   repositorio.
3. Crear o importar la organización de SonarQube Cloud y, dentro de ella, importar el
   repositorio `blackhellheim-api`.
4. Anotar dos valores públicos que aparecen en la configuración del proyecto:
   - **Organization key** (por ejemplo, `blackhellheim`).
   - **Project key** (por ejemplo, `blackhellheim_blackhellheim-api`).
5. En **Administration → Analysis Method**, desactivar **Automatic Analysis**. C# no se
   analiza con el análisis automático: requiere el análisis basado en CI que ejecuta
   este repositorio. Si se deja activo, los dos análisis compiten y el proyecto queda
   inconsistente.
6. Revisar la compuerta de calidad. La compuerta **Sonar way** es la recomendada; el
   flujo espera su resultado con `sonar.qualitygate.wait=true`. Si se usa otra compuerta,
   debe incluir cobertura sobre código nuevo, porque el flujo publica el informe de
   cobertura.

## Paso 2. Generar el token de Sonar y guardarlo como secreto

1. En SonarQube Cloud, ir a **My Account → Security**.
2. Generar un token de tipo **Project Analysis Token** limitado al proyecto
   `blackhellheim-api`, con un nombre reconocible (por ejemplo,
   `blackhellheim-api-ci`). Un token de proyecto tiene el alcance mínimo necesario.
3. Copiar el valor una sola vez; SonarQube Cloud no lo vuelve a mostrar.
4. Guardarlo como secreto del repositorio, nunca en un archivo:

   ```bash
   gh secret set SONAR_TOKEN --repo <propietario>/blackhellheim-api
   ```

   O en la interfaz: **Settings → Secrets and variables → Actions → New repository
   secret**, nombre `SONAR_TOKEN`.

5. Crear las variables públicas del repositorio:

   ```bash
   gh variable set SONAR_ORGANIZATION --repo <propietario>/blackhellheim-api --body "<organization key>"
   gh variable set SONAR_PROJECT_KEY --repo <propietario>/blackhellheim-api --body "<project key>"
   ```

   O en **Settings → Secrets and variables → Actions → Variables**.

6. Verificar que el token no quedó en el repositorio:

   ```bash
   git grep -n "SONAR_TOKEN" -- . ':!specs' || true
   ```

   Solo deben aparecer referencias a `${{ secrets.SONAR_TOKEN }}` en el flujo, nunca el
   valor.

## Paso 3. Desactivar la configuración predeterminada de CodeQL

La configuración avanzada por flujo no convive con la configuración predeterminada.

1. Ir a **Settings → Code security → Code scanning**.
2. Si **CodeQL analysis** está en modo **Default**, desactivarlo.
3. Dejar que el flujo `.github/workflows/codeql.yml` sea el único que ejecute CodeQL.
   Al fusionar el primer pull request, GitHub mostrará los resultados en la pestaña
   **Security → Code scanning**.

## Paso 4. Activar la protección de la rama principal

1. Ir a **Settings → Branches** (o **Rules**) y crear una regla para `main`.
2. Activar **Require a pull request before merging**.
3. Activar **Require status checks to pass before merging** y agregar exactamente estas
   verificaciones:
   - `Compilación y pruebas`
   - `CodeQL (C#)`
   - `SonarQube Cloud`
4. Activar **Require branches to be up to date before merging**.
5. Bloquear **force pushes** y la **eliminación** de la rama.
6. Opcional: exigir revisión de al menos una persona y descartar aprobaciones obsoletas.

Los nombres deben coincidir carácter por carácter con los jobs de los flujos. Si se
renombra un job, hay que actualizar la protección de rama y
`contratos/verificaciones-ci.md`.

> Un job omitido por condición reporta «Success» en GitHub. Por eso `SonarQube Cloud`
> puede ser una verificación requerida y, aun así, omitirse de forma visible en pull
> requests desde bifurcaciones sin token.

## Paso 5. Revisar permisos de Actions

1. Ir a **Settings → Actions → General → Workflow permissions**.
2. Dejar el permiso predeterminado en **Read repository contents**.
3. Los flujos declaran sus propios permisos: lectura general y escritura de eventos de
   seguridad solo en CodeQL. No se necesita ningún permiso de escritura adicional.
4. Si el repositorio exige aprobación para ejecutar flujos de contribuyentes nuevos,
   tenerlo en cuenta: la primera ejecución de un pull request externo puede quedar
   pendiente de aprobación manual.

## Validación local antes de subir

```bash
dotnet restore BlackHellHeim.sln
dotnet build BlackHellHeim.sln --no-restore --configuration Release
dotnet test BlackHellHeim.sln --no-build --configuration Release
```

Resultado esperado: compilación limpia y todas las pruebas en verde. Es exactamente lo
que ejecuta el job `Compilación y pruebas`.

Para validar la herramienta de Sonar sin subir nada:

```bash
dotnet tool restore
dotnet sonarscanner --version
```

Resultado esperado: `12.1.0` o la versión fijada en `.config/dotnet-tools.json`. No
ejecutar el análisis local sin necesidad; el token no debe quedar en el historial del
shell.

## Escenarios de validación de extremo a extremo

| # | Escenario | Cómo provocarlo | Resultado esperado |
|---|-----------|-----------------|--------------------|
| 1 | Pull request válido | Abrir un pull request con un cambio inocuo | Los tres checks aparecen y terminan en verde |
| 2 | Error de compilación | Introducir un error de sintaxis en una clase | `Compilación y pruebas` en rojo; el botón de fusión se bloquea |
| 3 | Prueba fallida | Romper una aserción de una prueba | `Compilación y pruebas` en rojo; el detalle muestra la prueba |
| 4 | Hallazgo de seguridad | Introducir un patrón vulnerable conocido en una rama de prueba | `CodeQL (C#)` publica el hallazgo con severidad y ubicación |
| 5 | Compuerta de calidad rechazada | Introducir duplicación o deuda técnica suficiente para bajar la compuerta | `SonarQube Cloud` en rojo con el motivo de la compuerta |
| 6 | Bifurcación sin token | Abrir un pull request desde una bifurcación | Compilación y CodeQL en verde; `SonarQube Cloud` omitido y visible |
| 7 | Push a `main` | Fusionar un pull request aprobado | Los tres flujos se ejecutan sobre `main` y actualizan el estado |
| 8 | Token vencido o revocado | Revocar el token en SonarQube Cloud y abrir un pull request | `SonarQube Cloud` falla o se omite de forma visible, nunca en verde falso |
| 9 | Commit nuevo durante una ejecución | Empujar otro commit al mismo pull request | La ejecución anterior se cancela y cuenta la del commit vigente |
| 10 | Solo documentación | Abrir un pull request que solo cambie un `.md` | Los tres checks reportan un estado comprensible; ninguno queda pendiente |

## Consultas útiles

```bash
# Ver el estado de los checks de un pull request
gh pr checks <número>

# Ver las ejecuciones recientes
gh run list --limit 10

# Ver los secretos y variables configurados (no muestra valores de secretos)
gh secret list
gh variable list
```

## Solución de problemas

- **`SonarQube Cloud` se omite en un pull request del mismo repositorio**: revisar que
  `SONAR_TOKEN` exista y que el evento sea `pull_request` (no `pull_request_target`).
- **Sonar falla con «project not found»**: revisar que `SONAR_ORGANIZATION` y
  `SONAR_PROJECT_KEY` coincidan con los valores de SonarQube Cloud.
- **Sonar falla por cobertura**: confirmar que el paso de pruebas usa
  `--collect:"XPlat Code Coverage"` y que la ruta
  `**/TestResults/**/coverage.cobertura.xml` existe en el workspace.
- **CodeQL no publica resultados**: confirmar que la configuración predeterminada está
  desactivada y que el job tiene `security-events: write`.
- **Un check requerido queda pendiente**: confirmar que el nombre del job no cambió y
  que el flujo se dispara para la base `main`.
