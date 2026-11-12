# Validación 026
Prerrequisitos: `dotnet`, HU-15/16/20 integradas.
1. `dotnet test --filter "FullyQualifiedName~Envio"`: caso de uso, adaptador y endpoint con dobles (sin red).
2. `dotnet test --filter "FullyQualifiedName~Contrato"`: el documento publicado coincide con [contratos/openapi.yaml](contratos/openapi.yaml).
3. Con la API en marcha, sesión y variantes cargadas: `POST /envios/cotizar` con un domicilio y una línea devuelve métodos con `costoMxn` entero; repetirlo en menos de 5 min no llama de nuevo a Printful (visible en el registro del doble).
