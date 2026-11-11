# Tareas: Base de datos local con migraciones y datos semilla

**Entrada**: `specs/003-sql-server-efcore/` (spec.md, plan.md, investigacion.md, modelo-datos.md, contratos/openapi.yaml, inicio-rapido.md)

**Prerrequisitos**: plan.md y spec.md. **La historia 002b debe estar implementada** (tipos `Tienda` y `CodigoTienda` en `Dominio`, `IContextoEjecucion`, correlación y ProblemDetails en `Api`). Si no lo está, se detiene todo y se avisa; no se duplican esos tipos.

**Pruebas**: obligatorias y primero (constitución III). Cada prueba se escribe y se ve fallar antes de implementar. Nada se difiere.

**Formato**: `- [ ] T### [P?] [US?] [ETIQUETA] Descripción con ruta`. Etiquetas: `[BE]` código/pruebas del API, `[DB]` base de datos y migraciones, `[CI]` automatización, `[MANUAL]` solo la desarrolladora. `[P]` = paralelizable.

## Fase 1: Preparación

- [ ] T001 [BE] Verificar prerrequisitos: existen `Tienda` y `CodigoTienda` en `src/Dominio/`, `IContextoEjecucion` y el middleware de correlación/ProblemDetails en `src/Api/`; si falta alguno, DETENER y avisar.
- [ ] T002 [P] [DB] Agregar `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 (MIT) y `Microsoft.EntityFrameworkCore.Design` 10.0.15 (MIT, `PrivateAssets=all`) a `src/Infraestructura/BlackHellHeim.Infraestructura.csproj`; confirmar licencias en `specs/003-sql-server-efcore/investigacion.md`.
- [ ] T003 [P] [BE] Agregar `Testcontainers.MsSql` 4.16.0 (MIT) a `src/Pruebas/BlackHellHeim.Pruebas.csproj` y confirmar su licencia en `investigacion.md`.
- [ ] T004 [P] [DB] Crear `.config/dotnet-tools.json` con `dotnet-ef` 10.0.15 (MIT) como herramienta local.
- [ ] T005 [P] [CI] Agregar `.env` al `.gitignore` de la raíz y crear `.env.example` con solo marcadores: `BHH_SQL_SA_PASSWORD='__DEFINIR_CLAVE_LOCAL__'` y `ConnectionStrings__BlackHellHeim='Server=localhost,1433;Database=BlackHellHeim;User Id=sa;Password=__DEFINIR_CLAVE_LOCAL__;Encrypt=True;TrustServerCertificate=True'`.
- [ ] T006 [MANUAL] Crear `.env` local a partir de `.env.example` con una contraseña propia (política de SQL Server: ≥ 8 caracteres con mayúsculas, minúsculas, dígitos y símbolos).
- [ ] T007 [BE] Ejecutar `dotnet restore` y `dotnet build BlackHellHeim.sln`: compila sin advertencias nuevas.

## Fase 2: Base de persistencia (bloquea las historias)

- [ ] T008 [DB] Crear `compose.yaml` en la raíz con el servicio `sqlserver` (`mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA=Y`, `MSSQL_PID=Developer`, `MSSQL_SA_PASSWORD=${BHH_SQL_SA_PASSWORD:?Defina BHH_SQL_SA_PASSWORD en .env}`, puerto `1433`, volumen `datos-sqlserver`, `healthcheck` con `sqlcmd -C -Q "SELECT 1"` usando `SQLCMDPASSWORD`).
- [ ] T009 [BE] Crear `src/Infraestructura/Persistencia/ContextoBlackHellHeim.cs` (`DbContext` con `DbSet<Tienda>`), sin configuración de entidad aún.
- [ ] T010 [P] [BE] Crear `src/Infraestructura/Persistencia/ExtensionesPersistencia.cs` con `AgregarPersistencia(IServiceCollection, string)` que registra el contexto con `UseSqlServer` y ámbito.

## Fase 3: Historia 1 — Base local lista con un solo comando (P1)

**Meta**: un comando levanta la base sana, la migra y siembra dos tiendas.

**Prueba independiente**: `./scripts/base-datos.sh` termina en 0, `dbo.Tiendas` tiene `BHH` y `DEM`, y repetirlo no duplica filas.

### Pruebas primero

- [ ] T011 [P] [US1] [BE] Escribir `src/Pruebas/Infraestructura/Persistencia/ContenedorSqlServer.cs`: fixture xUnit que inicia `mcr.microsoft.com/mssql/server:2022-latest` con Testcontainers y una contraseña generada en tiempo de ejecución, nunca versionada.
- [ ] T012 [P] [US1] [BE] Escribir `src/Pruebas/Infraestructura/Persistencia/ContextoBlackHellHeimPruebas.cs`: migrar crea `dbo.Tiendas` con `Codigo char(3)` no Unicode (clave primaria), `Nombre nvarchar(120)` no nulo y `Moneda char(3)` no Unicode no nulo, y las dos semillas `BHH` ("BlackHellHeim", MXN) y `DEM` ("Tienda de demostración", MXN); migrar dos veces no duplica. Debe fallar.
- [ ] T013 [P] [US1] [CI] Escribir la prueba de aceptación `scripts/probar-base-datos.sh` (aún sin el script que prueba): ejecuta `./scripts/base-datos.sh` dos veces, exige código de salida 0 en ambas y `SELECT COUNT(*) FROM dbo.Tiendas` = 2 tras cada una. Debe fallar porque `scripts/base-datos.sh` todavía no existe.

### Implementación

- [ ] T014 [US1] [DB] Crear `src/Infraestructura/Persistencia/Configuraciones/TiendaConfiguracion.cs`: tabla `dbo.Tiendas`; `Codigo` con `HasConversion` a `string`, `HasMaxLength(3)`, `IsUnicode(false)`, `IsFixedLength()`, `ValueGeneratedNever()`; `Nombre` `IsRequired()` `HasMaxLength(120)`; `Moneda` `IsRequired()` `HasMaxLength(3)` `IsUnicode(false)` `IsFixedLength()`; `HasData` con `BHH` y `DEM`. Aplicar la configuración en `ContextoBlackHellHeim`.
- [ ] T015 [US1] [DB] Crear `src/Infraestructura/Persistencia/FabricaContextoDiseno.cs` (`IDesignTimeDbContextFactory`) que lee `ConnectionStrings__BlackHellHeim` y, si falta, lanza un error que nombra la variable y remite a `.env.example`, sin imprimir valores.
- [ ] T016 [US1] [DB] Generar la migración con `dotnet ef migrations add MigracionInicial -p src/Infraestructura -s src/Infraestructura -o Persistencia/Migraciones` y revisar que crea `Tiendas` con las columnas de T012 y las dos semillas.
- [ ] T017 [US1] [CI] Escribir `scripts/base-datos.sh` (comando único, sin lógica de prueba): valida que `.env` exista y no conserve los marcadores de `.env.example`, lo carga, ejecuta `docker compose up -d --wait sqlserver`, `dotnet tool restore` y `dotnet ef database update -p src/Infraestructura -s src/Infraestructura`, informa el resultado y sale distinto de cero ante cualquier fallo.
- [ ] T018 [US1] [DB] Ejecutar `./scripts/probar-base-datos.sh` y `dotnet test --filter ContextoBlackHellHeimPruebas`: T012 y T013 en verde.

**Punto de control**: US1 funciona sola.

## Fase 4: Historia 2 — Migraciones aplicables y reversibles (P2)

**Prueba independiente**: `dotnet ef database update 0` deja la base sin `Tiendas`; volver a aplicar restaura tabla y semillas.

### Pruebas primero

- [ ] T019 [P] [US2] [BE] Agregar a `ContextoBlackHellHeimPruebas.cs`: revertir a `0` elimina la tabla y las semillas, y vuelve a aplicar las restaura (éxito).
- [ ] T020 [P] [US2] [BE] Agregar a `ContextoBlackHellHeimPruebas.cs`: el historial `__EFMigrationsHistory` contiene exactamente `MigracionInicial` tras migrar y queda vacío tras revertir (validación).
- [ ] T021 [P] [US2] [BE] Agregar a `ContextoBlackHellHeimPruebas.cs`: una migración que falla a mitad no se registra y deja la base en el estado anterior (error).

### Implementación

- [ ] T022 [US2] [DB] Verificar que el `Down` de `MigracionInicial` elimina la tabla y corregirlo si T019 falla; documentar el comando de reversión en `inicio-rapido.md`.
- [ ] T023 [US2] [BE] Ejecutar `dotnet test --filter ContextoBlackHellHeimPruebas` en verde.

## Fase 5: Historia 3 — Conexión del API sin credenciales en el repositorio (P3)

**Prueba independiente**: `git grep` de contraseñas sin coincidencias; arrancar sin la variable falla nombrándola, sin valor.

### Pruebas primero

- [ ] T024 [P] [US3] [BE] Escribir `src/Pruebas/Infraestructura/Persistencia/CadenaConexionPruebas.cs`: cadena válida se lee; cadena ausente y vacía lanzan `InvalidOperationException` cuyo mensaje nombra `ConnectionStrings__BlackHellHeim` y no contiene el valor. Debe fallar.
- [ ] T025 [P] [US3] [BE] Escribir `src/Pruebas/Entorno/ConfiguracionSinSecretosPruebas.cs`: `.env.example` solo contiene marcadores, `.env` está en `.gitignore`, y ningún archivo de `git ls-files` contiene la contraseña real ni `Password=` distinto del marcador.

### Implementación

- [ ] T026 [US3] [BE] Crear `src/Api/Configuracion/CadenaConexion.cs` con `Obtener(IConfiguration)` que valida la variable y lanza el error de T024 sin mostrar valores.
- [ ] T027 [US3] [BE] Registrar la persistencia en `src/Api/Program.cs` con `AgregarPersistencia`, sin conectar al arrancar.
- [ ] T028 [US3] [BE] Ajustar `src/Pruebas/Api/SaludPruebas.cs` para inyectar una cadena de prueba sin credenciales reales (autenticación integrada a un puerto cerrado) y confirmar que `/salud` sigue en 200 sin base.
- [ ] T029 [US3] [BE] Ejecutar `dotnet test --filter "CadenaConexionPruebas|ConfiguracionSinSecretosPruebas|SaludPruebas"` en verde.

## Fase 6: Historia 4 — Salud independiente de la base (P4)

**Contrato primero**: `contratos/openapi.yaml` ya define `GET /salud/base`; no se escribe código del endpoint antes de que esté vigente.

**Prueba independiente**: `/salud` da 200 con la base caída; `/salud/base` da 200 con la base y 503 sin ella en menos de 5 s.

### Pruebas primero

- [ ] T030 [P] [US4] [BE] Escribir `src/Pruebas/Api/SaludBaseDatosPruebas.cs`: con `WebApplicationFactory<Program>` y la cadena del contenedor, `/salud/base` → 200 `{"estado":"disponible"}`; con un puerto cerrado → 503 `application/problem+json` con `codigo: base_no_disponible`, `idCorrelacion`, sin cadena de conexión; y `/salud` → 200 en ambos casos. Debe fallar.
- [ ] T031 [P] [US4] [BE] Agregar a `SaludBaseDatosPruebas.cs`: una base que no responde devuelve 503 en menos de 5 s (tiempo máximo interno 3 s) y la cabecera `X-Correlation-ID` se conserva.
- [ ] T032 [P] [US4] [BE] Escribir en `src/Pruebas/Api/SaludBaseDatosPruebas.cs` la prueba de base real inaccesible: con el contenedor de `ContenedorSqlServer` ya sano, pausarlo (`docker pause`, con `PauseAsync` de Testcontainers si existe o ejecutando el comando) para que acepte conexiones TCP pero no responda; `GET /salud/base` debe devolver 503 `application/problem+json` con `codigo: base_no_disponible`, en un tiempo medido entre 2,5 s y 5 s (prueba que el límite de 3 s se aplica y no un rechazo inmediato); `GET /salud` sigue en 200. Al reanudar el contenedor (`unpause`), `/salud/base` vuelve a 200. Debe fallar.
- [ ] T033 [P] [US4] [BE] Agregar a `SaludBaseDatosPruebas.cs` que la cadena de conexión no aparece ni en el cuerpo ni en las cabeceras del 503 de T032 y que dos solicitudes consecutivas con la base pausada no se acumulan (cada una termina en < 5 s).

### Implementación

- [ ] T034 [US4] [BE] Crear `src/Api/Endpoints/SaludBaseDatosEndpoints.cs`: resuelve el contexto y ejecuta `CanConnectAsync` con un `CancellationTokenSource` enlazado de 3 s; 200 `{ "estado": "disponible" }` o 503 ProblemDetails (`base_no_disponible`, `idCorrelacion`) ante fallo, excepción o tiempo agotado.
- [ ] T035 [US4] [BE] Mapear el endpoint en `src/Api/Program.cs`.
- [ ] T036 [US4] [BE] Ejecutar `dotnet test --filter SaludBaseDatosPruebas` en verde (incluidas T031, T032 y T033). Si T032 falla por exceder 5 s, ajustar el endpoint de T034 (token enlazado de 3 s y `Connect Timeout`/`CommandTimeout` de la cadena) sin cambiar el contrato.

## Fase 7: Pulido y verificación final

- [ ] T037 [P] [BE] Confirmar que `src/Pruebas/Arquitectura` sigue esperando cinco proyectos y que `Dominio` y `Aplicacion` no referencian EF Core.
- [ ] T038 [P] [DB] Probar `docker compose down -v` seguido de `./scripts/base-datos.sh`: la base se recrea y las semillas vuelven a dos filas; y persistencia entre `docker compose restart sqlserver`.
- [ ] T039 [MANUAL] Aprobar el alcance del contrato `contratos/openapi.yaml` para que la HU4 lo alinee (versión 3.1.1) sin cambiar el comportamiento.
- [ ] T040 [MANUAL] Actualizar `docs/scrum/backlog.md` (fila de la HU3: Spec = `specs/003-sql-server-efcore`, Estado = Hecha) tras aprobar la verificación final, y el `**Estado**` de `spec.md`.
- [ ] T041 [BE] Verificación final: `dotnet build BlackHellHeim.sln` sin advertencias nuevas, `dotnet test BlackHellHeim.sln` en verde (requiere Docker para las pruebas de persistencia) y `docker compose up -d --wait sqlserver` con la base sana.

## Dependencias

- Fase 1 → Fase 2 → US1 → US2 (reutiliza la migración) → US3 → US4 (necesita la persistencia registrada de US3).
- Externa: la 002b debe estar implementada.
- Dependientes: la 004 compone este contrato (`/salud/base`) y la 005 extiende `compose.yaml`, `.env.example` y `scripts/base-datos.sh`.

## Ejecución en paralelo (ejemplos)

- Fase 1: T002, T003, T004, T005 juntas.
- US1, pruebas: T011, T012, T013 juntas.
- US2, pruebas: T019, T020, T021 juntas.

## Estrategia

MVP = US1 (Fases 1 a 3). Luego US2, US3 y US4. Las pruebas de cada historia se ven fallar antes de implementar. Si T001 falla, no se implementa nada.
