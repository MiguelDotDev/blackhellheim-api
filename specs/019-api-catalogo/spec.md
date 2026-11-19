# Especificación 019: API de catálogo

**Historia**: HU-19 (Sprint 5) · **Repos**: api · **Estado**: Hecha · **Creada**: 2026-11-11

**Entrada**: «Como visitante, quiero que la API de catálogo liste, filtre y detalle las playeras. Criterio: listado paginado con filtros; el detalle trae imágenes, colores y tallas.»

**Bloqueado por**: HU-16 (diseños, variantes, arte) y HU-20 (precio calculado). Las imágenes extra por color llegan con HU-21; hasta entonces la imagen es el arte principal. Orden recomendado dentro del sprint: HU-20 antes que HU-19 (el backlog las lista 19, 20); si HU-20 no está lista, el campo `precioMxn` queda `null` y se documenta.

## Escenarios de uso y pruebas

### HU 1 — Listar playeras (P1)
1. **Dado** diseños publicados, **cuando** pido el listado sin sesión, **entonces** recibo una página con nombre, slug, imagen principal, precio desde (MXN) y colores disponibles.
2. **Dado** diseños en borrador o archivados, **entonces** nunca aparecen.
3. **Dada** la página fuera de rango, **entonces** devuelve lista vacía con el total correcto.

### HU 2 — Filtros y orden (P1)
1. Filtros por texto (nombre/descripción), color, talla y rango de precio; combinables (Y lógico).
2. Orden por novedad (por omisión), precio ascendente/descendente y nombre.
3. Parámetro inválido (tamaño 0 o >50, orden desconocido, mín>máx) → 400 ProblemDetails con el campo.
4. Filtro por talla/color considera solo variantes **activas y disponibles**.

### HU 3 — Detalle por slug (P1)
1. **Dado** un slug publicado, **cuando** lo consulto, **entonces** recibo descripción, imágenes, colores (con código), tallas por color, precio por variante y disponibilidad.
2. Slug inexistente, en borrador o archivado → 404.
3. Sin precios inventados: el precio viene del motor (HU-20); el cliente nunca lo envía.

### Casos límite
Diseño sin variantes activas (no listado); una sola talla; precios iguales al ordenar (desempate por id); búsqueda con caracteres especiales o SQL (parametrizada); texto muy largo (>100 → 400); acentos y mayúsculas (búsqueda insensible).

## Requisitos funcionales
- **RF-001** `GET /catalogo/playeras` público (sin sesión), paginado (página, tamaño ≤50, defecto 12) y con filtros/orden.
- **RF-002** `GET /catalogo/playeras/{slug}` público.
- **RF-003** Solo diseños `Publicado` con ≥1 variante activa.
- **RF-004** DTOs de lectura; ninguna entidad expuesta; sin costo base ni márgenes.
- **RF-005** Respuestas cacheables: `ETag`/`Cache-Control: public, max-age=60`.
- **RF-006** Contrato OpenAPI previo y pruebas de éxito, error y validación.
- **RF-007** Consultas con proyección directa (sin cargar grafos completos) e índices necesarios (migración si hace falta).

## Criterios de éxito
- **CE-001** Listado de 12 elementos con 1000 diseños responde en <300 ms en local.
- **CE-002** Ningún diseño no publicado aparece en 100 % de las pruebas.
- **CE-003** El detalle incluye imágenes, colores y tallas para el 100 % de los diseños publicados.

## Fuera de alcance
Carrito, reseñas, búsqueda difusa, mockups (HU-21), pantallas (HU-23).
