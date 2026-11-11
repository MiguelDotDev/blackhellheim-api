# Plan 013: Registro e inicio de sesión con JWT

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, IV, VII, VIII.

## Resumen
ASP.NET Core Identity (usuarios, roles, hash, bloqueo) en Infraestructura; emisión y validación de JWT con `JwtBearer`; refresh opaco propio con tabla `TokensRefresco`. Casos de uso con Mediator en `Aplicacion/Autenticacion`; el dominio solo conoce `TokenRefresco` y la regla de rotación.

## Contexto técnico
C#/.NET 10, EF Core 10 + SQL Server (HU-3), xUnit + Testcontainers ya presentes.

**Dependencias nuevas (licencia verificada)**
| Paquete | Versión | Licencia |
|---|---|---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.x (alineada con EF 10.0.15) | MIT |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.x | MIT |
Se verificará la licencia en NuGet al agregarlas (regla 8 de AGENTS.md). Sin MediatR, AutoMapper ni FluentAssertions.

## Estructura
- `Dominio/Autenticacion/`: `TokenRefresco` (con `Rotar`, `Revocar`, `EstaActivo`), constantes `Roles`.
- `Aplicacion/Autenticacion/`: comandos `Registrar`, `IniciarSesion`, `Renovar`, `CerrarSesion`, consulta `ObtenerPerfil`; puertos `IServicioIdentidad` (crear/validar usuario), `IEmisorTokens` (JWT + refresh), `IRepositorioTokensRefresco`; DTOs; validadores (IValidadorSolicitud existente).
- `Infraestructura/Identidad/`: `Usuario : IdentityUser<Guid>`, adaptadores, configuración EF, migración `AgregarIdentidad`, siembra de roles y administrador inicial.
- `Api/Endpoints/AuthEndpoints.cs`: grupo `/auth`, cookie `refresh` (HttpOnly, Secure, SameSite=Lax, Path=/, 7 días), `UseAuthentication/UseAuthorization` después de correlación/errores (respetar `OrdenDelPipelinePruebas`), 401/403 como ProblemDetails.

## Decisiones
1. **Refresh propio, no el de Identity**: control de familia y reuso. Valor = 32 bytes aleatorios (CSPRNG) base64url; se guarda SHA-256.
2. **Rotación atómica**: `UPDATE ... WHERE UsadoUtc IS NULL` (ExecuteUpdate); 0 filas ⇒ reuso ⇒ revocar familia.
3. **Config por entorno**: `Jwt__Clave` (≥32 bytes), `Jwt__Emisor`, `Jwt__Audiencia`, `Administrador__Correo`, `Administrador__Contrasena`. Sin valores en el repo; `compose.yaml` los toma de variables; la API falla al arrancar si falta `Jwt__Clave` fuera de Pruebas.
4. **HS256** con clave simétrica (un solo emisor/validador); `ClockSkew = 0`.
5. **Detrás de proxy**: `UseForwardedHeaders` para que `Secure` se evalúe con HTTPS real (coordinada con HU-25).
6. **Límite de intentos**: bloqueo de Identity (5 fallos / 15 min); no se agrega limitador de tasa en esta HU.

## Pruebas
Unitarias de dominio (rotación, reuso, expiración), de manejadores con dobles de los puertos, de validadores, de endpoints con `FabricaApiPruebas` + Testcontainers (flujo completo, cookie, 401/403/409/423) y de contrato (comparador existente contra el OpenAPI).

## Bloqueos y [MANUAL]
Ninguno técnico. `[MANUAL]`: definir `Jwt__Clave` y credenciales del administrador en el `.env` local (no versionado) y, en producción, en los secretos de Azure (HU-25).
