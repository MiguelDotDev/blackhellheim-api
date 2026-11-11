# Investigación (Fase 0): Integración continua del API

Historia 006 · Fecha: 2026-11-10

El contexto técnico llegó resuelto en la descripción de la usuaria (GitHub Actions,
.NET 10, CodeQL para C#, SonarQube Cloud, secretos fuera del repositorio, acciones
oficiales fijadas con licencia y menos de 10 minutos). No quedan NEEDS CLARIFICATION.
Esta fase documenta las decisiones derivadas, sus alternativas y las licencias
verificadas.

## D1. Tres flujos separados en lugar de uno monolítico

**Decisión**: crear `.github/workflows/integracion-continua.yml`,
`.github/workflows/codeql.yml` y `.github/workflows/calidad-sonar.yml`, cada uno con su
propio job y su propio nombre de verificación.

**Justificación**: la protección de rama exige verificaciones por nombre; separarlas
permite requerir «Compilación y pruebas», «CodeQL (C#)» y «SonarQube Cloud» de forma
independiente. Además, un fallo en un análisis no impide ver el estado de los demás y
cada flujo se puede reintentar o ajustar sin tocar los otros. Los tres se ejecutan en
paralelo, así que el tiempo total no se suma.

**Alternativas descartadas**: un solo flujo con tres jobs (funciona, pero acopla
disparadores, permisos y nombres en un archivo y dificulta requerir checks por
separado) y un solo job con todos los pasos (un fallo de Sonar impediría saber si la
compilación pasó y los permisos de escritura de seguridad quedarían disponibles para
pasos que no los necesitan).

## D2. Acciones oficiales fijadas a SHA completo, con licencia anotada

**Decisión**: usar solo acciones de GitHub o del proveedor y fijar cada una a un SHA
completo de commit, con un comentario que indique la versión legible.

| Acción | Versión | SHA fijado | Licencia |
|--------|---------|------------|----------|
| `actions/checkout` | v7.0.1 | `3d3c42e5aac5ba805825da76410c181273ba90b1` | MIT |
| `actions/setup-dotnet` | v6.1.0 | `0f4a354dd5547a28c70b7853dbd95d5b67886e73` | MIT |
| `actions/setup-java` | v6.2.0 | `77dd5b2ce225ea11469928ac317850245a74ae78` | MIT |
| `github/codeql-action` (`init` y `analyze`) | v4.39.3 | `33d7b85da1a04171e211befe2c739303b9a4c066` | MIT |

**Justificación**: un SHA es inmutable; una etiqueta móvil podría cambiar de contenido
sin revisión. Las cuatro acciones son de GitHub y tienen licencia MIT, compatible con el
principio IV. La versión legible en el comentario facilita actualizarlas con revisión.

**Alternativas descartadas**: usar `@v7`, `@v6` o `@v4` (cómodo, pero una etiqueta puede
reapuntarse y rompe la reproducibilidad) y acciones de terceros no oficiales (mayor
superficie de confianza y licencias por verificar).

## D3. CodeQL con configuración avanzada y compilación manual

**Decisión**: usar `github/codeql-action/init` con `languages: csharp` y
`queries: security-extended`, compilar la solución manualmente después de `init` y
cerrar con `github/codeql-action/analyze`. Permisos del job: `contents: read` y
`security-events: write`.

**Justificación**: C# requiere compilación para que CodeQL observe el código generado y
las referencias; `security-extended` amplía la cobertura de seguridad respecto del
conjunto por defecto. La configuración avanzada por flujo es la única que permite fijar
la acción a SHA y elegir el conjunto de consultas. La documentación de GitHub indica que
la carga de resultados de code scanning siempre se permite cuando el evento
`pull_request` dispara la ejecución, aunque `security-events: write` se degrade a
lectura para el token de una bifurcación; por eso el análisis funciona en
contribuciones externas sin secretos.

**Alternativas descartadas**: la configuración predeterminada de CodeQL (no permite
fijar la acción a SHA ni elegir consultas y no se puede combinar con la configuración
avanzada), `queries: security-and-quality` (más lento y solapa con Sonar; se prefiere
seguridad en CodeQL y calidad en Sonar) y `autobuild` (menos control sobre la
compilación y sin caché explícita de NuGet).

## D4. SonarQube Cloud con `dotnet-sonarscanner`, no con la acción de SonarSource

**Decisión**: instalar `dotnet-sonarscanner` 12.1.0 como herramienta local del
repositorio en `.config/dotnet-tools.json` y ejecutar
`dotnet sonarscanner begin` → `dotnet build` → `dotnet test` → `dotnet sonarscanner end`
en el job de calidad. Usar Temurin JDK 21 con `actions/setup-java`.

**Justificación**: la documentación oficial de `SonarSource/sonarqube-scan-action`
indica que esa acción no sirve para soluciones .NET y remite al SonarScanner for .NET.
La herramienta NuGet es la vía oficial para C# y permite fijar la versión en el
repositorio. El manifiesto de herramientas hace reproducible la instalación y evita
depender de una versión global del ejecutor. El scanner requiere Java; Temurin 21 es la
opción soportada y gratuita.

**Alternativas descartadas**: `SonarSource/sonarcloud-github-action` (deprecada y no
apta para .NET), `SonarSource/sonarqube-scan-action` (no analiza soluciones .NET de
forma correcta), instalar la herramienta de forma global sin versión fija
(irreproducible) y ejecutar el scanner desde una imagen de contenedor (más peso y sin
ventaja para este tamaño).

## D5. Omisión visible del análisis de Sonar cuando no hay token

**Decisión**: un job `Detección de token de Sonar` lee `secrets.SONAR_TOKEN` en una
variable de entorno, escribe `disponible=true|false` en `GITHUB_OUTPUT` y el job
`SonarQube Cloud` declara `needs` sobre él y `if: needs.detectar-token.outputs.disponible == 'true'`.
Cuando el token no existe (pull request desde una bifurcación), el job de análisis se
omite y GitHub lo reporta como «Success» visible, sin ejecutar el scanner ni exponer
nada.

**Justificación**: el contexto `secrets` no se puede evaluar directamente en un `if` de
job; el patrón de job previo con salida es la forma soportada de condicionar sin
exponer el valor. GitHub documenta que un job omitido por condición reporta «Success» y
satisface una verificación requerida, lo que permite mantener «SonarQube Cloud» como
check obligatorio sin bloquear bifurcaciones.

**Alternativas descartadas**: `if: secrets.SONAR_TOKEN != ''` (el contexto no está
disponible en ese punto), `pull_request_target` (daría secretos a código de
bifurcaciones, riesgo de seguridad inaceptable) y fallar el job cuando no hay token
(castigaría contribuciones externas legítimas y contradice el RF-011).

## D6. Caché de NuGet y objetivo de 10 minutos

**Decisión**: usar `cache: true` y `cache-dependency-path: '**/*.csproj'` en
`actions/setup-dotnet` en los tres flujos, compilar en `Release`, ejecutar
`dotnet test --no-build` después de compilar y limitar cada job con
`timeout-minutes: 10`.

**Justificación**: la caché de paquetes evita descargar NuGet en cada ejecución y es la
mayor ganancia para el tamaño actual. Compilar una vez y probar con `--no-build` evita
duplicar la compilación. Los tres flujos corren en paralelo, por lo que el tiempo de
pared no es la suma. `timeout-minutes` convierte una ejecución colgada en un fallo
visible en lugar de un pendiente indefinido.

**Alternativas descartadas**: caché manual con `actions/cache` (más configuración y
clave que mantener; `setup-dotnet` ya la implementa), `dotnet test` sin compilación
previa (recompila y duplica trabajo) y un solo flujo secuencial (suma tiempos y supera
el objetivo con facilidad).

## D7. Permisos mínimos y concurrencia

**Decisión**: `permissions: contents: read` en la parte superior de cada flujo; el job
de CodeQL añade solo `security-events: write`. Ningún otro permiso de escritura.
`concurrency` por flujo y referencia con `cancel-in-progress: true`. Checkout con
`persist-credentials: false`.

**Justificación**: el principio VIII y el RF-013 exigen el mínimo privilegio. Escribir
resultados de seguridad es la única necesidad de escritura. Cancelar ejecuciones
antiguas evita gastar minutos en commits obsoletos y deja como resultado el del commit
vigente. `persist-credentials: false` impide que el token quede en la configuración de
Git del workspace.

**Alternativas descartadas**: `permissions: write-all` (viola el mínimo privilegio),
dejar los permisos por defecto del repositorio (pueden ser de escritura y cambian según
la configuración) y `cancel-in-progress: false` (acumula ejecuciones y retrasa el
resultado del commit actual).

## D8. Cobertura para la compuerta de calidad de Sonar

**Decisión**: agregar `coverlet.collector` 10.1.0 (MIT) al proyecto
`BlackHellHeim.Pruebas`, ejecutar `dotnet test --collect:"XPlat Code Coverage"` en el
job de Sonar y publicar el informe Cobertura con
`sonar.cs.cobertura.reportsPaths=**/TestResults/**/coverage.cobertura.xml`. La compuerta
se espera con `sonar.qualitygate.wait=true` y `sonar.qualitygate.timeout=300`.

**Justificación**: la compuerta «Sonar way» evalúa cobertura sobre código nuevo; sin
informe, la cobertura se interpreta como 0 % y el análisis fallaría por una causa
artificial. `coverlet.collector` es el colector estándar de .NET, tiene licencia MIT y no
requiere cambiar el marco de pruebas. Esperar la compuerta dentro del job hace que el
resultado sea el del check y no una consulta posterior.

**Alternativas descartadas**: no medir cobertura y desactivar esa condición de la
compuerta (debilita la calidad y oculta una métrica útil), usar `dotnet-coverage` de
Microsoft (herramienta adicional con otra licencia y formato) y calcular la cobertura en
el flujo de pruebas y pasarla como artefacto a Sonar (complejidad entre flujos sin
ventaja real).

## D9. Protección de rama y verificaciones requeridas

**Decisión**: documentar y activar la protección de `main` con las verificaciones
requeridas `Compilación y pruebas`, `CodeQL (C#)` y `SonarQube Cloud`. No se requiere el
job `Detección de token de Sonar`.

**Justificación**: los nombres coinciden exactamente con los jobs, que es como GitHub
publica los checks. Un job omitido reporta «Success», así que `SonarQube Cloud` puede
ser obligatorio y omitirse en bifurcaciones sin bloquear. La detección de token es un
detalle interno y no aporta valor como puerta.

**Alternativas descartadas**: requerir todos los jobs (incluida la detección, sin valor
para la revisión) y no activar la protección (el flujo solo informaría y un cambio roto
podría integrarse, incumpliendo el RF-004).

## D10. Versión de .NET en el flujo

**Decisión**: `dotnet-version: 10.0.x` en `actions/setup-dotnet`, sin `global.json`.

**Justificación**: los proyectos fijan `net10.0`; la banda `10.0.x` instala la última
revisión disponible y coincide con el SDK local 10.0.114. Un `global.json` estricto
podría bloquear el entorno local o exigir actualizaciones coordinadas sin beneficio para
esta historia.

**Alternativas descartadas**: `dotnet-version: 10.0.114` (fija un parche que envejece y
obliga a actualizar el flujo en cada revisión) y `global.json` con una versión exacta
(misma desventaja y un archivo más que mantener).

## D11. Licencias verificadas (principio IV)

| Componente | Versión | Licencia | Verificación |
|------------|---------|----------|--------------|
| `actions/checkout` | v7.0.1 | MIT | API de licencias de GitHub |
| `actions/setup-dotnet` | v6.1.0 | MIT | API de licencias de GitHub |
| `actions/setup-java` | v6.2.0 | MIT | API de licencias de GitHub |
| `github/codeql-action` | v4.39.3 | MIT | API de licencias de GitHub |
| `dotnet-sonarscanner` | 12.1.0 | LGPL-3.0 | `LICENSE.txt` del paquete NuGet |
| `coverlet.collector` | 10.1.0 | MIT | Nuspec del paquete NuGet |
| Temurin JDK 21 | 21 (LTS) | GPLv2 con Classpath Exception | Distribución Eclipse Temurin |
| .NET 10 SDK / ASP.NET Core | 10.0.x | MIT | Repositorios `dotnet` y `aspnetcore` |

Ningún componente comercial; sin MediatR 13+, AutoMapper ni FluentAssertions. La
herramienta de Sonar es la vía oficial y gratuita para repositorios públicos.

## D12. Detalles menores

- **Nombres de flujos y jobs**: en español y estables; los jobs se llaman como las
  verificaciones requeridas. Los archivos usan `kebab-case` en español.
- **Rama principal**: `main`, tanto para `push` como para la base de `pull_request`.
- **Sin `paths-ignore`**: un pull request de solo documentación también ejecuta las
  verificaciones; así la protección de rama nunca queda con un check pendiente.
- **Sin despliegue**: ningún flujo publica la API, aprovisiona base de datos o
  almacenamiento ni publica OpenAPI (RF-015).
- **CodeQL y Sonar automáticos**: la configuración predeterminada de CodeQL y el
  análisis automático de Sonar se desactivan en la interfaz del repositorio para que no
  compitan con los flujos de esta historia; el paso manual queda en `inicio-rapido.md`.
