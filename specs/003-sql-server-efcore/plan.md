# Plan de implementación: Base de datos local con migraciones y datos semilla

**Rama**: `sprint/01` | **Fecha**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de funcionalidad en `specs/003-sql-server-efcore/spec.md`

**Historia del backlog**: HU3 (componentes BE y DB)

## Resumen

La historia 003 incorpora SQL Server como base de datos local en Docker Compose, con el
esquema gestionado por migraciones de EF Core en el proyecto `Infraestructura`. Un comando
único (`./scripts/base-datos.sh`) levanta el contenedor, espera a que esté sano y aplica la
migración inicial con dos tiendas de referencia (`BHH` y `DEM`). La cadena de conexión y la
contraseña viven en un `.env` ignorado por git; el repositorio solo incluye `.env.example`
con marcadores de posición. La comprobación de salud existente (`/salud`) sigue siendo de
vida del API; se añade `/salud/base`, que informa la disponibilidad de la base con un
tiempo máximo de espera. Las pruebas de persistencia usan un contenedor real de SQL Server
mediante Testcontainers (MIT) y verifican migrar, sembrar, revertir y volver a aplicar.

**Prerrequisito**: la historia 002b debe estar implementada (tipos `Tienda` y
`CodigoTienda` en `Dominio`, y `IContextoEjecucion` con el middleware de correlación y
ProblemDetails en `Api`). Si no lo está, esta historia se detiene y se avisa; no se duplican
esos tipos.

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (destino `net10.0`).

**Dependencias principales**:
- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 (MIT, verificado) en `Infraestructura`.
- `Microsoft.EntityFrameworkCore.Design` 10.0.15 (MIT, verificado) en `Infraestructura`,
  como dependencia de desarrollo (`PrivateAssets=all`), para las migraciones.
- `dotnet-ef` 10.0.15 (MIT, verificado) como herramienta local en
  `.config/dotnet-tools.json`.
- Pruebas: `Testcontainers.MsSql` 4.16.0 (MIT, verificado), `xunit` 2.9.3 (Apache 2.0),
  `Microsoft.NET.Test.Sdk` 18.11.1 (MIT) y `Microsoft.AspNetCore.Mvc.Testing` 10.0.15
  (MIT), ya presentes.
- SQL Server 2022 Developer Edition en la imagen oficial
  `mcr.microsoft.com/mssql/server:2022-latest` (gratuita para desarrollo y pruebas bajo el
  EULA de Microsoft; no es software libre, pero el backlog exige SQL Server y la
  constitución permite software gratuito). El detalle de licencias está en
  `investigacion.md`.
- Sin MediatR 13+, AutoMapper, FluentAssertions ni paquetes comerciales.

**Almacenamiento**: SQL Server 2022 local en un contenedor; volumen nombrado
`datos-sqlserver` para persistir entre reinicios. Base `BlackHellHeim`. Sin Azurite ni
almacenamiento de archivos.

**Pruebas**: xUnit en `src/Pruebas`, con tres frentes:
1. Persistencia real: un contenedor SQL Server por ejecución (Testcontainers) que aplica la
   migración, comprueba las semillas, revierte a `0` y vuelve a aplicar.
2. Configuración y salud: validación de la cadena de conexión, `/salud/base` disponible y no
   disponible, y `/salud` sin cambios.
3. La prueba de arquitectura existente, que sigue esperando exactamente cinco proyectos.

Comando único de verificación: `dotnet test BlackHellHeim.sln` (requiere Docker para las
pruebas de persistencia).

**Plataforma destino**: desarrollo local en Linux/macOS con Docker y .NET 10; la API se
ejecuta en el host, no en un contenedor en esta historia.

**Tipo de proyecto**: servicio web (API) con arquitectura limpia y persistencia relacional.

**Metas de rendimiento**: N/A. La comprobación de la base debe responder en menos de 5
segundos cuando la base no responde (CE-007); el tiempo máximo interno se fija en 3
segundos.

**Restricciones**:
- Sin credenciales en archivos versionados; `.env` ignorado por git.
- Sin Dockerfile del API, sin almacenamiento de archivos, sin OpenAPI interactivo y sin
  despliegue.
- La regla de dependencias no cambia; no se crean proyectos nuevos.
- Código, migraciones, pruebas, contrato y documentación en español; marca
  **BlackHellHeim**.

**Escala/Alcance**: 1 contenedor de SQL Server, 1 `DbContext`, 1 configuración de entidad,
1 migración inicial con 2 filas semilla, 1 script de arranque, 1 comprobación de salud de la
base y alrededor de 8 pruebas nuevas.

## Verificación de la constitución

*COMPUERTA: debe pasar antes de la investigación de la Fase 0. Se vuelve a comprobar
tras el diseño de la Fase 1.*

| # | Principio | Estado | Nota |
|---|-----------|--------|------|
| I | Arquitectura limpia | ✅ | El `DbContext`, las configuraciones y las migraciones viven en `Infraestructura`; `Api` solo registra y consume el contexto. No se crean proyectos ni se rompe la matriz. |
| II | Contrato primero | ✅ | `contratos/openapi.yaml` define `GET /salud/base` antes de implementarlo; los errores usan ProblemDetails. Scalar y la documentación interactiva quedan para la HU4. |
| III | Reglas de negocio probadas | ✅ | Se prueban la migración, las semillas, la reversión, la reaplicación, la configuración faltante y la salud disponible/no disponible. Las pruebas existentes se mantienen. |
| IV | Solo software libre | ✅ | EF Core y Testcontainers son MIT (verificado). SQL Server Developer es gratuito para desarrollo bajo EULA de Microsoft; se justifica porque el backlog lo exige. Sin paquetes comerciales. |
| V | El precio lo calcula el backend | N/A | Sin precios en esta historia. |
| VI | Pedidos idempotentes y webhooks | N/A | Sin pedidos ni webhooks. |
| VII | Puertos y adaptadores | ✅ | La persistencia se expone mediante el `DbContext` de `Infraestructura`; no se acopla `Dominio` ni `Aplicacion` a EF Core. |
| VIII | Seguridad y secretos | ✅ | `.env` fuera de git; `.env.example` solo con marcadores; la cadena de conexión se lee de `ConnectionStrings__BlackHellHeim`; los errores no imprimen el valor. |
| IX | Código de creadora | N/A | Sin códigos de creadora. |
| X | Diseños originales | N/A | Sin diseños. |
| XI | Idioma y estilo | ✅ | Especificación, plan, migraciones, pruebas y documentación en español; marca **BlackHellHeim**. |

Recomprobación tras la Fase 1: sin cambios. La tabla de complejidad queda vacía. La CI y el
análisis estático de la Definición de Hecho corresponden a la HU6 y están fuera del alcance
de esta historia; se registran como pendientes en el backlog.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/003-sql-server-efcore/
├── plan.md               # Este archivo (salida de /speckit.plan)
├── investigacion.md      # Salida de la Fase 0 (/speckit.plan)
├── modelo-datos.md       # Salida de la Fase 1 (/speckit.plan)
├── inicio-rapido.md      # Salida de la Fase 1 (/speckit.plan)
├── contratos/
│   └── openapi.yaml      # Contrato de GET /salud/base
└── tasks.md              # Salida de /speckit.tasks (no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
compose.yaml                      # SQL Server 2022 con healthcheck y volumen
.env.example                      # Marcadores de posición, sin secretos
.gitignore                        # Agrega .env
.config/
└── dotnet-tools.json             # dotnet-ef 10.0.15
scripts/
└── base-datos.sh                 # Comando único: compose + migración
src/
├── Infraestructura/
│   └── Persistencia/
│       ├── ContextoBlackHellHeim.cs
│       ├── FabricaContextoDiseno.cs
│       ├── ExtensionesPersistencia.cs
│       ├── Configuraciones/
│       │   └── TiendaConfiguracion.cs
│       └── Migraciones/           # Generadas por dotnet ef
├── Api/
│   ├── Configuracion/
│   │   └── CadenaConexion.cs      # Lectura y validación de la variable
│   ├── Endpoints/
│   │   └── SaludBaseDatosEndpoints.cs
│   └── Program.cs                 # Registro de persistencia y endpoint
└── Pruebas/
    ├── Api/
    │   ├── SaludPruebas.cs        # Ajuste: cadena de prueba sin credenciales
    │   └── SaludBaseDatosPruebas.cs
    └── Infraestructura/
        └── Persistencia/
            ├── ContenedorSqlServer.cs
            ├── ContextoBlackHellHeimPruebas.cs
            └── CadenaConexionPruebas.cs
```

**Decisión de estructura**: se conservan los cinco proyectos. `Infraestructura` aloja el
`DbContext`, las configuraciones de entidad y las migraciones; `Api` es el punto de
composición y expone la comprobación de la base; las pruebas de persistencia viven en
`Pruebas`. No se agrega un proyecto de migraciones ni se modifica la matriz de dependencias.

## Diseño de la persistencia

### Contexto y entidad

- `ContextoBlackHellHeim : DbContext` con `DbSet<Tienda>`.
- `TiendaConfiguracion : IEntityTypeConfiguration<Tienda>`:
  - tabla `dbo.Tiendas`;
  - clave primaria `Codigo`, convertida a `char(3)` no Unicode;
  - `Nombre` `nvarchar(120)` obligatorio;
  - `Moneda` `char(3)` no Unicode obligatoria;
  - semillas `BHH` (BlackHellHeim, MXN) y `DEM` (Tienda de demostración, MXN) con
    `HasData`, de modo que la migración inicial las inserta y la reversión las elimina.
- `FabricaContextoDiseno : IDesignTimeDbContextFactory<ContextoBlackHellHeim>` lee
  `ConnectionStrings__BlackHellHeim`; si falta, lanza un error claro sin imprimir valores.
  Permite ejecutar `dotnet ef` con `--project src/Infraestructura --startup-project
  src/Infraestructura` sin arrancar la API.
- `ExtensionesPersistencia.AgregarPersistencia(IServiceCollection, string)` registra el
  contexto con `UseSqlServer` y tiempo de vida con ámbito.

### Migración inicial

- Nombre: `MigracionInicial`, generada con
  `dotnet ef migrations add MigracionInicial -p src/Infraestructura -s src/Infraestructura -o Persistencia/Migraciones`.
- Crea `dbo.Tiendas` e inserta las dos semillas; EF registra la migración en
  `__EFMigrationsHistory`.
- Revertir con `dotnet ef database update 0 -p src/Infraestructura -s src/Infraestructura`
  elimina la tabla y las semillas; volver a aplicar las restaura.

### Comando único y Compose

`compose.yaml` define un único servicio `sqlserver` con la imagen oficial de Microsoft,
`ACCEPT_EULA=Y`, `MSSQL_PID=Developer`, contraseña desde `${BHH_SQL_SA_PASSWORD}`, puerto
`1433`, volumen `datos-sqlserver` y un `healthcheck` con `sqlcmd` que comprueba
`SELECT 1`.

`./scripts/base-datos.sh` es el comando único documentado:

1. Verifica que exista `.env` y que no conserve los marcadores de `.env.example`.
2. Carga `.env` y exporta `BHH_SQL_SA_PASSWORD` y `ConnectionStrings__BlackHellHeim`.
3. Ejecuta `docker compose up -d --wait sqlserver` (espera al healthcheck).
4. Ejecuta `dotnet tool restore` y
   `dotnet ef database update -p src/Infraestructura -s src/Infraestructura`.
5. Informa el resultado; si algo falla, termina con código distinto de cero.

**Por qué un script y no un servicio de migración en Compose**: el Dockerfile del API está
fuera de alcance y un servicio de migración con la imagen del SDK montaría el repositorio y
dejaría artefactos de compilación con permisos del contenedor. El script evita ese problema,
propaga los errores y mantiene un solo comando. `docker compose up` por sí solo levanta la
base; el comando documentado para dejar la base lista es el script. Esta desviación de la
letra del criterio del backlog se anota en `investigacion.md`.

### Configuración y secretos

- `.env.example` (versionado) contiene solo marcadores:
  `BHH_SQL_SA_PASSWORD='__DEFINIR_CLAVE_LOCAL__'` y
  `ConnectionStrings__BlackHellHeim='Server=localhost,1433;Database=BlackHellHeim;User Id=sa;Password=__DEFINIR_CLAVE_LOCAL__;Encrypt=True;TrustServerCertificate=True'`.
- `.env` (ignorado) contiene los valores reales del equipo.
- `CadenaConexion.Obtener(IConfiguration)` valida que
  `ConnectionStrings__BlackHellHeim` exista y no esté vacía; si falta, lanza
  `InvalidOperationException` con un mensaje que nombra la variable y remite a
  `.env.example`, sin mostrar el valor.
- La API no conecta al arrancar; solo registra el contexto. La conexión se comprueba en
  `/salud/base` y en las pruebas de persistencia.

### Salud

- `GET /salud` se conserva tal cual: comprobación de vida del API, sin base de datos.
- `GET /salud/base` resuelve el `DbContext` y ejecuta `CanConnectAsync` con un
  `CancellationTokenSource` enlazado de 3 segundos:
  - 200 con `{ "estado": "disponible" }` si conecta;
  - 503 con ProblemDetails (`codigo: base_no_disponible`, `idCorrelacion`) si no conecta,
    lanza o se agota el tiempo.
- No se agrega el paquete de health checks de EF Core; la comprobación es explícita y
  acotada.

### Pruebas de persistencia

- `ContenedorSqlServer` es un fixture de xUnit que inicia
  `mcr.microsoft.com/mssql/server:2022-latest` con Testcontainers y una contraseña generada
  en tiempo de ejecución (nunca versionada). Se comparte por clase de prueba.
- `ContextoBlackHellHeimPruebas`:
  1. migrar crea el esquema y las dos semillas;
  2. migrar dos veces no duplica;
  3. revertir a `0` elimina la tabla y las semillas;
  4. volver a aplicar restaura el esquema y las semillas.
- `SaludBaseDatosPruebas` usa `WebApplicationFactory` con la cadena del contenedor
  (disponible) y con una cadena a un puerto cerrado (no disponible); comprueba 200/503 y que
  `/salud` sigue en 200.
- `CadenaConexionPruebas` cubre cadena válida, ausente y vacía, y verifica que el mensaje no
  contiene el valor.
- `SaludPruebas` existente se ajusta para inyectar una cadena de prueba sin credenciales
  reales (por ejemplo, autenticación integrada a un puerto cerrado), de modo que el API
  arranque sin depender de la base.

## Fases de diseño

### Fase 0: investigación

Salida: `investigacion.md`. Decisiones D1–D9: imagen y licencia de SQL Server, versiones de
EF Core y `dotnet-ef`, ubicación de las migraciones, semillas con `HasData`, configuración
por variable de entorno, comando único, salud separada, estrategia de pruebas con
Testcontainers y tabla de licencias. No quedan `NEEDS CLARIFICATION`.

### Fase 1: diseño y contratos

Salidas:
- `modelo-datos.md`: tabla `Tiendas`, columnas, clave, restricciones, semillas, historial de
  migraciones y variables de configuración.
- `contratos/openapi.yaml`: `GET /salud/base` con 200 y 503 ProblemDetails; nota de que
  `/salud` no cambia.
- `inicio-rapido.md`: preparación de `.env`, comando único, verificación de migraciones y
  semillas, salud con la base arriba y abajo, reversión, reinicio y pruebas.

### Fase 2: tareas

`tasks.md` se genera con `/speckit.tasks`; no lo crea este comando.

## Seguimiento de complejidad

Sin violaciones que justificar. La tabla queda vacía.
