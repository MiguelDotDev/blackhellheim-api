# Constitución de blackhellheim-api

Versión 1.0.0 · Ratificada 2026-11-05 · Última modificación 2026-11-05

Tienda de playeras Print-On-Demand para México (MXN, español). Este repositorio es la API (.NET 10). El frontend vive en `blackhellheim-web`.

## Principios

### I. Arquitectura limpia (NO NEGOCIABLE)
Capas: `Dominio`, `Aplicacion`, `Infraestructura`, `Api`, `Pruebas`. Las dependencias apuntan hacia adentro: Dominio no depende de nada; Aplicación solo de Dominio. Los controladores nunca exponen entidades de dominio, solo DTOs. Casos de uso con CQRS usando Mediator (MIT) y comportamientos de pipeline (validación, registro, transacción).

### II. Contrato primero
Todo endpoint se define en OpenAPI (`specs/NNN-*/contratos/openapi.yaml`) antes de implementarse. El frontend se desarrolla contra ese contrato y un servidor mock. Los errores usan ProblemDetails. La documentación se sirve con Scalar.

### III. Reglas de negocio probadas
Toda regla de negocio tiene pruebas unitarias de caso exitoso, caso de error y validación. Sin pruebas no hay "Hecho".

### IV. Solo software libre o gratuito
No se usan MediatR 13+, AutoMapper, FluentAssertions ni otras dependencias de licencia comercial. Toda dependencia nueva requiere verificar su licencia y anotarla en el plan.

### V. El precio lo calcula siempre el backend
El cliente nunca envía precios. Fórmula: `costo_mxn = (costo_base_usd + impresion_extra_usd) × tipo_cambio × (1 + colchon_cambiario)`; `precio = redondeo(costo_mxn ÷ (1 − margen − comision_pasarela))`, terminado en 9 o 90. El precio se congela en el pedido al confirmarse el pago.

### VI. Pedidos idempotentes y webhooks verificados
La creación de pedidos usa tabla outbox. Todo webhook (Stripe, Printful) verifica firma y es idempotente ante reintentos.

### VII. Puertos y adaptadores
Los proveedores externos se acceden solo mediante interfaces de Aplicación: `IProveedorImpresion` (Printful), `IPasarelaPago` (Stripe), `IAlmacenArchivos` (Azure Blob/Azurite), `IProveedorTipoCambio` (Banxico), `IEnvioNotificaciones` (Web Push). Las pruebas usan dobles; nunca se llama a un proveedor real en CI.

### VIII. Seguridad y secretos
Ningún secreto en el repositorio (repositorio público). Identity + JWT: acceso de ~15 min, refresh rotatorio en cookie HttpOnly/Secure/SameSite=Lax guardado con hash; la reutilización de un refresh revoca la familia. Comprar exige sesión iniciada; no existe compra como invitado.

### IX. Código de creadora mínimo
El código no da descuento ni comisión. Solo se guarda en el pedido (un código por pedido). Cuenta como venta únicamente un pedido en estado "entregado"; cancelados o reembolsados no cuentan. El administrador marca la entrega manualmente.

### X. Diseños originales
Solo diseños propios. Prohibido usar logotipos o nombres de bandas o marcas de terceros.

### XI. Idioma y estilo
Especificaciones, código, commits y documentación en español. Marca escrita **BlackHellHeim**.

## Flujo de trabajo
- Una especificación por historia del backlog, numerada con el número de la historia (`specs/NNN-slug`). Si la historia toca ambos repositorios, el backend es dueño del contrato.
- Flujo: `/speckit.specify` → `/speckit.clarify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.analyze` → `/speckit.implement`.
- Sprints de 1 semana; WIP máximo 2; Jira proyecto `BHH`.
- Definición de Hecho: pruebas unitarias, CI en verde, análisis estático limpio (SonarQube Cloud, CodeQL), especificación actualizada, OpenAPI actualizado, migración incluida.

## Gobernanza
Esta constitución prevalece sobre otras prácticas. Cambiarla exige actualizar la versión y justificar el cambio en el commit.
