# Validación 028
Prerrequisitos: `dotnet`, Docker (SQL Server de pruebas), HU-13/16/20 integradas.
1. `dotnet test --filter "FullyQualifiedName~Pedido"`: dominio, casos de uso, endpoints y SQL Server.
2. `dotnet test --filter "FullyQualifiedName~Contrato"`: el documento publicado coincide con [contratos/openapi.yaml](contratos/openapi.yaml).
3. Con sesión: `POST /pedidos` con `Idempotency-Key: abc` dos veces → mismo `id`, un solo pedido; con otro cuerpo → `409`.
