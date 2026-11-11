# Tareas: Casos de uso con CQRS, mediador y pipeline

**Entrada**: `specs/002b-casos-de-uso-mediator/` (spec.md, plan.md, investigacion.md, modelo-datos.md, contratos/openapi.yaml, inicio-rapido.md)

**Prerrequisitos**: plan.md y spec.md. Complementa `specs/002-estructura-base-api` (estructura en cinco proyectos y prueba de arquitectura, ya presentes). Es la base de las historias 003 y 004.

**Pruebas**: obligatorias y primero (constitución III). Cada prueba se escribe y se ve fallar antes de implementar. Nada se difiere.

**Formato**: `- [ ] T### [P?] [US?] [ETIQUETA] Descripción con ruta`. Etiquetas: `[BE]` código/pruebas del API, `[DB]` base de datos, `[CI]` automatización, `[MANUAL]` solo la desarrolladora. `[P]` = paralelizable.

## Fase 1: Preparación

- [ ] T001 [BE] Verificar el punto de partida: `dotnet build BlackHellHeim.sln` y `dotnet test BlackHellHeim.sln` en verde, y que `src/Pruebas/Arquitectura` espera cinco proyectos; revisar sus reglas para saber si `Api → Infraestructura` ya está permitida.
- [ ] T002 [BE] Agregar `Mediator.Abstractions` 3.0.2 (MIT) a `src/Aplicacion/BlackHellHeim.Aplicacion.csproj` y `Mediator.SourceGenerator` 3.0.2 (MIT, `PrivateAssets=all`) a `src/Api/BlackHellHeim.Api.csproj`; confirmar las licencias en `specs/002b-casos-de-uso-mediator/investigacion.md` (sin MediatR 13+, AutoMapper ni FluentAssertions).
- [ ] T003 [BE] Agregar la referencia de proyecto `Api → Infraestructura` en `src/Api/BlackHellHeim.Api.csproj` y, si la prueba de arquitectura la bloquea, ajustar su regla a la matriz de `specs/002-estructura-base-api` sin cambiar el número de proyectos.
- [ ] T004 [BE] Ejecutar `dotnet build BlackHellHeim.sln`: compila sin advertencias nuevas.

## Fase 2: Contrato primero y base común (bloquea las historias)

- [ ] T005 [BE] Revisar `specs/002b-casos-de-uso-mediator/contratos/openapi.yaml`: `GET /tiendas/{codigo}` con 200 (`Tienda`), 400, 404 y 500 con `ProblemDetails` y cabecera `X-Correlation-ID`; el contrato debe estar vigente antes de escribir el endpoint.
- [ ] T006 [P] [BE] Crear `src/Aplicacion/Comun/Errores/ErrorUniforme.cs` con `Tipo` (`Validacion`, `NoEncontrado`, `Conflicto`, `Cancelado`, `Inesperado`), `Codigo`, `Mensaje`, `Campos` (`IReadOnlyDictionary<string, string[]>?`, solo en validación) e `IdCorrelacion`; sin trazas, rutas internas ni datos sensibles.
- [ ] T007 [P] [BE] Crear `src/Aplicacion/Comun/Errores/ErrorCampo.cs`, `ExcepcionCasoDeUso.cs` y `ExcepcionValidacion.cs`.
- [ ] T008 [P] [BE] Crear `src/Aplicacion/Comun/CasosDeUso/ResultadoCasoDeUso.cs` con `Exito(valor)` y `Fallo(error)`; `Valor` solo en éxito y `Error` solo en fallo; un no encontrado es un fallo `NoEncontrado`, no una excepción.
- [ ] T009 [P] [BE] Crear los puertos `src/Aplicacion/Comun/Correlacion/IContextoEjecucion.cs` (`string IdCorrelacion { get; set; }`), `src/Aplicacion/Comun/Registro/IRegistroEjecucion.cs` (`Registrar(RegistroCasoDeUso)`), `src/Aplicacion/Comun/Registro/RegistroCasoDeUso.cs` (`TipoSolicitud`, `Exito`, `DuracionMs`, `IdCorrelacion`, `Momento`, `CodigoError?`, `TipoExcepcion?`; sin carga útil ni valores sensibles) y `src/Aplicacion/Comun/Validacion/IValidadorSolicitud.cs` (`IReadOnlyList<ErrorCampo> Validar(TSolicitud)`).
- [ ] T010 [P] [BE] Crear los dobles de prueba `src/Pruebas/Dobles/ContextoEjecucionFalso.cs`, `RegistroEjecucionEnMemoria.cs`, `RepositorioTiendasFalso.cs` y `ValidadorSolicitudFalso.cs`.

## Fase 3: Historia 1 — Consulta de ejemplo de punta a punta (P1)

**Meta**: `GET /tiendas/BHH` cruza API → Aplicación → Dominio sin que la API conozca el manejador.

**Prueba independiente**: `curl http://localhost:5068/tiendas/BHH` devuelve 200 con `TiendaDto`; `/tiendas/ZZZ` devuelve 404.

### Pruebas primero

- [ ] T011 [P] [US1] [BE] Escribir `src/Pruebas/Dominio/CodigoTiendaPruebas.cs`: `CodigoTienda.Crear` recorta, pasa a mayúsculas y acepta exactamente tres letras A–Z; rechaza vacío, dos o cuatro letras, números, acentos y espacios internos con `ArgumentException`; igualdad por valor (éxito, error y validación). Debe fallar.
- [ ] T012 [P] [US1] [BE] Escribir `src/Pruebas/Aplicacion/Tiendas/ManejadorConsultarTiendaPruebas.cs`: código existente → éxito con `TiendaDto` (nunca la entidad); código inexistente → fallo `NoEncontrado`; el repositorio recibe el código normalizado. Debe fallar.
- [ ] T013 [P] [US1] [BE] Escribir `src/Pruebas/Api/ConsultarTiendaPruebas.cs` con `WebApplicationFactory<Program>`: `/tiendas/BHH` → 200; `/tiendas/ZZZ` → 404 `application/problem+json`; `/salud` sigue en 200 con `{"estado":"ok"}`. Debe fallar.

### Implementación

- [ ] T014 [US1] [BE] Crear `src/Dominio/Tiendas/CodigoTienda.cs` (objeto de valor, `Crear(string)`, igualdad por valor).
- [ ] T015 [US1] [BE] Crear `src/Dominio/Tiendas/Tienda.cs` (`Codigo` obligatorio, `Nombre` no vacío, `Moneda` tres letras mayúsculas; solo lectura; constructor privado sin parámetros o equivalente que permita el mapeo posterior de EF Core en la historia 003).
- [ ] T016 [P] [US1] [BE] Crear `src/Aplicacion/Tiendas/IRepositorioTiendas.cs` (`Task<Tienda?> ObtenerPorCodigoAsync(CodigoTienda, CancellationToken)`).
- [ ] T017 [P] [US1] [BE] Crear `src/Aplicacion/Tiendas/ConsultarTienda/ConsultarTiendaQuery.cs` (`Codigo` string, `IQuery<ResultadoCasoDeUso<TiendaDto>>`) y `TiendaDto.cs` (`Codigo`, `Nombre`, `Moneda`).
- [ ] T018 [US1] [BE] Crear `src/Aplicacion/Tiendas/ConsultarTienda/ManejadorConsultarTienda.cs` (`IQueryHandler`) usando el puerto y devolviendo `TiendaDto` o `Fallo` `NoEncontrado`.
- [ ] T019 [US1] [BE] Crear `src/Infraestructura/Tiendas/RepositorioTiendasEnMemoria.cs` con las semillas ilustrativas `BHH` ("BlackHellHeim", MXN) y `DEM` ("Tienda de demostración", MXN).
- [ ] T020 [US1] [BE] Crear `src/Api/Endpoints/TiendasEndpoints.cs` con `GET /tiendas/{codigo}` que inyecta solo `ISender`, devuelve `TiendaDto` (200) o ProblemDetails (404), y declarar el mapeo en `src/Api/Program.cs` con `AddMediator` (`ServiceLifetime.Scoped`) y el registro del repositorio en `src/Api/Comun/RegistroServicios.cs`.
- [ ] T021 [US1] [BE] Ejecutar `dotnet test --filter "CodigoTiendaPruebas|ManejadorConsultarTiendaPruebas|ConsultarTiendaPruebas"` en verde y confirmar que la prueba de arquitectura sigue en verde (la API no referencia `ManejadorConsultarTienda`).

**Punto de control**: US1 funciona sola.

## Fase 4: Historia 2 — Validación antes del manejador (P2)

**Prueba independiente**: `GET /tiendas/B` → 400 con errores por campo, y el manejador no se invoca.

### Pruebas primero

- [ ] T022 [P] [US2] [BE] Escribir `src/Pruebas/Aplicacion/Comportamientos/ValidacionBehaviorPruebas.cs`: sin validadores pasa al manejador; con errores lanza `ExcepcionValidacion` y el manejador no se invoca; varios validadores acumulan errores por campo. Debe fallar.
- [ ] T023 [P] [US2] [BE] Escribir `src/Pruebas/Aplicacion/Tiendas/ValidadorConsultarTiendaPruebas.cs`: `Codigo` obligatorio y exactamente `^[A-Z]{3}$`; vacío, nulo, minúsculas, dos o cuatro letras y símbolos son inválidos (éxito, error y validación).
- [ ] T024 [P] [US2] [BE] Agregar a `ConsultarTiendaPruebas.cs`: `GET /tiendas/B` → 400 `application/problem+json` con `codigo`, `idCorrelacion` y `errores.codigo`.

### Implementación

- [ ] T025 [US2] [BE] Crear `src/Aplicacion/Comun/Comportamientos/ValidacionBehavior.cs` (genérico abierto, ejecuta todos los `IValidadorSolicitud<T>`, lanza `ExcepcionValidacion`).
- [ ] T026 [US2] [BE] Crear `src/Aplicacion/Tiendas/ConsultarTienda/ValidadorConsultarTienda.cs`.
- [ ] T027 [US2] [BE] Ejecutar `dotnet test --filter "ValidacionBehaviorPruebas|ValidadorConsultarTiendaPruebas|ConsultarTiendaPruebas"` en verde.

## Fase 5: Historia 3 — Registro de cada ejecución (P3)

**Prueba independiente**: tras una consulta, el registro contiene tipo de solicitud, resultado, duración e `IdCorrelacion`, sin datos sensibles.

### Pruebas primero

- [ ] T028 [P] [US3] [BE] Escribir `src/Pruebas/Aplicacion/Comportamientos/RegistroBehaviorPruebas.cs`: registra éxito y fallo con `DuracionMs`, `IdCorrelacion`, `CodigoError` y `TipoExcepcion` (sin mensaje ni traza); no altera la respuesta; un fallo del registro no propaga. Debe fallar.
- [ ] T029 [P] [US3] [BE] Agregar a `ConsultarTiendaPruebas.cs` una prueba que verifica que el registro no contiene la carga útil de la solicitud ni valores sensibles.

### Implementación

- [ ] T030 [US3] [BE] Crear `src/Aplicacion/Comun/Comportamientos/RegistroBehavior.cs`.
- [ ] T031 [US3] [BE] Crear `src/Api/Comun/RegistroEjecucionLogger.cs` (adaptador de `IRegistroEjecucion` con `ILogger`) y registrarlo.
- [ ] T032 [US3] [BE] Ejecutar `dotnet test --filter "RegistroBehaviorPruebas|ConsultarTiendaPruebas"` en verde.

## Fase 6: Historia 4 — Manejo uniforme de errores (P4)

**Prueba independiente**: un fallo inesperado devuelve el mismo formato ProblemDetails con correlación y sin detalles internos.

### Pruebas primero

- [ ] T033 [P] [US4] [BE] Escribir `src/Pruebas/Aplicacion/Comportamientos/ErroresBehaviorPruebas.cs`: error no controlado → `ExcepcionCasoDeUso` con `Inesperado` y mensaje genérico; cancelación → `Cancelado`; las excepciones ya uniformes pasan sin cambios. Debe fallar.
- [ ] T034 [P] [US4] [BE] Escribir `src/Pruebas/Aplicacion/Comportamientos/CorrelacionBehaviorPruebas.cs`: garantiza un `IdCorrelacion` no vacío; conserva el existente; genera uno si falta.
- [ ] T035 [P] [US4] [BE] Agregar a `ConsultarTiendaPruebas.cs`: la cabecera `X-Correlation-ID` se conserva si es válida (1–64 caracteres) y se genera y devuelve si falta o es inválida; el cuerpo de error no incluye trazas ni rutas internas.

### Implementación

- [ ] T036 [US4] [BE] Crear `src/Aplicacion/Comun/Comportamientos/CorrelacionBehavior.cs` y `ErroresBehavior.cs`.
- [ ] T037 [US4] [BE] Crear `src/Api/Comun/ContextoEjecucionHttp.cs` y `MiddlewareCorrelacionYErrores.cs` (cabecera `X-Correlation-ID` y traducción de `ErrorUniforme` a ProblemDetails con `codigo` e `idCorrelacion`).
- [ ] T038 [US4] [BE] Registrar en `src/Api/Comun/RegistroServicios.cs` los comportamientos genéricos con ámbito en este orden: `CorrelacionBehavior`, `ErroresBehavior`, `RegistroBehavior`, `ValidacionBehavior`; y registrar el middleware en `Program.cs`.
- [ ] T039 [US4] [BE] Ejecutar `dotnet test --filter "ErroresBehaviorPruebas|CorrelacionBehaviorPruebas|ConsultarTiendaPruebas"` en verde.

## Fase 7: Pulido y verificación final

- [ ] T040 [P] [BE] Confirmar que `src/Pruebas/Arquitectura` sigue en verde y que `Dominio` no depende de nada, `Aplicacion` solo de `Dominio`, y que `Api` no referencia manejadores concretos.
- [ ] T041 [P] [BE] Verificar que el orden del pipeline registrado coincide con `plan.md` (correlación, errores, registro, validación, manejador) con una prueba que lo inspecciona.
- [ ] T042 [MANUAL] Aprobar que `Tienda` sea compatible con el mapeo de EF Core de la historia 003 (ctor privado, propiedades con setter privado) antes de implementar esa historia.
- [ ] T043 [MANUAL] Decidir si 002b se registra como fila propia en `docs/scrum/backlog.md` (hoy el backlog solo lista la HU2 con `specs/002-estructura-base-api (parcial)`) y actualizar el `**Estado**` de `spec.md` tras aprobar la verificación final.
- [ ] T044 [BE] Verificación final: `dotnet build BlackHellHeim.sln` sin advertencias nuevas, `dotnet test BlackHellHeim.sln` en verde y llamadas `curl` de `inicio-rapido.md` (`/tiendas/BHH` 200, `/tiendas/B` 400, `/tiendas/ZZZ` 404, `/salud` 200).

## Dependencias

- Fase 1 → Fase 2 → US1 → US2, US3 y US4 (cada una añade un comportamiento al pipeline; se pueden hacer en cualquier orden tras US1, pero comparten `RegistroServicios.cs`, así que se editan en serie).
- Dependientes: las specs 003 y 004 requieren esta historia implementada (`Tienda`, `CodigoTienda`, `IContextoEjecucion`, `ErrorUniforme`, ProblemDetails).

## Ejecución en paralelo (ejemplos)

- Fase 2: T006, T007, T008, T009, T010 juntas.
- US1, pruebas: T011, T012, T013 juntas.
- US4, pruebas: T033, T034, T035 juntas.

## Estrategia

MVP = US1 (Fases 1 a 3). Luego US2, US3 y US4. Las pruebas de cada historia se ven fallar antes de implementar.
