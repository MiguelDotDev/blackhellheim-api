# Contrato de despliegue (sin OpenAPI: no hay endpoints nuevos)
- `GET /salud` → 200 `{ "estado": "ok" }` (existente) es la sonda de la revisión.
- Variables de entorno esperadas por la API: `ConnectionStrings__BlackHellHeim`, `Jwt__*`, `Administrador__*`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.
- Etiquetas de imagen: `ghcr.io/<propietario>/blackhellheim-api:<sha>` y `.../blackhellheim-migraciones:<sha>`.
- Variables del repositorio en GitHub (no secretas): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.
