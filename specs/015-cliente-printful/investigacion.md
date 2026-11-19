# Investigación 015 (T002, 2026-11-11)

Fuente: https://developers.printful.com/docs/v2-beta/ (API v2, base `https://api.printful.com`).

- Productos: `GET /v2/catalog-products` (`limit` 1–100, `offset`) y `/v2/catalog-products/{id}` (campos `id`, `name`, `type`, `techniques[].key`).
- Variantes: `GET /v2/catalog-products/{id}/catalog-variants` (`id`, `size`, `color`, `color_code`).
- Disponibilidad: `GET /v2/catalog-products/{id}/availability` (`catalog_variant_id`, `techniques[].technique`, `selling_regions[].name/availability`).
- Precios: `GET /v2/catalog-products/{id}/prices` (moneda y precio por técnica de cada variante).
- Límite: 120 solicitudes por 60 s; en 429 usar la cabecera `Retry-After`.

**Por verificar con token real [MANUAL]**: forma exacta de `prices` (`technique_key`), nombres de las regiones de venta (`mexico`, `north_america`, `worldwide` son supuestos, configurables en `Printful:RegionesPorPais`) y el filtro de categoría (hoy se filtra por tipo del producto). Sin dependencias nuevas (licencia: no aplica).
