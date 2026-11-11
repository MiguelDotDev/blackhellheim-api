# Tareas 013: Registro e inicio de sesión con JWT

Etiquetas: [BE] [DB] [CI] [MANUAL]. Pruebas primero (constitución III). `[P]` paralelizable.

## Fase 1: Contrato y preparación
- [ ] T001 [BE] Copiar `contratos/openapi.yaml` a la ubicación que lee `ContratoOpenApiPruebas` y escribir primero la prueba de contrato (falla).
- [ ] T002 [BE] Agregar `Identity.EntityFrameworkCore` y `Authentication.JwtBearer` 10.0.x, verificar licencia MIT, regenerar `packages.lock.json` de Infraestructura y Api, y confirmar `restore --locked-mode`.

## Fase 2: Dominio y aplicación (pruebas primero)
- [ ] T003 [P] [BE] Pruebas de `TokenRefresco`: rotar activo, rotar usado (reuso), caducado, revocado.
- [ ] T004 [BE] `Dominio/Autenticacion/TokenRefresco` y `Roles`.
- [ ] T005 [P] [BE] Puertos `IServicioIdentidad`, `IEmisorTokens`, `IRepositorioTokensRefresco` y DTOs.
- [ ] T006 [P] [BE] Pruebas de validadores (correo, contraseña 10–128, letra+número) y de manejadores `Registrar`, `IniciarSesion`, `Renovar`, `CerrarSesion`, `ObtenerPerfil` (éxito, error, validación, reuso revoca familia, rol ignorado).
- [ ] T007 [BE] Implementar validadores y manejadores hasta pasar T006.

## Fase 3: Infraestructura
- [ ] T008 [DB] `Usuario`, configuración EF de `TokensRefresco`, `ContextoBlackHellHeim` hereda de `IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>`; migración `AgregarIdentidad` con semilla de roles.
- [ ] T009 [BE] Adaptadores de identidad y emisor JWT (HS256, 15 min, `ClockSkew` 0); refresh CSPRNG con hash SHA-256; rotación atómica con `ExecuteUpdate`.
- [ ] T010 [BE] Prueba de integración (Testcontainers) de rotación concurrente: dos renovaciones simultáneas, solo una gana.
- [ ] T011a [BE] Prueba y método `PurgarCaducadosAsync` del repositorio de refresh (RF-011).
- [ ] T011 [BE] Siembra del administrador inicial desde `Administrador__*` (omitida si no hay valores; nunca registra la contraseña).

## Fase 4: API
- [ ] T012 [BE] Pruebas de endpoints (registro 201/400/409, inicio 200/401/423, renovar 200/401 y reuso, cerrar 204, `/auth/yo` 200/401, 403 por rol, atributos de la cookie, ausencia de secretos en respuestas).
- [ ] T013 [BE] `AuthEndpoints`, autenticación JWT, autorización por rol y errores 401/403 en ProblemDetails; fallo de arranque sin `Jwt__Clave`; `Secure` siempre activo salvo `Development` (con prueba); ajustar `OrdenDelPipelinePruebas`.
- [ ] T014 [BE] Actualizar `compose.yaml`/`.env.example` con nombres de variables sin valores reales; documentar en `inicio-rapido`.

## Fase 5: Cierre
- [ ] T015 [CI] `dotnet build` y `dotnet test` completos en verde; Sonar/CodeQL sin hallazgos bloqueantes.
- [ ] T016 [MANUAL] Definir `Jwt__Clave` (aleatoria ≥32 bytes) y credenciales del administrador en el `.env` local; en producción, como secretos de Azure (HU-25).
- [ ] T017 [BE] Actualizar `docs/scrum/backlog.md` (fila HU-13: Spec y Estado) y commit `HU-13: registro e inicio de sesión con JWT`.

**Trazabilidad**: RF-001/002→T006-7; RF-003→T006,T012; RF-004/005→T009,T012; RF-006→T003-4,T010; RF-007→T012; RF-008→T008,T011; RF-009→T012-13; RF-010→T001,T013; RF-011→T011a; CE-002→T003,T010; CE-003→T012.
