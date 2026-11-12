# Investigación: Base de datos local con migraciones y datos semilla

Historia 003 · Fase 0 · Fecha: 2026-11-11

Este documento resuelve las incógnitas del contexto técnico del plan. No quedan
`NEEDS CLARIFICATION`. Todas las versiones se verificaron contra NuGet y los paquetes
descargados; las licencias se anotaron en la tabla final.

## D1. Motor e imagen de base de datos

**Decisión**: SQL Server 2022 Developer Edition en la imagen oficial de Microsoft
`mcr.microsoft.com/mssql/server:2022-latest`, servicio `sqlserver` en `compose.yaml`.

**Justificación**: el backlog de la HU3 exige SQL Server y EF Core; la imagen oficial
garantiza paridad con el proveedor y evita imágenes de terceros. Developer Edition es
gratuita para desarrollo y pruebas bajo el EULA de Microsoft, sin límite de tamaño y con
todas las funciones, que es justo el uso local de esta historia. La etiqueta `2022-latest`
ya está disponible en la máquina de desarrollo y es la versión estable que EF Core 10
soporta de forma madura.

**Alternativas consideradas**:
- `2025-latest`: también oficial y disponible localmente, pero más reciente y sin ventaja
  funcional para esta historia; se puede subir de versión en una historia posterior.
- Azure SQL Edge: pensada para ARM y con funciones recortadas; además su ciclo de vida está
  en retirada.
- PostgreSQL o SQLite: descartadas porque el backlog y la carpeta de la historia fijan SQL
  Server; cambiarlas exigiría renegociar el alcance.
- Fijar un digest o una etiqueta CU concreta: se deja para la HU6 (CI), que necesita
  reproducibilidad estricta. En local, `2022-latest` se actualiza con `docker compose pull`.

## D2. Versiones de EF Core y herramienta de migraciones

**Decisión**: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 y
`Microsoft.EntityFrameworkCore.Design` 10.0.15 en `Infraestructura`; `dotnet-ef` 10.0.15
como herramienta local en `.config/dotnet-tools.json`.

**Justificación**: es la última revisión estable de la línea 10 que coincide con `net10.0`
y con el SDK del repositorio; la 11.0.0 existe pero adelantarse a la línea mayor agrega
riesgo sin beneficio. `Design` va con `PrivateAssets=all` porque solo se necesita en tiempo
de diseño. La herramienta local evita depender de una instalación global y fija la versión
para todo el equipo y para la CI.

**Alternativas consideradas**:
- `dotnet ef` global: no reproducible y frágil en máquinas nuevas.
- EF Core 11.0.0: sin necesidad y sin soporte probado del resto de la solución.

## D3. Ubicación del contexto, configuraciones y migraciones

**Decisión**: todo el acceso a datos vive en `src/Infraestructura/Persistencia`
(`ContextoBlackHellHeim`, `Configuraciones/TiendaConfiguracion`, `Migraciones`) con una
`FabricaContextoDiseno` que permite ejecutar `dotnet ef` usando `Infraestructura` como
proyecto y como proyecto de arranque.

**Justificación**: respeta la matriz de dependencias (Dominio no conoce EF Core; Aplicación
solo conoce Dominio) y mantiene las migraciones junto al modelo que las genera. Usar
`Infraestructura` como proyecto de arranque evita cargar la API en tiempo de diseño, lo que
traería consigo el generador del mediador y la necesidad de configuración de la aplicación
para una simple operación de esquema. La fábrica lee la misma variable de entorno que la API
y falla con un mensaje claro si falta.

**Alternativas consideradas**:
- Migraciones en `Api`: rompe la separación y obliga a arrancar la composición web en cada
  comando de EF.
- Un proyecto nuevo de persistencia: viola la regla de cinco proyectos de la prueba de
  arquitectura y no aporta valor.

## D4. Datos semilla

**Decisión**: dos tiendas de referencia (`BHH` BlackHellHeim y `DEM` Tienda de demostración)
declaradas con `HasData` en `TiendaConfiguracion` y por tanto incluidas en la migración
inicial.

**Justificación**: `HasData` genera `INSERT` dentro de la migración, así que sembrar es
aplicar la migración, revertir elimina las filas y volver a aplicar las restaura. Es
determinista, idempotente y no requiere código de arranque. Las dos tiendas ya existen como
datos ilustrativos en la historia 002b, de modo que no se inventan reglas de negocio.

**Alternativas consideradas**:
- Sembrador en el arranque de la API: no es reversible, puede duplicar y acopla el
  despliegue a datos de ejemplo.
- Script SQL aparte: se desincroniza del esquema y duplica el mantenimiento.

## D5. Configuración de la cadena de conexión y secretos

**Decisión**: la API y la fábrica de diseño leen `ConnectionStrings__BlackHellHeim`
(mapeo estándar de .NET a `ConnectionStrings:BlackHellHeim`). El repositorio versiona solo
`.env.example` con marcadores; `.env` queda en `.gitignore` y el script lo carga y exporta
antes de invocar Compose y `dotnet ef`. La contraseña de `sa` se llama
`BHH_SQL_SA_PASSWORD` y Compose la inyecta en `MSSQL_SA_PASSWORD`.

**Justificación**: cumple el principio VIII (ningún secreto versionado) y el criterio del
backlog (credenciales fuera del repositorio) sin introducir un almacén externo para
desarrollo local. Una sola variable de entorno alimenta tanto al contenedor como al cliente,
de modo que no pueden desincronizarse. El mensaje de error nombra la variable faltante y
remite a `.env.example`, sin imprimir el valor.

**Alternativas consideradas**:
- `appsettings.Development.json`: riesgo alto de filtrar la cadena con contraseña al
  repositorio público.
- `dotnet user-secrets`: válido solo para la API en el host, no para Compose ni para las
  pruebas; agrega un paso manual que rompe el comando único.
- Autenticación integrada de Windows: no funciona de forma uniforme en Linux/macOS.

## D6. Comando único

**Decisión**: `./scripts/base-datos.sh` es el comando documentado. Ejecuta, en orden:
validación de `.env`, `docker compose up -d --wait sqlserver`, `dotnet tool restore` y
`dotnet ef database update -p src/Infraestructura -s src/Infraestructura`. Devuelve código
distinto de cero y un mensaje claro si cualquier paso falla.

**Justificación**: `docker compose up` por sí solo levanta la base pero no aplica
migraciones, y el Dockerfile del API está fuera de alcance, así que no puede existir un
servicio de migración construido sobre la imagen del API. Un servicio con la imagen del SDK
montaría el repositorio y dejaría `bin/` y `obj/` con permisos del contenedor. El script
mantiene un solo comando, espera al `healthcheck`, propaga errores y no deja pasos manuales.
La desviación de la letra del criterio del backlog («docker compose up crea la base y aplica
la migración») se registra aquí: el script incluye ese `up` y añade la migración.

**Alternativas consideradas**:
- Servicio `migraciones` en Compose con la imagen del SDK: permisos, artefactos y arranque
  más lento.
- Migrar automáticamente al arrancar la API: oculta fallos, mezcla responsabilidades y
  choca con el despliegue de la HU5.
- `docker compose up` sin migración: incumple el criterio de aceptación.

## D7. Salud del API y salud de la base

**Decisión**: `GET /salud` se conserva sin cambios y sin tocar la base. Se agrega
`GET /salud/base`, que resuelve el `DbContext` y ejecuta `CanConnectAsync` con un
`CancellationTokenSource` de 3 segundos; responde 200 con `{"estado":"disponible"}` o 503
con ProblemDetails (`codigo: base_no_disponible` e `idCorrelacion`) si no conecta, lanza o
se agota el tiempo.

**Justificación**: separa «el API está vivo» de «el API puede atender operaciones con
persistencia», que es exactamente lo que pide la historia. El tiempo interno de 3 segundos
deja margen frente al máximo de 5 segundos del criterio de éxito. Se reutiliza el formato
uniforme de errores y la correlación de la historia 002b, sin agregar paquetes.

**Alternativas consideradas**:
- `AddHealthChecks().AddDbContextCheck(...)`: agrega un paquete, impone su propio formato y
  no incluye el identificador de correlación.
- Hacer que `/salud` dependa de la base: convierte una caída de la base en una falsa caída
  del API y contradice la historia.
- Comprobar con una consulta real a `Tiendas`: más costoso y no aporta más que abrir
  conexión.

## D8. Pruebas de persistencia

**Decisión**: pruebas de integración con un contenedor real de SQL Server por ejecución,
usando `Testcontainers.MsSql` 4.16.0 (MIT) en `src/Pruebas/Infraestructura/Persistencia`.
Un fixture compartido inicia la imagen `2022-latest` con una contraseña generada en tiempo
de ejecución; las pruebas aplican la migración, comprueban las semillas, revierten a `0` y
vuelven a aplicar. Requieren Docker y fallan con un mensaje explícito si no está disponible.

**Justificación**: es la única forma de probar de verdad lo que pide la historia
(migraciones, semillas, reversión) porque `HasData` y el SQL generado son específicos del
proveedor. InMemory y SQLite no ejecutan el SQL de SQL Server ni el historial de migraciones,
así que darían falsa confianza. Testcontainers está bajo MIT y ya es el estándar del
ecosistema .NET.

**Alternativas consideradas**:
- EF Core InMemory: no prueba migraciones ni SQL; descartado.
- SQLite en memoria: otro proveedor; las migraciones de SQL Server no aplican.
- Instancia local compartida: exige preparación manual, no es aislada y puede ensuciar
  datos.
- Omitir las pruebas cuando no hay Docker: se prefiere un fallo claro antes que un verde
  engañoso. La CI de la HU6 dispone de Docker en el runner.

## D9. Correlación y errores de la nueva comprobación

**Decisión**: `/salud/base` reutiliza el middleware de correlación y el mapeo de
`ErrorUniforme` a ProblemDetails de la historia 002b. La respuesta 503 incluye
`idCorrelacion` y `codigo`, nunca la cadena de conexión, el nombre del servidor ni la
excepción.

**Justificación**: mantiene un solo formato de error en toda la API y evita que la nueva
comprobación exponga detalles internos (RF-012). No se duplica el middleware.

**Alternativas consideradas**:
- Respuesta de texto plano: rompe la uniformidad y no permite seguimiento.
- Incluir la excepción: filtraría detalles internos y posibles datos de conexión.

## Dependencias y riesgos

- **Prerrequisito**: la historia 002b aporta `Tienda`, `CodigoTienda`, la correlación y el
  formato de errores. Si 003 se implementara antes, habría que crear esos tipos aquí y 002b
  los reutilizaría; el orden recomendado es 002b y después 003 para no duplicarlos.
- **Docker obligatorio**: el comando único y las pruebas de persistencia lo requieren. La
  HU6 debe garantizar Docker en el runner de CI.
- **Puerto 1433**: si está ocupado, Compose falla; el script informa y no deja contenedores
  a medias. Se documenta cómo cambiarlo en `.env` si hiciera falta.
- **Volumen con esquema viejo**: `dotnet ef database update` aplica solo las migraciones
  pendientes; si el historial no coincide, EF falla y el script informa sin borrar datos.
- **Licencia de SQL Server**: no es software libre, pero es gratuita para desarrollo y
  pruebas; el backlog la exige y la constitución permite software gratuito. Queda anotada.

## Tabla de licencias

| Dependencia | Versión | Licencia | Verificación | Uso |
|-------------|---------|----------|--------------|-----|
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.15 | MIT | `.nuspec` descargado de NuGet | Acceso a datos |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.15 | MIT | `.nuspec` descargado de NuGet | Migraciones en tiempo de diseño |
| `dotnet-ef` | 10.0.15 | MIT | `.nuspec` descargado de NuGet | Herramienta local de migraciones |
| `Testcontainers.MsSql` | 4.16.0 | MIT | `.nuspec` descargado de NuGet | Pruebas de persistencia |
| `xunit` | 2.9.3 | Apache 2.0 | Ya presente en el repositorio | Pruebas |
| `Microsoft.NET.Test.Sdk` | 18.11.1 | MIT | Ya presente en el repositorio | Pruebas |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.15 | MIT | Ya presente en el repositorio | Pruebas de la API |
| Imagen `mcr.microsoft.com/mssql/server:2022-latest` | 2022 | EULA de Microsoft; Developer Edition gratuita para desarrollo y pruebas | Documentación oficial de Microsoft | Base de datos local |

Sin MediatR 13+, AutoMapper, FluentAssertions ni dependencias de licencia comercial.
