# Validación 030
Prerrequisitos: `dotnet`, Docker (SQL Server de pruebas), HU-28 y HU-29 integradas.
1. `dotnet test --filter "FullyQualifiedName~Borrador|FullyQualifiedName~Salida"`: caso de uso, despachador, adaptador y concurrencia con dobles (sin red).
2. Con la bandera apagada (por omisión) un pedido pagado queda con borrador `Creado` y el pedido en `Pagado`.
3. `[MANUAL]` con token real: comprobar el borrador en el panel de Printful antes de encender `Printful__ConfirmarPedidosReales`.
