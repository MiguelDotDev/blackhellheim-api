# Plan de implementación: Integración continua del API

**Rama**: `002-integracion-continua` | **Fecha**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de funcionalidad en `specs/002-integracion-continua/spec.md`

## Resumen

La historia 002 dota al repositorio de una integración continua completa en GitHub
Actions con tres flujos independientes: `integracion-continua.yml` (restore, build y
`dotnet test` sobre `BlackHellHeim.sln` con .NET 10), `codeql.yml` (análisis estático de
seguridad para C#) y `calidad-sonar.yml` (análisis de calidad con SonarQube Cloud). Los
tres se disparan en cada pull request y en cada push a `main`, usan permisos mínimos en
`GITHUB_TOKEN`, fijan cada acción oficial a un SHA completo con su licencia anotada y
aprovechan la caché de NuGet de `actions/setup-dotnet` para mantener cada job por debajo
de 10 minutos. El token de Sonar vive en un secreto del repositorio; cuando no existe
(bifurcaciones), el análisis se omite de forma visible sin romper la compilación ni las
pruebas. Los nombres de los jobs coinciden con las verificaciones requeridas de la
protección de rama, de modo que un cambio roto no pueda integrarse.

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10; los proyectos declaran `net10.0` y el flujo
instala la última revisión `10.0.x` del SDK. No se agrega `global.json`: el repositorio
fija el framework de destino y el flujo la banda `10.0.x`, lo que evita bloquear el SDK
local 10.0.114 y mantiene el rango de parches actualizado.

**Dependencias principales**:
- Acciones oficiales de GitHub, fijadas a SHA completo (véase la tabla de licencias).
- `dotnet-sonarscanner` 12.1.0 (NuGet, LGPL-3.0), herramienta oficial de SonarSource
  para soluciones .NET, instalada como herramienta local del repositorio.
- `coverlet.collector` 10.1.0 (NuGet, MIT), colector de cobertura que alimenta a Sonar.
- Temurin JDK 21 (GPLv2 con Classpath Exception) mediante `actions/setup-java`, requisito
  del scanner de Sonar.
- Sin MediatR 13+, AutoMapper ni FluentAssertions (principio IV). Licencias verificadas
  en `investigacion.md`.

**Almacenamiento**: N/A. Esta historia no aprovisiona base de datos, almacenamiento de
archivos ni artefactos persistentes; solo registros de ejecución de GitHub.

**Pruebas**: `dotnet test BlackHellHeim.sln` sobre la solución completa con xUnit. El
flujo de compilación y pruebas lo ejecuta en `Release`; el flujo de Sonar lo repite con
`--collect:"XPlat Code Coverage"` para publicar cobertura en el análisis de calidad. Las
pruebas unitarias de reglas de negocio siguen siendo responsabilidad de cada historia
(principio III); aquí se garantiza que se ejecuten en cada cambio.

**Plataforma destino**: ejecutores `ubuntu-latest` alojados por GitHub. El repositorio es
público y usa recursos gratuitos (Actions para repositorios públicos y SonarQube Cloud
para proyectos públicos).

**Tipo de proyecto**: automatización de integración continua de un servicio web; no se
modifica el código de la API.

**Metas de rendimiento**: cada job con `timeout-minutes: 10`; con la caché de NuGet y la
ejecución en paralelo de los tres flujos, el resultado completo de un pull request queda
disponible en 10 minutos o menos para el tamaño actual del repositorio.

**Restricciones**: repositorio público sin secretos versionados; solo software libre o
gratuito; permisos mínimos en `GITHUB_TOKEN`; acciones fijadas a SHA; nombres de
verificación estables para la protección de rama; todo en español.

**Escala/Alcance**: una solución (`BlackHellHeim.sln`), cinco proyectos, tres flujos de
trabajo y cuatro verificaciones visibles (compilación y pruebas, CodeQL, SonarQube Cloud
y la detección de token que habilita el análisis de calidad).

## Verificación de la constitución

*COMPUERTA: debe pasar antes de la investigación de la Fase 0. Se vuelve a comprobar
tras el diseño de la Fase 1.*

| # | Principio | Estado | Nota |
|---|-----------|--------|------|
| I | Arquitectura limpia | ✅ | No se tocan capas ni referencias entre proyectos; la CI compila la solución existente. |
| II | Contrato primero | N/A | No se agregan endpoints ni se modifica el contrato OpenAPI. |
| III | Reglas de negocio probadas | ✅ | Cada pull request y push a `main` ejecuta todas las pruebas de la solución; un fallo bloquea la integración. |
| IV | Solo software libre | ✅ | Acciones MIT, `dotnet-sonarscanner` LGPL-3.0, `coverlet.collector` MIT, Temurin GPLv2+CE y .NET 10 MIT. Licencias anotadas en `investigacion.md`. |
| V | El precio lo calcula el backend | N/A | Sin precios en esta historia. |
| VI | Pedidos idempotentes y webhooks | N/A | Sin pedidos ni webhooks. |
| VII | Puertos y adaptadores | ✅ | La CI no llama a proveedores externos reales; solo restaura paquetes y ejecuta pruebas. |
| VIII | Seguridad y secretos | ✅ | Cero secretos versionados; `SONAR_TOKEN` en GitHub Secrets, enmascarado y usado solo en el paso que lo necesita; permisos mínimos por flujo. |
| IX | Código de creadora | N/A | Sin código de creadora. |
| X | Diseños originales | N/A | Sin diseños. |
| XI | Idioma y estilo | ✅ | Nombres de flujos, jobs, pasos, documentación y comentarios en español; marca **BlackHellHeim**. |

Recomprobación tras la Fase 1: el diseño de tres flujos, permisos, secretos y
verificaciones requeridas mantiene todas las compuertas en verde. Sin violaciones que
justificar: la tabla de complejidad queda vacía.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/002-integracion-continua/
├── plan.md                    # Este archivo (salida de /speckit.plan)
├── investigacion.md           # Salida de la Fase 0 (/speckit.plan)
├── modelo-datos.md            # Salida de la Fase 1 (/speckit.plan)
├── inicio-rapido.md           # Salida de la Fase 1 (/speckit.plan)
├── contratos/
│   └── verificaciones-ci.md   # Contrato de verificaciones y permisos (Fase 1)
├── checklists/
│   └── requirements.md        # Lista de verificación de la especificación
└── tasks.md                   # Salida de /speckit.tasks (no lo crea este comando)
```

*Nombres en español conforme a AGENTS.md y al principio XI: `investigacion.md`
corresponde a `research.md` y `inicio-rapido.md` a `quickstart.md` de la plantilla.*

### Código fuente (raíz del repositorio)

```text
.github/
└── workflows/
    ├── integracion-continua.yml   # Restore, build y dotnet test
    ├── codeql.yml                 # Análisis estático de seguridad (C#)
    └── calidad-sonar.yml          # Análisis de calidad con SonarQube Cloud

.config/
└── dotnet-tools.json              # Fija dotnet-sonarscanner 12.1.0

BlackHellHeim.sln                  # Solución objetivo de restore/build/test
src/
└── Pruebas/
    └── BlackHellHeim.Pruebas.csproj  # Agrega coverlet.collector (MIT) para cobertura
```

**Decisión de estructura**: tres flujos separados en lugar de uno monolítico. Cada
flujo produce una verificación con nombre propio, se puede requerir de forma
independiente en la protección de rama y aísla los fallos: un problema de Sonar no
oculta el resultado de compilación y pruebas, y un fallo de compilación no impide que
CodeQL reporte su propio estado. Los nombres de los jobs son estables y coinciden con
las verificaciones requeridas documentadas en `contratos/verificaciones-ci.md`.

### Diseño de los flujos

| Flujo | Disparadores | Job(s) | Permisos | Secretos/variables |
|-------|--------------|--------|----------|-------------------|
| `integracion-continua.yml` | `pull_request` y `push` a `main` | `Compilación y pruebas` | `contents: read` | ninguno |
| `codeql.yml` | `pull_request` y `push` a `main` | `CodeQL (C#)` | `contents: read`, `security-events: write` | ninguno |
| `calidad-sonar.yml` | `pull_request` y `push` a `main` | `Detección de token de Sonar` y `SonarQube Cloud` | `contents: read` | `SONAR_TOKEN` (secreto), `SONAR_ORGANIZATION` y `SONAR_PROJECT_KEY` (variables) |

Reglas comunes:

- `concurrency` por flujo y referencia, con `cancel-in-progress: true`, para que solo la
  última ejecución del commit vigente cuente.
- `timeout-minutes: 10` por job.
- `actions/checkout` con `persist-credentials: false`.
- `actions/setup-dotnet` con `dotnet-version: 10.0.x`, `cache: true` y
  `cache-dependency-path: '**/*.csproj'`.
- Sin `paths-ignore`: incluso un pull request de solo documentación dispara las
  verificaciones y reporta un estado comprensible, en lugar de dejar una verificación
  requerida en pendiente.
- CodeQL se configura explícitamente para `csharp`; si en el futuro se agrega otro
  lenguaje, hay que ampliar `languages` (o la matriz) y la matriz de requisitos de
  `contratos/verificaciones-ci.md` para que la cobertura quede declarada.
- Ningún paso imprime secretos; el token de Sonar se pasa por variable de entorno y
  GitHub lo enmascara en los registros.

### Licencias de las dependencias nuevas (principio IV)

| Componente | Versión | Licencia |
|------------|---------|----------|
| `actions/checkout` | v7.0.1 | MIT |
| `actions/setup-dotnet` | v6.1.0 | MIT |
| `actions/setup-java` | v6.2.0 | MIT |
| `github/codeql-action` | v4.39.3 | MIT |
| `dotnet-sonarscanner` | 12.1.0 | LGPL-3.0 |
| `coverlet.collector` | 10.1.0 | MIT |
| Temurin JDK 21 | 21 (LTS) | GPLv2 con Classpath Exception |
| .NET 10 SDK / ASP.NET Core | 10.0.x | MIT |

Ninguna licencia comercial. La verificación detallada y las alternativas están en
`investigacion.md` (D11).

## Seguimiento de complejidad

> Solo se llena si la verificación de la constitución tuviera violaciones que justificar.

Sin violaciones: tabla vacía.
