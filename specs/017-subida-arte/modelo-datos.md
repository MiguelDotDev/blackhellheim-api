# Modelo de datos 017
Reutiliza `ArchivosArte` de la HU-16, agrega por **migración** `AgregarHashArte` (columna `HashSha256 char(64)` con índice único) si HU-16 no la incluyó. Contenedor de Blob: `arte` (acceso público de lectura de blobs). Nombre: `{guid}.png`.
