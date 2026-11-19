# Validación de la imagen Docker con SkiaSharp (HU-17)

Fecha: 2026-11-12 · Rama `sprint/05`.

## Resultado
`docker compose up -d --build` construye la imagen `final` y levanta SQL Server, migraciones, Azurite y API (healthy). **No hizo falta cambiar el Dockerfile**: el paquete `SkiaSharp.NativeAssets.Linux.NoDependencies` (sin dependencias del sistema) funciona sobre `aspnet:10.0` sin instalar nada.

## Prueba de extremo a extremo (dentro del contenedor)
PNG RGBA de 4500×5400, tercio central opaco y laterales transparentes:

| Entrada | Respuesta |
|---|---|
| PNG con laterales transparentes | `201`, 4500×5400, subido a Azurite |
| PNG totalmente opaco | `400 arte-sin-transparencia` |

Que el opaco se rechace con ese código y el transparente se acepte prueba que Skia decodifica y mide la transparencia en el contenedor (si faltara la librería nativa, sería `500`).

## Cómo repetirla
Con `BHH_JWT_CLAVE`, `BHH_ADMIN_CORREO` y `BHH_ADMIN_CONTRASENA` definidos: `docker compose up -d --build`, iniciar sesión en `/auth/inicio-sesion` y subir el PNG con `POST /admin/arte` (multipart, campo `archivo`).
