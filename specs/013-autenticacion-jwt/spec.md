# Especificación 013: Registro e inicio de sesión con JWT

**Historia**: HU-13 (Sprint 3) · **Repos**: api · **Estado**: Plan cerrado · **Creada**: 2026-11-11

**Entrada**: «Como cliente, quiero registrarme e iniciar sesión con JWT de acceso corto y refresh rotativo, con roles Cliente y Administrador. Criterio: un refresh reusado revoca la familia de tokens; pruebas de caso exitoso, error y validación.»

**Bloqueado por**: nada (la HU-14 web y la HU-28 consumen esta historia). Los endpoints ya existentes no cambian.

## Escenarios de uso y pruebas

### HU 1 — Registro de cliente (P1)
Como visitante quiero crear una cuenta con correo y contraseña para poder comprar (la compra exige sesión, constitución VIII).

**Prueba independiente**: registrar un correo nuevo y obtener sesión iniciada con rol Cliente.

1. **Dado** un correo no registrado y una contraseña válida, **cuando** me registro, **entonces** se crea la cuenta con rol Cliente y recibo un token de acceso y la cookie de refresh.
2. **Dado** un correo ya registrado, **cuando** me registro, **entonces** recibo un error 409 ProblemDetails sin revelar datos de la otra cuenta.
3. **Dado** un correo mal formado o una contraseña que no cumple la política, **cuando** me registro, **entonces** recibo 400 ProblemDetails con los campos inválidos.
4. **Dado** que envío un campo `rol`, **cuando** me registro, **entonces** se ignora: nadie se auto-asigna Administrador.

### HU 2 — Inicio de sesión (P1)
1. **Dado** credenciales correctas, **cuando** inicio sesión, **entonces** recibo acceso (vida ~15 min) y refresh en cookie HttpOnly, Secure, SameSite=Lax.
2. **Dado** credenciales incorrectas o correo inexistente, **cuando** inicio sesión, **entonces** recibo 401 con el mismo mensaje en ambos casos.
3. **Dados** 5 intentos fallidos seguidos, **cuando** intento otra vez, **entonces** la cuenta se bloquea 15 minutos y recibo 429/423 ProblemDetails.

### HU 3 — Renovación rotativa y detección de reuso (P1)
1. **Dado** un refresh vigente, **cuando** pido renovar, **entonces** recibo un acceso nuevo y un refresh nuevo, y el anterior queda inutilizable.
2. **Dado** un refresh ya usado (rotado), **cuando** lo presento, **entonces** se revoca toda la familia de tokens de esa sesión, recibo 401 y la cookie se borra.
3. **Dado** un refresh caducado, revocado o inexistente, **cuando** lo presento, **entonces** recibo 401 y la cookie se borra.
4. **Dadas** dos renovaciones simultáneas con el mismo refresh, **entonces** solo una tiene éxito y la otra se trata como reuso.

### HU 4 — Cierre de sesión y roles (P2)
1. **Cuando** cierro sesión, **entonces** la familia del refresh se revoca y la cookie se borra (idempotente: sin cookie también devuelve 204).
2. **Dado** un acceso con rol Cliente, **cuando** llamo a un recurso de Administrador, **entonces** recibo 403; sin token, 401.
3. **Cuando** consulto mi perfil con un acceso válido, **entonces** veo mi correo y mi rol; con acceso caducado, 401.
4. **Dado** que el sistema arranca por primera vez, **entonces** existen los roles Cliente y Administrador y, si hay credenciales de administrador configuradas, una cuenta Administrador (nunca desde el código fuente).

### Casos límite
Correo con mayúsculas/espacios (se normaliza); contraseña vacía o de >128 caracteres; cuerpo JSON ausente; cookie manipulada; reloj desfasado (tolerancia 0 para el acceso); usuario eliminado con refresh vivo (401); refresh enviado en el cuerpo en vez de cookie (se ignora).

## Requisitos funcionales
- **RF-001** Registro por correo y contraseña; rol asignado siempre Cliente.
- **RF-002** Política de contraseña: mínimo 10 caracteres, con letra y número; máximo 128.
- **RF-003** Inicio de sesión con respuesta uniforme ante credencial inválida; bloqueo tras 5 fallos por 15 min.
- **RF-004** Token de acceso JWT firmado, vida 15 min, con id de usuario y rol; sin datos sensibles.
- **RF-005** Refresh opaco aleatorio, entregado solo en cookie HttpOnly/Secure/SameSite=Lax, vida 7 días, guardado únicamente como hash; nunca aparece en cuerpos ni registros.
- **RF-006** Cada renovación rota el refresh y lo encadena a una familia; reutilizar uno ya rotado revoca la familia completa.
- **RF-007** Cierre de sesión revoca la familia y borra la cookie.
- **RF-008** Roles Cliente y Administrador sembrados por migración; el Administrador inicial se crea desde configuración de entorno, nunca del repo.
- **RF-009** Los errores usan ProblemDetails con correlación (comportamiento existente); no se distingue «correo inexistente» de «contraseña errónea».
- **RF-010** Contrato en `contratos/openapi.yaml` antes del código; los controladores devuelven DTOs, nunca entidades de Identity.
- **RF-011** Los tokens vencidos de refresh se pueden purgar (consulta de limpieza disponible; programarla queda fuera).

## Entidades clave
Usuario (identidad, correo, rol), Rol, TokenRefresco (familia, hash, expiración, estado). Ver `modelo-datos.md`.

## Criterios de éxito
- **CE-001** Registro e inicio de sesión completan en menos de 2 s en condiciones normales.
- **CE-002** El 100 % de los refresh reutilizados revoca su familia (prueba automática).
- **CE-003** Cero tokens, hashes o contraseñas en respuestas de error y registros.
- **CE-004** Cada regla tiene pruebas de éxito, error y validación (constitución III).

## Supuestos y fuera de alcance
- Fuera: recuperación de contraseña, confirmación de correo, inicio con redes sociales, MFA, pantallas (HU-14), enviar correos.
- Se asume el mismo origen web/API mediante el proxy `/api` (HU-25), por lo que la cookie SameSite=Lax funciona sin CORS.
- La clave de firma y las credenciales del administrador inicial son secretos de entorno `[MANUAL]`.
