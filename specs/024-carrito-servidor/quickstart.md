# Validación 024
Prerrequisitos: `dotnet`, Docker (SQL Server de pruebas), HU-13/16/19/20 integradas.
1. `dotnet test --filter "FullyQualifiedName~Carrito"`: dominio, casos de uso, endpoints y SQL Server con dobles.
2. `dotnet test --filter "FullyQualifiedName~Contrato"`: el documento publicado coincide con [contratos/openapi.yaml](contratos/openapi.yaml) y con el de la 019 (con `varianteId`).
3. Con la API en marcha y sesión: `PUT /carrito/lineas/{varianteId}` con cantidad 2, `GET /carrito` (precios calculados por el servidor), `POST /carrito/fusionar` con una línea válida y una inválida.
