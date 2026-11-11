# Informe de la noche (2026-11-11)

Rama: `sprint/01`. No se hizo push. Todo el trabajo está en commits locales.

## Resultado

| HU | Spec | Commit | Tareas hechas | Pendientes | Estado en el backlog |
|----|------|--------|---------------|------------|----------------------|
| 2 | 002 + 002b | `de1cd8f` (ya enviado antes) | 50 de 50 | 0 | Hecha |
| 3 | `specs/003-sql-server-efcore` | `5d7cb18` "HU3: SQL Server y EF Core" | 44 de 47 | 3, todas `[MANUAL]` | En progreso |
| 4 | `specs/004-openapi-scalar` | `528b693` "HU4: OpenAPI con Scalar" | 38 de 41 | 3, todas `[MANUAL]` | En progreso |
| 5 | `specs/005-dockerfile-compose` | `c4e28b9` "HU5: Dockerfile y Compose" | 49 de 52 | 3, todas `[MANUAL]` | En progreso |

`dotnet build`: 0 advertencias, 0 errores. `dotnet test`: **144 pruebas en verde** (incluye 25 s de
pruebas de persistencia con contenedores reales). `./scripts/verificar-entorno.sh`: **0 fallos**
(entorno sano, caída y recuperación de la base, esquema al día, idempotencia, secretos, imagen,
Production, `/scalar` y `/openapi/v1.json` en el contenedor, y reproducibilidad con 66 archivos
idénticos entre dos construcciones sin caché).

No se detuvo ninguna spec por fallos: ninguna tarea falló tres veces. El commit de este informe
es el último ("Informe de la noche").

## Tareas `[MANUAL]` que te tocan

HU3 (`specs/003-sql-server-efcore/tasks.md`):
- **T006**: crea tu `.env` (`cp .env.example .env`) y reemplaza los dos `__DEFINIR_CLAVE_LOCAL__`
  por una contraseña tuya (≥ 8 caracteres con mayúsculas, minúsculas, dígitos y símbolos; sin
  `;`, `=` ni comillas). **Importante**: antes de usarla ejecuta `docker compose down -v`; el
  volumen `blackhellheim-api_datos-sqlserver` quedó creado con una contraseña temporal de las
  pruebas y SQL Server la guarda al inicializar el volumen (si no, verás `Login failed for user 'sa'`).
- **T045**: aprueba el contrato `contratos/openapi.yaml` de la 003. La HU4 ya lo alineó (3.1.1,
  `500`, `ProblemDetails` canónico, sin `const`).
- **T046**: cuando apruebes todo, marca la fila de la HU3 como `Hecha` y pon `Hecha` en el `Estado`
  de `spec.md`.

HU4 (`specs/004-openapi-scalar/tasks.md`):
- **T022**: abre `http://localhost:5068/scalar` (con `./scripts/entorno.sh`) y confirma que se ven el
  título, la versión y las tres operaciones.
- **T039**: decide si `/scalar` y `/openapi/v1.json` quedan públicos en el despliegue (hoy lo son en
  todos los entornos, incluido el contenedor) y anótalo en `investigacion.md`.
- **T040**: marca la HU4 como `Hecha` en el backlog y en `spec.md` cuando lo apruebes.

HU5 (`specs/005-dockerfile-compose/tasks.md`):
- **T025**: lo mismo que T006 (un solo `.env` sirve para las tres).
- **T047**: si tienes una máquina `linux/arm64`, prueba `./scripts/entorno.sh`; si no, deja
  constancia (el `Dockerfile` mapea `TARGETARCH`, pero no se verificó).
- **T051**: marca la HU5 como `Hecha` cuando lo apruebes.

Para probar todo con tu `.env`: `./scripts/entorno.sh` y después `./scripts/verificar-entorno.sh`.

## Decisiones que tomé

1. **Verificación sin tu `.env`**: no creé `.env` (es tuyo). Los scripts aceptan `BHH_ENV_ARCHIVO`
   (por defecto `.env`) y se probó con un archivo temporal fuera del repositorio.
2. **`global.json`**: `10.0.100` con `rollForward: latestFeature`, no `10.0.402`/`latestPatch`:
   tu SDK local (10.0.114) no cumpliría el plan; la reproducibilidad la da el digest del SDK.
3. **Digests**: SDK y `aspnet` tenían versiones nuevas; se fijaron los digests vigentes (anotados en
   `005/investigacion.md`).
4. **Docker sin BuildKit**: tu máquina no tiene `buildx`; el `Dockerfile` evita `COPY --chmod`.
5. **`FabricaApiPruebas`**: xUnit exige un único constructor público, así que la cadena propia se
   pasa con `FabricaApiPruebas.Con(cadena)` y no con un constructor opcional.
6. **`/salud/base` y SqlClient**: con el contenedor pausado, la cancelación de SqlClient no
   respeta los 3 s; el endpoint acota la espera con un temporizador y corre la comprobación en un
   ámbito propio (`Task.WhenAny`). Lo cubre una prueba con la base real pausada (2,5–5 s).
7. **Errores uniformes (HU4)**: no creé `ProblemaDeError.cs`; se reutiliza `TraductorProblemas`
   (ya era la traducción única). El manejo de excepciones pasó del middleware a
   `ManejadorExcepcionesGlobal` (`IExceptionHandler`) y el middleware quedó solo con la correlación.
8. **Comparación de contratos**: ignora la cabecera `X-Correlation-ID`, los `servers`, `info`,
   `tags`, ejemplos y `x-*`; el esquema se publica como `Tienda` (no `TiendaDto`) con
   `CreateSchemaReferenceId`, y el patrón `^[A-Z]{3}$` se declara con `RegularExpression`.
9. **Contratos de la 003**: el contrato menciona `Retry-After` en el 503, pero la API no lo envía
   (las cabeceras no se comparan). Queda como mejora si lo quieres.
10. **`TipoError.NoDisponible`** (503, `base_no_disponible`) se agregó a `ErrorUniforme`.
11. **Prueba de secretos**: busca `Password=` con un valor real en `git ls-files`; ignora marcadores
    `__...__`, variables y plantillas.
12. **Backlog**: HU3, HU4 y HU5 quedan "En progreso" porque tienen tareas `[MANUAL]` pendientes. Rellené
    la columna Spec de las filas 3, 4 y 5.
13. **Pruebas de la 003 ajustadas por la 005**: `.env.example` ahora tiene 3 líneas (se agregó
    `BHH_API_PUERTO`); se actualizó esa prueba.

## Fallos y cosas por saber

- Una ejecución de pruebas se colgó (contenedor de Testcontainers pausado) por la cancelación de
  SqlClient (decisión 6). La limpié liberando solo mis procesos y mi contenedor; el arreglo está en
  el commit de la HU3. Hubo otros contenedores en tu máquina (`blackhellheim-api`, `petmate-sqlserver`)
  que no toqué.
- Al revisar tras el commit de la HU5, la prueba de secretos falló porque los scripts y la prueba de
  Compose (ya versionados) contenían el patrón de búsqueda literal; se reescribió con `Passwor[d]=`
  y las 144 pruebas volvieron a pasar (commit "Corrige patrones de búsqueda de secretos").
- El constructor clásico de Docker imprime un aviso de obsolescencia; es inofensivo.
- No probado (documentado en el plan): arranque sin red (C5), imagen no descargable y arm64.
- Quedó sin versionar `.claude/settings.json` (no es mío, no lo toqué).
- Varias pruebas de Docker tardan: `dotnet test` ~25 s, `probar-imagen.sh` ~1 min,
  `verificar-entorno.sh` ~10 min con reproducibilidad (`--sin-reproducibilidad` la omite).
- El entorno Compose quedó detenido (`docker compose down`); el volumen se conserva.

## Qué hacer ahora

1. Revisa el informe y las tareas `[MANUAL]`.
2. `git push origin sprint/01` (yo no hice push).
3. Cuando hayas probado y aprobado cada HU, marca `Hecha` en el backlog (T046, T040, T051).
