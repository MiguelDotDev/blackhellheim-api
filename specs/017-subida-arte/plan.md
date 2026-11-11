# Plan 017: Subida de arte

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, IV, VII, VIII.

## Resumen
Validador en Aplicación (puro, sobre un flujo y metadatos), puerto `IAlmacenArchivos` con adaptador de Azure Blob (`Azure.Storage.Blobs`) y endpoint multipart. La decodificación de la imagen usa una biblioteca con licencia libre.

## Dependencias nuevas (licencia a verificar en NuGet al agregarlas)
| Paquete | Licencia | Nota |
|---|---|---|
| `Azure.Storage.Blobs` | MIT | Cliente oficial. |
| `Testcontainers.Azurite` (opcional) | MIT | Pruebas con Azurite real; alternativa: doble en memoria. |
| Decodificador PNG | **Decisión** | `ImageSharp` NO: licencia dividida (Six Labors Split License, comercial para uso en proyectos con ingresos). Se usa **`SkiaSharp`** (MIT) o, para solo leer cabecera/alfa, un lector PNG propio de ~120 líneas sobre `System.IO.Compression` (ChunkIHDR + IDAT). Candidatas libres: lector propio de cabecera PNG (IHDR) y/o `SkiaSharp` (MIT, con `NativeAssets.Linux.NoDependencies` si hace falta). **Decisión pendiente: se resuelve en T002.** |

## Decisiones
1. Validar en este orden barato→caro: tamaño (streaming con tope) → firma PNG → IHDR (tipo de color con alfa, dimensiones) → muestreo de transparencia.
2. Escribir primero a un archivo temporal acotado, validar, subir y borrar el temporal.
3. Hash SHA-256 calculado durante la lectura; lookup por hash antes de subir.
4. URL pública = `Url` del blob; en local, Azurite (`http://127.0.0.1:10000/devstoreaccount1/arte/...`).
5. Límite de cuerpo de Kestrel/formularios subido a 200 MB solo para esta ruta.
6. Cuenta de almacenamiento: `AlmacenArchivos__CadenaConexion` por entorno (sin valores en repo).

## Estructura
`Aplicacion/Arte/{IAlmacenArchivos,ValidadorArte,SubirArte}`, `Infraestructura/Almacen/AlmacenBlobAzure`, `Api/Endpoints/ArteAdminEndpoints.cs`.

## Pruebas
Unitarias del validador con PNG generados en la prueba (transparente, opaco, pequeño, grande, corrupto, APNG, JPEG renombrado), manejador con almacén doble, integración con Azurite, endpoint (códigos 201/400/401/403/413/415).

## Bloqueos y [MANUAL]
HU-13, HU-16. `[MANUAL]` crear la Storage Account y el contenedor `arte` público en Azure y cargar la cadena en secretos (HU-25).
