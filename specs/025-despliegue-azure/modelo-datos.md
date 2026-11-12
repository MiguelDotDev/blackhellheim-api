# Modelo de datos 025
No introduce entidades ni migraciones. Define el **inventario de recursos Azure** (nombres con prefijo `bhh`):

| Recurso | Nombre | Notas |
|---|---|---|
| Grupo de recursos | `bhh-prod-rg` | Alcance del rol de OIDC. |
| Entorno Container Apps | `bhh-prod-env` | Log Analytics integrado. |
| Container App API | `bhh-api` | Ingress externo 8080, modo de revisiones múltiples, mínimo 1 réplica (sin escala a cero por webhooks, HU-40). |
| Job de migraciones | `bhh-migraciones` | Disparo manual desde el flujo; imagen `migraciones`. |
| Azure SQL | `bhh-sql` / `bhh-db` | Oferta gratuita si disponible; firewall solo para servicios de Azure. |
| Registro de aplicación | `bhh-github-oidc` | 2 credenciales federadas (`main`, entorno `produccion`). |
| Secretos ACA | `cadena-conexion`, `jwt-clave`, `admin-correo`, `admin-contrasena` | `secretref`. |
