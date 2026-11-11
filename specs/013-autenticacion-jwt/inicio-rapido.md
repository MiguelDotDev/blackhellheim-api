# Inicio rápido 013: autenticación

1. En tu `.env` local (no versionado) define `BHH_JWT_CLAVE` con una clave aleatoria de al menos 32 caracteres (`openssl rand -base64 48`). Opcional: `BHH_ADMIN_CORREO` y `BHH_ADMIN_CONTRASENA` crean el administrador inicial al arrancar (la contraseña cumple 10+ caracteres con letras y números).
2. `docker compose up -d` aplica la migración `AgregarIdentidad` (tablas de Identity, `TokensRefresco` y los roles Cliente y Administrador).
3. `POST /auth/registro` y `POST /auth/inicio-sesion` devuelven el acceso en el cuerpo y el refresh en la cookie `refresh` (HttpOnly, Secure, SameSite=Lax). En desarrollo local por HTTP (`ASPNETCORE_ENVIRONMENT=Development`) `Secure` se relaja.
4. La API no arranca sin `Jwt__Clave` válida. En producción las claves son secretos de Azure (HU-25).
