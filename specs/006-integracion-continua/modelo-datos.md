# Modelo de datos (Fase 1): Integración continua del API

Historia 006 · Fecha: 2026-11-10

## Naturaleza de los datos

Esta historia no crea tablas ni persistencia propia. Los datos de la integración
continua viven en GitHub Actions (ejecuciones, jobs y checks) y en SonarQube Cloud
(análisis, métricas y compuerta de calidad). El modelo describe las entidades que la
especificación exige y cómo se representan en esas plataformas, para que los flujos y la
protección de rama hablen el mismo lenguaje.

## Entidades

### 1. Ejecución de verificación

Intento completo asociado a un commit o a un pull request.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `id_ejecucion` | identificador | sí | Único por flujo y ejecución; lo asigna GitHub |
| `commit` | SHA | sí | Commit exacto evaluado; un commit nuevo invalida el resultado anterior |
| `referencia` | cadena | sí | Rama o `refs/pull/<n>/merge` del evento |
| `flujo` | enumeración | sí | `integracion-continua`, `codeql` o `calidad-sonar` |
| `estado` | enumeración | sí | `pendiente`, `exitoso`, `fallido`, `omitido` |
| `inicio` / `fin` | fecha y hora | sí | El fin debe ser posterior al inicio |
| `duracion` | segundos | no | Se deriva de inicio y fin |
| `disparador` | enumeración | sí | `pull_request` o `push` |

**Transiciones**: `pendiente → exitoso`, `pendiente → fallido`,
`pendiente → omitido` (solo el job de Sonar sin token). Una ejecución cancelada por
`concurrency` no produce un resultado válido para el commit vigente; la ejecución nueva
es la que cuenta.

### 2. Verificación (check)

Resultado con nombre que GitHub publica en el pull request y que la protección de rama
puede exigir.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `nombre` | cadena | sí | Exactamente `Compilación y pruebas`, `CodeQL (C#)` o `SonarQube Cloud` |
| `ejecucion` | referencia | sí | Pertenece a una ejecución de verificación |
| `estado` | enumeración | sí | `pendiente`, `exitoso`, `fallido`, `omitido` |
| `detalle` | cadena | no | Enlace al registro o al análisis publicado |

**Reglas de validación**:
- Un pull request no puede integrarse si alguna verificación requerida está
  `pendiente` o `fallida` y la protección de rama está activa.
- `omitido` se reporta como éxito para la protección de rama, pero debe quedar visible
  en el pull request; nunca se convierte en un éxito silencioso.
- El nombre es estable: cambiarlo obliga a actualizar la protección de rama.

### 3. Hallazgo de seguridad

Resultado del análisis estático de CodeQL.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `id_hallazgo` | identificador | sí | Lo asigna el análisis |
| `regla` | cadena | sí | Consulta de CodeQL que lo detectó |
| `severidad` | enumeración | sí | `baja`, `media`, `alta`, `crítica` |
| `ubicacion` | ruta y línea | sí | Archivo y línea del código analizado |
| `estado` | enumeración | sí | `abierto`, `corregido`, `descartado` |
| `analisis` | referencia | sí | Ejecución de CodeQL que lo produjo |

**Reglas de validación**: todo hallazgo publicado debe tener severidad y ubicación; un
cambio sin hallazgos produce una verificación exitosa. Los hallazgos de un pull request
desde bifurcación también se publican en repositorios públicos, sin secretos.

### 4. Resultado de calidad

Estado de la compuerta de SonarQube Cloud para el cambio.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `proyecto` | cadena | sí | Clave `SONAR_PROJECT_KEY` del repositorio |
| `organizacion` | cadena | sí | `SONAR_ORGANIZATION` de SonarQube Cloud |
| `rama` / `pull_request` | cadena | sí | Contexto analizado |
| `compuerta` | enumeración | sí | `aprobada` o `rechazada` |
| `metricas` | objeto | no | Cobertura, duplicación, deuda técnica y fiabilidad |
| `informe_cobertura` | ruta | no | `**/TestResults/**/coverage.cobertura.xml` |
| `estado` | enumeración | sí | `pendiente`, `exitoso`, `fallido`, `omitido` |

**Reglas de validación**: el job espera la compuerta con
`sonar.qualitygate.wait=true`; si no hay token, el resultado es `omitido` y visible. Si
el token está vencido o revocado, el resultado es `fallido` o `omitido` explícito,
nunca `exitoso`.

### 5. Credencial

Valor sensible necesario para un análisis externo.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `nombre` | cadena | sí | `SONAR_TOKEN` |
| `almacen` | enumeración | sí | Secreto del repositorio en GitHub |
| `alcance` | cadena | sí | Solo el paso que ejecuta el scanner |
| `vigencia` | fecha | no | Se rota si se sospecha exposición |
| `expuesta_en_repositorio` | booleano | sí | Siempre `false` |

**Reglas de validación**: el valor nunca se versiona, imprime, registra ni incluye en
artefactos; GitHub lo enmascara en los registros. Una credencial ausente produce
`omitido`; una vencida o revocada produce fallo visible.

### 6. Variable de configuración

Dato no sensible que parametriza el análisis de calidad.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `nombre` | cadena | sí | `SONAR_ORGANIZATION` o `SONAR_PROJECT_KEY` |
| `valor` | cadena | sí | Identificadores públicos de SonarQube Cloud |
| `almacen` | enumeración | sí | Variable del repositorio en GitHub |

**Reglas de validación**: si falta una variable, el job de Sonar falla de forma visible;
no se asume un valor por omisión que pueda analizar el proyecto equivocado.

## Relaciones

```text
Ejecución de verificación 1 ── N Verificación
Ejecución de verificación 1 ── N Hallazgo de seguridad   (solo flujo CodeQL)
Ejecución de verificación 1 ── 1 Resultado de calidad    (solo flujo Sonar)
Credencial 1 ── N Resultado de calidad                   (habilita el análisis)
Variable de configuración 1 ── N Resultado de calidad    (parametriza el análisis)
```

## Datos que no se almacenan aquí

- Contenido del código, artefactos de compilación y paquetes NuGet: viven en el
  workspace efímero del ejecutor y en la caché de Actions.
- Métricas históricas de calidad: viven en SonarQube Cloud.
- Alertas históricas de seguridad: viven en GitHub code scanning.
- Resultados de pruebas: se muestran en el registro del job; no se publican como
  artefacto en esta historia.

## Fuera de alcance

No hay entidades de dominio, migraciones de EF Core ni base de datos. Cualquier
persistencia de negocio llega en su historia propia de la fundación.
