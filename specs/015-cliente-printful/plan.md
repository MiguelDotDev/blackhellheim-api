# Plan 015: Cliente de Printful

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, IV, VII, VIII.

## Resumen
Puerto `IProveedorImpresion` (Aplicación) y adaptador `ProveedorImpresionPrintful` (Infraestructura) con `HttpClient` tipado y un `DelegatingHandler` propio de reintentos (sin paquetes nuevos). Casos de uso Mediator `ObtenerProducto`, `ListarProductos`, `ListarVariantes` y endpoints de administración.

## Contexto técnico
.NET 10; `IHttpClientFactory` y `IMemoryCache` ya en el framework. **Dependencias nuevas: ninguna** (el reintento es un handler de ~60 líneas; evita `Microsoft.Extensions.Http.Resilience` y Polly, ambos MIT, si más adelante se prefiere).

## Decisiones
1. API v2 de catálogo de Printful (`/v2/catalog-products`, `/catalog-variants`, disponibilidad por producto) con `Authorization: Bearer <token>`; las rutas exactas se verifican contra la documentación vigente en la tarea T002 y se anotan en `investigacion` del plan.
2. Configuración `Printful__Token`, `Printful__UrlBase`, `Printful__TiempoEsperaSegundos`; sin valor en repo.
3. `ManejadorReintentos`: 429 → `Retry-After` (segundos o fecha, tope 60 s, 3 intentos); 5xx/`HttpRequestException` → 0,5·2ⁿ s con jitter; 4xx → sin reintento. Reloj y espera inyectables para pruebas sin dormir.
4. Mapeo de errores a `ExcepcionCasoDeUso` con `TipoError` existentes; 503 incluye `retryAfter`.
5. Caché de 10 min por (producto, técnica, país).
6. Pruebas con `HttpMessageHandler` falso; nada de red en CI.

## Estructura
`Aplicacion/Impresion/{IProveedorImpresion,Modelos,Consultas}`, `Infraestructura/Printful/{ProveedorImpresionPrintful,ManejadorReintentos,OpcionesPrintful,DtosPrintful}`, `Api/Endpoints/PrintfulAdminEndpoints.cs`.

## Bloqueos y [MANUAL]
HU-13 (autorización por rol) para los endpoints: hasta entonces los casos de uso se prueban sin HTTP. `[MANUAL]` crear la cuenta de Printful y el token privado y guardarlo en `.env`/secretos de Azure.
