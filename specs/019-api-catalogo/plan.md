# Plan 019: API de catálogo

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, V. **Dependencias nuevas**: ninguna.

## Resumen
Dos consultas Mediator de solo lectura (`ListarCatalogo`, `ObtenerPlayera`) sobre un puerto `IConsultasCatalogo` implementado en Infraestructura con proyecciones EF (`AsNoTracking`, `Select` a DTO). Endpoints públicos con `Cache-Control` corto y `ETag` derivado de los datos.

## Decisiones
1. Paginación por desplazamiento (página/tamaño); suficiente para el volumen del proyecto.
2. Búsqueda de texto con `LIKE` parametrizado y colación insensible; sin índice de texto completo.
3. El precio llega por un puerto `ICalculadoraPrecios` de HU-20; si la variante no tiene precio, `null` y no se lista como disponible para compra.
4. Orden estable con desempate por `Id`.
5. Fábrica de datos de prueba en `Pruebas` (sin red); pruebas de endpoint con Testcontainers.

## Estructura
`Aplicacion/Catalogo/{ListarCatalogo,ObtenerPlayera,IConsultasCatalogo,Dtos}`, `Infraestructura/Catalogo/ConsultasCatalogo`, `Api/Endpoints/CatalogoEndpoints.cs`.

## Bloqueos
HU-16 y HU-20 (sin ellas solo se pueden escribir los contratos y las pruebas de validación).
