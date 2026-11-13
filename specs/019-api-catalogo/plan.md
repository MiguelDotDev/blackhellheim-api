# Plan 019: API de catálogo

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, V. **Dependencias nuevas**: ninguna.

## Resumen
Dos consultas Mediator de solo lectura (`ListarCatalogo`, `ObtenerPlayera`) sobre un puerto `IConsultasCatalogo` implementado en Infraestructura con proyecciones EF (`AsNoTracking`, `Select` a DTO). Endpoints públicos con `Cache-Control` corto y `ETag` derivado de los datos.

## Decisiones
1. Paginación por desplazamiento (página/tamaño); suficiente para el volumen del proyecto.
2. **Precio no persistido**: el filtro y el orden por precio no pueden resolverse en SQL (el precio se calcula con el tipo de cambio del día). Se carga la proyección ligera de variantes activas de los diseños publicados que cumplen los demás filtros, se calcula el precio en memoria con `ICalculadoraPrecios` (un solo tipo de cambio por petición), y se filtra, ordena y pagina en memoria. Válido hasta unos miles de diseños; si crece, HU futura persistirá el precio por día.
3. Búsqueda de texto con `LIKE` parametrizado y colación insensible; sin índice de texto completo.
4. El precio llega por el puerto `ICalculadoraPrecios` de HU-20 (se amplió con `PrepararAsync`, que fija tipo de cambio y parámetros una sola vez por petición). Sin tipo de cambio el precio es `null` y no se inventa. `disponible` es `true` para toda variante activa: el stock en tiempo real de Printful no se guarda (se evaluará al integrar pedidos).
5. Orden estable con desempate por `Id`.
6. Fábrica de datos de prueba en `Pruebas` (sin red); pruebas de endpoint con Testcontainers.

## Estructura
`Aplicacion/Catalogo/{ListarCatalogo,ObtenerPlayera,IConsultasCatalogo,Dtos}`, `Infraestructura/Catalogo/ConsultasCatalogo`, `Api/Endpoints/CatalogoEndpoints.cs`.

## Bloqueos
HU-16 y HU-20 (sin ellas solo se pueden escribir los contratos y las pruebas de validación).
