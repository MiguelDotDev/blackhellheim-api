# Validación 029
Prerrequisitos: `dotnet`, Docker (SQL Server de pruebas), HU-22 y HU-28 integradas.
1. `dotnet test --filter "FullyQualifiedName~Pago|FullyQualifiedName~Stripe"`: verificador, casos de uso, consumidores, endpoint y SQL Server con dobles (sin red).
2. `dotnet test --filter "FullyQualifiedName~Contrato"`: el documento publicado coincide con [contratos/openapi.yaml](contratos/openapi.yaml).
3. `[MANUAL]` con claves de prueba de Stripe y `stripe listen --forward-to`: pagar con la tarjeta de prueba marca el pedido `Pagado`; la ficha de OXXO lo deja `PendientePago`.
