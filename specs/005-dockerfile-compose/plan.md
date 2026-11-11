# Plan de implementación: Imagen de contenedor del API y entorno local con un solo comando

**Rama**: `sprint/01` | **Fecha**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de funcionalidad en `specs/005-dockerfile-compose/spec.md`

**Historia del backlog**: HU5 (componentes BE y CI)

## Resumen

La historia 005 empaqueta el API en una imagen de contenedor multietapa y extiende el
`compose.yaml` de la historia 003 con el servicio del API, de modo que un solo comando
(`./scripts/entorno.sh`, que envuelve `docker compose up --build --wait`) construya la imagen,
levante SQL Server, aplique las migraciones y deje el API sano.

Decisiones principales (detalle y alternativas en [investigacion.md](investigacion.md)):

- **Dockerfile multietapa** con imágenes oficiales de Microsoft para .NET 10 fijadas por
  *digest*: etapa `compilacion` (SDK) → etapa `final` (`aspnet`, usuario `app` UID 1654, sin SDK
  ni fuentes). Restauración con `--locked-mode` y `packages.lock.json` para que la
  construcción sea reproducible.
- **Migraciones con un paquete de EF Core (*bundle*)** generado en el mismo Dockerfile
  (etapa `migraciones`) y ejecutado por un servicio Compose de un solo uso. Así el comando
  único deja la base migrada sin SDK en el host ni montar el repositorio (la objeción que
  la HU3 tenía contra un servicio de migración).
- **Dependencia por salud**: `api` espera a `migraciones` (`service_completed_successfully`),
  que a su vez espera a `sqlserver` (`service_healthy`).
- **Healthchecks**: SQL Server con `sqlcmd` (`SELECT 1`); API con una sonda HTTP de bash
  (`/dev/tcp`, sin instalar `curl`) contra `GET /salud/base` (listo). `GET /salud` queda como
  prueba de vida. **No se agregan endpoints ni se cambia su comportamiento.**
- **Secretos**: solo por `.env` (ignorado por git) reutilizando los nombres de la HU3;
  Compose falla con `${VAR:?}` nombrando la variable faltante sin mostrar valores.
- **Verificación automática**: pruebas estáticas en xUnit (Dockerfile, Compose, `.env.example`,
  `.gitignore`, secretos) y un script de humo con Docker (`scripts/verificar-entorno.sh`) que
  comprueba salud, caída y recuperación de la base, idempotencia, imagen y reproducibilidad.

**Prerrequisito**: las historias 002b y 003 deben estar implementadas. Hoy el repositorio **no**
contiene `compose.yaml`, `.env.example`, `scripts/base-datos.sh`, `ContextoBlackHellHeim`,
`FabricaContextoDiseno` ni `/salud/base` (el `Program.cs` actual solo expone `/salud`). Si al
ejecutar `/speckit.implement` siguen sin existir, la implementación se detiene y se avisa; no se
reimplementan ni se duplican.

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (destino `net10.0`); scripts en Bash; Dockerfile y
Compose.

**Dependencias principales** (licencias verificadas; detalle en `investigacion.md`):

| Componente | Versión | Licencia | Uso |
|------------|---------|----------|-----|
| `mcr.microsoft.com/dotnet/sdk` | 10.0 (SDK 10.0.402) | MIT (.NET) | Etapa de compilación, solo en construcción |
| `mcr.microsoft.com/dotnet/aspnet` | 10.0 (10.0.13) | MIT (.NET); paquetes de Ubuntu 24.04 con sus licencias | Imagen final |
| `mcr.microsoft.com/mssql/server` | 2022-latest | EULA de Microsoft, Developer gratuita para desarrollo y pruebas | Base local (ya fijada por la HU3) |
| `dotnet-ef` | 10.0.15 | MIT | Generar el *bundle* de migraciones (herramienta de la HU3) |
| Docker Engine / Compose v2 | Engine 29.x, Compose 5.x | Apache 2.0 | Orquestación local |
| Paquetes NuGet nuevos | — | — | **Ninguno.** Solo se activan archivos de bloqueo (`packages.lock.json`) |

Sin MediatR 13+, AutoMapper, FluentAssertions ni paquetes comerciales. Docker Desktop **no** es
requisito (su licencia es comercial para empresas grandes); se usa Docker Engine + plugin
Compose, o cualquier runtime compatible con ambos.

**Almacenamiento**: SQL Server 2022 de la HU3 con volumen nombrado `datos-sqlserver`. Sin
Azurite ni archivos.

**Pruebas**: xUnit en `src/Pruebas` más un script de humo:
1. Estáticas (sin Docker, corren con `dotnet test`): `Dockerfile`, `compose.yaml`,
   `.env.example`, `.gitignore`, `.dockerignore` y búsqueda de secretos en archivos versionados.
2. Humo con Docker (`scripts/verificar-entorno.sh`): estado sano, caída/recuperación de la
   base, idempotencia, imagen y reproducibilidad.
3. Las pruebas existentes (arquitectura, `/salud`, `/salud/base`) siguen en verde.

**Plataforma destino**: Linux/macOS con Docker Engine + Compose v2, arquitectura `linux/amd64`
verificada; `linux/arm64` soportada por `TARGETARCH` pero **no verificada** en esta historia.

**Tipo de proyecto**: servicio web (API) con arquitectura limpia; esta historia es de
empaquetado y no toca capas de código de negocio.

**Metas de rendimiento**: entorno limpio sano en < 10 min (incluye descargas) y en caliente en
< 3 min (CE-001).

**Restricciones**:
- Sin credenciales en archivos versionados, en la imagen ni en los registros.
- Sin despliegue, publicación de imágenes, CI, endpoints ni cambios de esquema.
- Imagen final sin SDK, fuentes, `obj/`, `bin/` ni `appsettings.Development.json`.
- Código, scripts, comentarios y documentación en español; marca **BlackHellHeim**.

**Escala/Alcance**: 3 servicios Compose (`sqlserver`, `migraciones`, `api`), 1 Dockerfile de 3
etapas, 3 scripts, 1 `.dockerignore`, 1 `global.json`, archivos de bloqueo NuGet y alrededor de
10 pruebas nuevas.

## Verificación de la constitución

*COMPUERTA: debe pasar antes de la investigación de la Fase 0. Se vuelve a comprobar tras el
diseño de la Fase 1.*

| # | Principio | Estado | Nota |
|---|-----------|--------|------|
| I | Arquitectura limpia | ✅ | No se crean proyectos ni se cambian referencias. El `Dockerfile` copia los cinco proyectos tal cual; la prueba de arquitectura sigue esperando cinco. |
| II | Contrato primero | ✅ | No hay endpoints nuevos ni cambios de contrato. `contratos/entorno.md` define el contrato operativo (comando, variables, salud, códigos de salida) antes de implementarlo. |
| III | Reglas de negocio probadas | ✅ | Sin reglas de negocio. Las reglas del entorno (sin secretos, usuario sin privilegios, dependencia por salud, variable faltante) tienen pruebas de éxito, error y validación. |
| IV | Solo software libre | ✅ | Imágenes .NET y herramientas MIT/Apache 2.0; SQL Server Developer es gratuita bajo EULA (justificada en HU3). Cero paquetes NuGet nuevos. Licencias anotadas arriba y en `investigacion.md`. |
| V | El precio lo calcula el backend | N/A | Sin precios. |
| VI | Pedidos idempotentes y webhooks | N/A | Sin pedidos ni webhooks. |
| VII | Puertos y adaptadores | N/A | Sin proveedores externos. Azurite queda fuera de alcance. |
| VIII | Seguridad y secretos | ✅ | `.env` ignorado; `.env.example` con marcadores; `${VAR:?}` sin eco de valores; usuario `app`; imagen sin secretos; búsqueda automatizada de secretos. |
| IX | Código de creadora | N/A | Sin códigos de creadora. |
| X | Diseños originales | N/A | Sin diseños. |
| XI | Idioma y estilo | ✅ | Todo en español; marca **BlackHellHeim**. |

Recomprobación tras la Fase 1: sin cambios. Seguimiento de complejidad vacío. La CI y el
análisis estático de la Definición de Hecho corresponden a la HU6 (fuera de alcance).

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/005-dockerfile-compose/
├── plan.md               # Este archivo (salida de /speckit.plan)
├── investigacion.md      # Fase 0: decisiones, alternativas y licencias
├── modelo-datos.md       # Fase 1: entidades operativas, variables y estados de salud
├── inicio-rapido.md      # Fase 1: guía de validación de punta a punta
├── contratos/
│   └── entorno.md        # Contrato operativo (sin endpoints nuevos)
├── checklists/
│   └── requirements.md   # Ya existente
└── tasks.md              # Salida de /speckit.tasks (no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
Dockerfile                        # Nuevo: etapas compilacion → migraciones, final
.dockerignore                     # Nuevo: excluye .git, .env, bin/, obj/, specs/, docs/
compose.yaml                      # Modificado (HU3): + servicios migraciones y api
.env.example                      # Modificado (HU3): + puerto del API, solo marcadores
.gitignore                        # Verificado: .env ya ignorado (HU3)
global.json                       # Nuevo: fija el SDK 10.0.402 (rollForward latestPatch)
Directory.Build.props             # Nuevo: bloqueo NuGet y compilación determinista
docker/
└── sonda-http.sh                 # Sonda de salud en bash, sin curl
scripts/
├── base-datos.sh                 # HU3: se conserva sin cambios
├── entorno.sh                    # Nuevo: comando único (valida, up --build --wait, informa)
└── verificar-entorno.sh          # Nuevo: humo con Docker
src/
├── Api/packages.lock.json        # Generado por restore
├── Aplicacion/packages.lock.json # Generado por restore
├── Dominio/packages.lock.json    # Generado por restore
├── Infraestructura/packages.lock.json
└── Pruebas/
    └── Entorno/
        ├── DockerfilePruebas.cs
        ├── ComposePruebas.cs
        ├── ConfiguracionSinSecretosPruebas.cs
        └── RaizRepositorio.cs     # Localiza la raíz para leer archivos
```

**Decisión de estructura**: sin proyectos nuevos. Los artefactos de contenedor viven en la raíz
(convención de Docker); los scripts, en `scripts/` junto al de la HU3; las pruebas estáticas, en
`src/Pruebas/Entorno/`.

## Diseño

### Dockerfile (resumen; el código completo es parte de la implementación)

1. `ARG` de imágenes con *digest* (SDK y aspnet) declarados al inicio.
2. Etapa `restauracion`: copia `*.sln`, `global.json`, `Directory.Build.props`,
   `.config/dotnet-tools.json`, los `.csproj` y los `packages.lock.json`; ejecuta
   `dotnet restore --locked-mode` (la capa se reutiliza como caché mientras no cambien).
3. Etapa `compilacion`: copia el código y ejecuta `dotnet publish src/Api -c Release
   --no-restore -p:UseAppHost=false -o /app/publicar`. Genera también el *bundle* de
   migraciones con `dotnet ef migrations bundle --self-contained -r linux-${arch}` (ver D3).
4. Etapa `migraciones` (base `mcr.microsoft.com/dotnet/runtime-deps:10.0` por *digest*, usuario
   `app`): solo contiene el *bundle*; el *entrypoint* lee la cadena de la variable de entorno.
5. Etapa `final` (base `aspnet` por *digest*): copia `/app/publicar` y `docker/sonda-http.sh`
   (`--chmod=555`), `USER app`, `EXPOSE 8080`, `ENV ASPNETCORE_ENVIRONMENT=Production`,
   `HEALTHCHECK` opcional replicado en Compose. Sin SDK, sin fuentes, sin `appsettings.Development.json`
   (se elimina en la publicación). Etiquetas OCI: `org.opencontainers.image.source`,
   `.version`, `.revision` (con `ARG VERSION`/`REVISION`), `.licenses=MIT`.

### Compose (extensión del de la HU3)

```text
sqlserver   (HU3, sin cambios funcionales)
  healthcheck: sqlcmd -C -Q "SELECT 1" con SQLCMDPASSWORD=$MSSQL_SA_PASSWORD (no -P)
  interval 10s, timeout 5s, retries 12, start_period 20s

migraciones  (nuevo, un solo uso)
  build: { context: ., target: migraciones }
  depends_on: sqlserver: { condition: service_healthy }
  environment: ConnectionStrings__BlackHellHeim = Server=sqlserver,1433;...${BHH_SQL_SA_PASSWORD:?...}
  restart: "no"

api          (nuevo)
  build: { context: ., target: final }
  depends_on: sqlserver: service_healthy; migraciones: service_completed_successfully
  environment: ConnectionStrings__BlackHellHeim (misma cadena, host sqlserver)
  ports: ${BHH_API_PUERTO:-5068}:8080
  healthcheck: ["CMD","/app/sonda-http.sh","/salud/base"]
  interval 10s, timeout 5s, retries 6, start_period 20s
  restart: unless-stopped
```

- La cadena de conexión dentro de Compose usa el host `sqlserver`. El `.env` de la HU3 (con
  `localhost`) sigue sirviendo para `dotnet run` y `base-datos.sh`; Compose **sobrescribe** esa
  variable para sus servicios construyéndola con `BHH_SQL_SA_PASSWORD`. No se renombra ni se
  duplica ninguna variable de la HU3; solo se agrega `BHH_API_PUERTO` (opcional).
- Restricción documentada: la contraseña no debe contener `;`, `=` ni comillas, para no romper
  la cadena de conexión.
- `docker compose up --wait` termina con error si algún servicio no queda sano, lo que cubre
  los escenarios de fallo (escenario 5 de la HU 1).

### Salud

- **Vida**: `GET /salud` (sin base) — sin cambios; es la sonda de vida del API.
- **Lista**: `GET /salud/base` (HU3) — 200 `disponible` / 503 ProblemDetails con tiempo máximo
  de 3 s. El `healthcheck` del contenedor usa esta ruta, de modo que el API no es "sano" con la
  base caída y vuelve a serlo al recuperarse, sin reiniciar (HU 2).
- **Esquema al día (RF-009)**: lo garantiza el orden de arranque: `api` no inicia hasta que
  `migraciones` termina con éxito, y el humo compara `__EFMigrationsHistory` con las
  migraciones del *bundle*. Se **descarta** ampliar `/salud/base` para detectar migraciones
  pendientes, porque la spec prohíbe cambiar el comportamiento de esas rutas y nuevos
  endpoints. Queda anotado como limitación en `investigacion.md` (D6).
- **Espera acotada (RF-010)**: el API no conecta al arrancar (HU3); el reintento lo hace el
  `healthcheck` (≈ 80 s: `start_period` 20 s + 6 reintentos de 10 s). El servicio SQL Server
  tiene su propio margen (≈ 140 s). Pasado el margen, `up --wait` falla y lo informa.

### Comando único: `./scripts/entorno.sh`

1. Verifica que Docker y Compose v2 existan y que el daemon responda (RF-018).
2. Verifica `.env` y que no conserve los marcadores de `.env.example`; nombra la variable
   faltante sin imprimir valores (RF-014).
3. Comprueba puertos 1433 y `BHH_API_PUERTO` libres (si no son de este proyecto) (RF-018).
4. Ejecuta `docker compose up --build --wait --remove-orphans`.
5. Informa servicios sanos y la dirección `http://localhost:${BHH_API_PUERTO:-5068}`; con
   fallo, muestra `docker compose ps` y las últimas líneas de registro **filtrando** cadenas de
   conexión, y sale con código distinto de cero.
6. Es idempotente (ejecutarlo otra vez no falla ni duplica semillas; el *bundle* es idempotente).
7. `./scripts/entorno.sh --recrear` hace `down -v` tras pedir confirmación explícita (RF-017).

### Pruebas de verificación

**Estáticas (xUnit, sin Docker)** en `src/Pruebas/Entorno/`:

| Prueba | Tipo | Comprueba |
|--------|------|-----------|
| Dockerfile fija bases por `@sha256` | éxito/error | Ninguna `FROM` con etiqueta suelta |
| Dockerfile final usa `USER app` y no `root` | éxito/error | Última `USER` ≠ `root` |
| Dockerfile final sin SDK ni `COPY . .` en la etapa final | error | Etapa final solo copia publicación |
| Restore con `--locked-mode` | éxito | Presente en el Dockerfile |
| Compose: `api` depende de `sqlserver` (`service_healthy`) y de `migraciones` | éxito/error | Condiciones presentes |
| Compose: healthcheck en `sqlserver` y `api` | éxito/error | Ambos definidos |
| Compose: variables con `${...:?}` y sin contraseñas literales | validación | Sin valores reales |
| `.env.example` solo marcadores; `.env` en `.gitignore`/`.dockerignore` | éxito/error | Sin valores reales |
| Búsqueda de secretos en archivos versionados (`git ls-files`) | validación | 0 coincidencias |
| `Program.cs` y contratos sin cambios de endpoints | regresión | Pruebas existentes en verde |

**Humo con Docker** (`scripts/verificar-entorno.sh`, requiere Docker; lo ejecutará la CI en la
HU6):

1. `up --build --wait`: tres servicios en el estado esperado, API y base `healthy`.
2. `GET /salud` y `GET /salud/base` → 200.
3. `stop sqlserver`: `/salud/base` → 503, `/salud` → 200, contenedor `api` pasa a `unhealthy`;
   `start sqlserver`: vuelve a `healthy` sin reiniciar `api`.
4. Segunda ejecución del comando: sin error y `SELECT COUNT(*) FROM dbo.Tiendas` = 2.
5. Variable obligatoria ausente: falla nombrando la variable, sin eco del valor.
6. Imagen: `Config.User` = `app`/`1654`; no existe `dotnet --list-sdks`; sin `.env`,
   `appsettings.Development.json` ni `obj/`; sin la contraseña en `docker history`/`inspect`
   de la imagen ni en los registros.
7. Reproducibilidad: construir dos veces con `--no-cache` y comparar el hash SHA-256 de los
   archivos de `/app` (los `.dll` son deterministas) y las versiones de `.deps.json`.
8. Registros: ni `docker compose logs` ni la salida del comando contienen la contraseña ni la
   cadena de conexión completa.

## Fases de diseño

### Fase 0: investigación

Salida: `investigacion.md`. Decisiones D1–D9: imágenes base y *digests*, usuario sin
privilegios, migraciones con *bundle*, sonda de salud sin `curl`, reproducibilidad, salud y
límites de RF-009, secretos y Compose, entorno de ejecución del contenedor, licencias. No
quedan `NEEDS CLARIFICATION`.

### Fase 1: diseño y contratos

Salidas: `modelo-datos.md`, `contratos/entorno.md`, `inicio-rapido.md`.

### Fase 2: tareas

`tasks.md` se genera con `/speckit.tasks`; no lo crea este comando.

## Riesgos y pendientes

- **Dependencia dura de 002b y 003**: no existen en el árbol actual (ver Prerrequisito).
- **Digests**: los registrados se tomaron de las imágenes locales; deben revalidarse con el
  registro al implementar (`docker pull` y `docker image inspect`) y actualizarse a propósito.
- **Bundle de EF Core con `FabricaContextoDiseno`**: se debe comprobar en la primera tarea que
  el *bundle* usa la variable de entorno en tiempo de ejecución. Plan de contingencia: servicio
  `migraciones` basado en la etapa SDK con `dotnet ef database update` (más pesado), o seguir
  usando `scripts/base-datos.sh` desde el host.
- **arm64**: no verificado; se declara y se informa claramente si falla.
- **Backlog**: `docs/scrum/backlog.md` solo lista las HU 2 y 6 en lo revisado; se debe añadir
  o confirmar la fila de la HU5 y que pertenece a `sprint/01`.

## Seguimiento de complejidad

Sin violaciones que justificar. La tabla queda vacía.
