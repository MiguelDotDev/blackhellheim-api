# Tareas 024: Carrito de compras (parte API)

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero (constitución III). Formato: `- [ ] Tnnn [P] [USn] [etiqueta] descripción con ruta`.

## Fase 1: Preparación
- [ ] T001 [BE] Prueba de contrato de `contratos/openapi.yaml` en `src/Pruebas/Api` (falla hasta tener los endpoints); al final quitar `x-pendiente`.
- [ ] T002 [BE] Agregar `varianteId` (Guid) a `VarianteCatalogoDto` en `src/Aplicacion/Catalogo/Catalogo.cs`, a su consulta y a `specs/019-api-catalogo/contratos/openapi.yaml`; actualizar las pruebas de catálogo y de contrato.
- [ ] T003 [MANUAL] Coordinar con `blackhellheim-web` el carrito local de visitante y la llamada `POST /carrito/fusionar` tras iniciar sesión.

## Fase 2: Base común
- [ ] T004 [DB] Dominio `src/Dominio/Carritos`: `Carrito` (Id, UsuarioId, ActualizadoUtc) y `LineaCarrito` (Id, CarritoId, VarianteId, Cantidad tinyint) con reglas: cantidad entre 1 y 10, máximo 20 líneas distintas, fusión que suma con tope 10.
- [ ] T005 [DB] Configuración EF y migración `AgregarCarrito`: `Carritos` FK a usuarios en cascada y único (UsuarioId); `LineasCarrito` FK a Carritos en cascada y a Variantes, único (CarritoId, VarianteId) y restricción CHECK de `Cantidad` entre 1 y 10; actualizar la prueba que cuenta migraciones.
- [ ] T006 [BE] Opciones `Carrito__MaxLineas`=20 y `Carrito__MaxCantidad`=10 validadas al arrancar, puerto `IRepositorioCarritos` en `src/Aplicacion/Carritos` y registro en `src/Api/Comun/RegistroCarritos.cs`.

## Fase 3: US1 ver y editar el carrito (P1)
Criterio independiente: con sesión se crea, actualiza, elimina y consulta una línea con precios del servidor.
- [ ] T007 [P] [BE] [US1] Pruebas de dominio y de `FijarLineaCarrito`/`ObtenerCarrito`/`VaciarCarrito`: éxito, cantidad 0 elimina, cantidad 11 o -1 → 400, variante inexistente/inactiva/no publicada → 400, línea 21 → 400, variante desactivada después → `disponible: false` sin sumar al total, carrito vacío, sin sesión → 401.
- [ ] T008 [BE] [US1] Casos de uso y repositorio (`src/Aplicacion/Carritos`, `src/Infraestructura/Carritos`); el DTO se arma con `ICalculadoraPrecios.PrepararAsync` y los datos de variante y diseño.
- [ ] T009 [BE] [US1] `src/Api/Endpoints/CarritoEndpoints.cs`: `GET /carrito`, `PUT /carrito/lineas/{varianteId}`, `DELETE /carrito` (solo con sesión, DTO y ProblemDetails).

## Fase 4: US2 fusionar (P1)
Criterio independiente: fusionar un carrito de visitante con líneas válidas e inválidas conserva las válidas e informa las omitidas.
- [ ] T010 [P] [BE] [US2] Pruebas de `FusionarCarrito`: suma con tope 10, variantes nuevas, línea inválida omitida e informada (`omitidas` con motivo), más de 20 líneas → 400, carrito vacío, fusionar dos veces suma.
- [ ] T011 [BE] [US2] `FusionarCarrito` y `POST /carrito/fusionar`.

## Fase 5: US3 el servidor decide el precio (P1)
- [ ] T012 [P] [BE] [US3] Pruebas: un campo `precio` en el cuerpo se ignora; sin tipo de cambio → 503 con ProblemDetails y sin precios inventados; CE-001 y CE-003.
- [ ] T013 [BE] [US3] Mapeo del 503 sin tipo de cambio y ajustes que dejen las pruebas en verde.

## Fase 6: Cierre
- [ ] T014 [P] [BE] SQL Server real: único por usuaria y por línea, CHECK de cantidad, cascada, CE-002 (20 líneas bajo 500 ms) en `src/Pruebas/Carritos`.
- [ ] T015 [BE] Quitar `x-pendiente` del contrato, `dotnet build`/`dotnet test` en verde, tareas marcadas, fila de `docs/scrum/backlog.md` = Hecha y commit `HU-24: carrito`.

## Dependencias y paralelismo
Fase 2 bloquea las historias; T002 antes de T008. US2 y US3 usan el DTO de US1 (T008). MVP = fases 1–3.

**Trazabilidad**: RF-001→T007–T011; RF-002→T004–T006,T014; RF-003→T012–T013; RF-004→T010–T011; RF-005→T007; RF-006→todas; RF-007→T002; CE-001→T012; CE-002→T014; CE-003→T010.
