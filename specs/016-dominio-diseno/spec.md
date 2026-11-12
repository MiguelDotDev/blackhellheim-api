# Especificación 016: Dominio de diseño, producto base y variante

**Historia**: HU-16 (Sprint 4) · **Repos**: api · **Estado**: Plan cerrado · **Creada**: 2026-11-11

**Entrada**: «Quiero el dominio de diseño, producto base y variante. Criterio: migración aplicada; un diseño apunta a variantes de Printful y a un archivo de arte.»

**Bloqueado por**: HU-15 (los ids y datos de variantes salen del catálogo de Printful; para persistir no hace falta llamarlo: el alta recibe los ids) y HU-13 (rol Administrador en endpoints). El archivo de arte lo produce HU-17; aquí solo se referencia.

**Decisión de alcance (documentada)**: la HU-18 (web) necesita crear y editar diseños, pero ninguna HU del API lo cubre. Esta HU incluye por eso los casos de uso y endpoints mínimos de administración `POST/PUT/GET /admin/disenos`. Si las 8 h resultan cortas, se divide con el Responsable de producto.

## Escenarios de uso y pruebas

### HU 1 — Modelar diseño, producto base y variante (P1)
1. **Dado** un diseño con nombre único, **cuando** lo guardo en borrador, **entonces** queda con slug generado y sin variantes.
2. **Dado** un producto base de Printful (id, nombre, tipo) , **cuando** lo registro por primera vez, **entonces** se crea; **y** si ya existe, se reutiliza sin duplicar (por id de Printful).
3. **Dada** una variante (id Printful, color, código de color, talla, costo base USD), **entonces** pertenece a un único producto base y su id de Printful es único.
4. **Dado** un diseño, **cuando** asocio variantes de un mismo producto base, **entonces** el diseño apunta a ellas; una variante inexistente da 400.
5. **Dado** un diseño, **cuando** asocio un archivo de arte (ya subido en HU-17), **entonces** el diseño referencia exactamente un arte principal; un arte inexistente da 400.

### HU 2 — Estados y reglas (P1)
1. Un diseño pasa de `Borrador` a `Publicado` solo si tiene arte y al menos una variante activa; si no, 409 ProblemDetails con el motivo.
2. `Publicado` → `Archivado` y viceversa permitidos; `Archivado` no aparece en catálogo (HU-19).
3. Nombres y slugs únicos sin distinguir mayúsculas; nombre 3–80 caracteres.
4. Prohibido usar nombres o logos de terceros (constitución X): se valida contra una lista de términos bloqueados configurable `[MANUAL]` de curar, y avisa sin impedir si hay coincidencia parcial (mensaje, no bloqueo).

### HU 3 — Administración (P2)
1. `POST /admin/disenos` crea; `PUT /admin/disenos/{id}` edita nombre, descripción, variantes y estado; `GET /admin/disenos` pagina; `GET /admin/disenos/{id}` detalla. Solo rol Administrador. Respuestas DTO.
2. Ediciones concurrentes: control optimista (`ETag`/versión) → 412 si la versión no coincide.

### Casos límite
Variantes de dos productos base distintos en un diseño (rechazo); quitar la última variante de un diseño publicado (rechazo); descripción >2000 caracteres; slug con acentos (se normaliza); diseño sin descripción (permitido).

## Requisitos funcionales
- **RF-001** Entidades `Diseno`, `ProductoBase`, `Variante`, `DisenoVariante` y `ArchivoArte` (referencia) en Dominio con invariantes.
- **RF-002** Migración EF de Core con índices únicos (id de Printful, slug, nombre).
- **RF-003** Reglas de estado y publicación.
- **RF-004** Control optimista de concurrencia.
- **RF-005** Casos de uso y endpoints de administración con contrato OpenAPI previo.
- **RF-006** El precio **no** se guarda aquí: lo calcula HU-20 desde `CostoBaseUsd`.
- **RF-007** Pruebas de éxito, error y validación por regla.

## Criterios de éxito
- **CE-001** `docker compose up` aplica la migración sin perder datos previos.
- **CE-002** Todas las invariantes tienen prueba de dominio.
- **CE-003** Un diseño completo (arte + 3 variantes) se guarda y recupera idéntico.

## Fuera de alcance
Subida de archivos (HU-17), catálogo público (HU-19), precios (HU-20), mockups (HU-21), imágenes adicionales por color (HU-21), pantallas (HU-18).
