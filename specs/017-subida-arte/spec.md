# Especificación 017: Subida de arte a Azure Blob con validaciones

**Historia**: HU-17 (Sprint 4) · **Repos**: api · **Estado**: Hecha · **Creada**: 2026-11-11

**Entrada**: «Como administradora, quiero subir el arte a Azure Blob con validaciones. Criterio: rechaza lo que no sea PNG con fondo transparente, menor de 200 MB y de resolución mínima; devuelve URL pública.»

**Bloqueado por**: HU-13 (rol Administrador) y HU-16 (tabla `ArchivosArte`). Cuenta de Azure Storage de producción: `[MANUAL]` (HU-25); en local se usa Azurite (compose ya lo levanta).

## Escenarios de uso y pruebas

### HU 1 — Subir un arte válido (P1)
1. **Dado** un PNG con canal alfa y fondo realmente transparente (al menos 5 % de píxeles transparentes incluyendo las 4 esquinas), de resolución mínima configurable (valor confirmado 1800×2400 px, T003a) y ≤200 MB, **cuando** lo subo, **entonces** se guarda en el contenedor público de arte con nombre generado, se registra en `ArchivosArte` y recibo `201` con id, URL pública, ancho, alto y tamaño.
2. **Dado** el mismo contenido subido dos veces (mismo SHA-256), **entonces** se reutiliza el existente y no se duplica el blob.

### HU 2 — Rechazos con motivo claro (P1)
Cada rechazo es `400`/`413`/`415` ProblemDetails con un código estable:
1. Extensión o `Content-Type` distinto de PNG → 415 `arte-no-png`; se valida también la firma binaria (los 8 bytes de PNG) para no confiar en el nombre.
2. PNG sin canal alfa o con fondo opaco (esquinas opacas o <5 % transparente) → 400 `arte-sin-transparencia`.
3. Resolución menor a la mínima → 400 `arte-resolucion-insuficiente`.
4. Tamaño >200 MB → 413 `arte-demasiado-grande`, cortado en streaming sin leer todo.
5. Archivo vacío, corrupto o truncado → 400 `arte-ilegible`.
6. Sin rol Administrador → 403; sin sesión → 401.

### HU 3 — Seguridad (P1)
1. El nombre del blob lo genera el servidor (GUID); el nombre original nunca forma la ruta (sin path traversal).
2. Los metadatos del PNG no se confían; la imagen se decodifica de forma segura con límites de memoria y de dimensiones máximas (por omisión 100 megapíxeles en total, configurable; con 24 MP de mínimo, un PNG de 20 000 px por lado ocuparía unos 1,6 GB) para evitar bombas de descompresión.
3. El almacenamiento solo lo accede el API por el puerto `IAlmacenArchivos`; la cadena de conexión viene de configuración.

### Casos límite
Subida cancelada a mitad (no queda blob huérfano); falla del almacenamiento (503); PNG animado (APNG: rechazo); PNG de 16 bits; perfil de color; nombres Unicode; dos subidas concurrentes del mismo contenido; Azurite apagado en local.

## Requisitos funcionales
- **RF-001** Puerto `IAlmacenArchivos` (`GuardarAsync`, `ExisteAsync`, `EliminarAsync`) con adaptador Azure Blob y doble en memoria para pruebas.
- **RF-002** Validador de arte (firma PNG, alfa, transparencia, resolución mínima, tamaño, dimensiones máximas).
- **RF-003** `POST /admin/arte` multipart; devuelve DTO con URL pública.
- **RF-004** Deduplicación por SHA-256.
- **RF-005** Límites configurables (`Arte__MinAncho`, `MinAlto`, `MaxBytes`). Resolución mínima confirmada: **1800×2400 px** (150 DPI en el área de impresión de 12×16 pulgadas). Fuente: guía de Printful sobre archivos de impresión (mínimo 150 DPI para DTG; 300 DPI ideal; área de 12×16 pulgadas, 15×18 en algunos productos).
- **RF-006** Registro en `ArchivosArte` (HU-16) sin migración propia.
- **RF-007** Nada de secretos en el repo; Azurite local con su clave pública de desarrollo documentada, no la de producción.

## Criterios de éxito
- **CE-001** Cada motivo de rechazo tiene una prueba y un código estable.
- **CE-002** Un PNG de 50 MB válido se sube en menos de 10 s en red local.
- **CE-003** Un archivo de 201 MB se rechaza sin cargarlo completo en memoria.

## Fuera de alcance
Miniaturas, CDN, edición de imágenes, mockups (HU-21), detección de arte negro (HU-18, web), antivirus. El contenedor de producción y su acceso público: `[MANUAL]`.
