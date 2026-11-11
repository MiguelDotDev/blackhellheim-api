# Especificación 015: Cliente de Printful (catálogo, variantes y disponibilidad)

**Historia**: HU-15 (Sprint 4) · **Repos**: api · **Estado**: Plan cerrado · **Creada**: 2026-11-11

**Entrada**: «Quiero un cliente de Printful para catálogo, variantes y disponibilidad. Criterio: lista variantes de una playera filtradas por técnica y país; un 429 se reintenta con retry-after.»

**Bloqueado por**: HU-13 para el endpoint de administración (rol Administrador). El puerto y el adaptador no dependen de nada. Cuenta y token de Printful: `[MANUAL]`.

## Escenarios de uso y pruebas

### HU 1 — Consultar catálogo de Printful (P1)
Como administradora quiero ver los productos base (playeras) y sus variantes para armar diseños (HU-16/18).
1. **Dado** un id de producto de catálogo válido, **cuando** lo consulto, **entonces** obtengo nombre, tipo, técnicas disponibles y colores.
2. **Dado** un id inexistente, **cuando** lo consulto, **entonces** recibo 404 ProblemDetails.
3. **Cuando** listo productos con filtro de categoría «playeras», **entonces** recibo una página con id y nombre.

### HU 2 — Variantes filtradas por técnica y país (P1)
1. **Dado** un producto, una técnica (p. ej. DTG) y un país (MX), **cuando** pido sus variantes, **entonces** recibo solo las variantes con esa técnica y disponibles para envío a ese país, con color, código de color, talla y costo base en USD.
2. **Dada** una técnica o país desconocidos, **entonces** recibo 400 con los valores admitidos.
3. **Dado** que ninguna variante cumple, **entonces** recibo lista vacía (200), no error.

### HU 3 — Resiliencia ante límites y fallos (P1)
1. **Dado** que Printful responde 429 con `Retry-After`, **cuando** se hace la llamada, **entonces** se espera ese tiempo (con tope) y se reintenta hasta 3 veces; si persiste, se devuelve 503 ProblemDetails con el tiempo sugerido.
2. **Dado** un 5xx transitorio, **entonces** se reintenta con espera creciente hasta 3 veces.
3. **Dado** un 401/403 de Printful (token inválido), **entonces** no se reintenta y se devuelve 502 sin exponer el token.
4. **Dado** un tiempo de espera agotado (10 s por intento), **entonces** 504.

### HU 4 — Seguridad y pruebas sin red (P2)
1. El token vive en configuración de entorno y nunca aparece en registros ni respuestas.
2. En CI nunca se llama a Printful: las pruebas usan un doble HTTP (constitución VII).

### Casos límite
`Retry-After` en segundos o como fecha; valor absurdo (>60 s: se recorta); respuesta con JSON inesperado (502 con mensaje genérico); paginación con límites; moneda distinta de USD (se rechaza); ráfagas (resultado cacheado 10 min).

## Requisitos funcionales
- **RF-001** Puerto `IProveedorImpresion` en Aplicación con `ObtenerProductoAsync`, `ListarVariantesAsync(productoId, tecnica, pais)`, `ObtenerDisponibilidadAsync` y modelos propios (sin tipos de Printful).
- **RF-002** Adaptador HTTP en Infraestructura contra la API v2 de catálogo de Printful.
- **RF-003** Filtro por técnica y país aplicado con datos de disponibilidad de Printful.
- **RF-004** Reintento de 429 respetando `Retry-After` (tope 60 s, 3 intentos) y de 5xx con espera creciente; sin reintento de 4xx restantes.
- **RF-005** Errores traducidos a `ExcepcionCasoDeUso` → ProblemDetails (404, 400, 502, 503, 504).
- **RF-006** Endpoints de administración `GET /admin/printful/productos`, `/{id}`, `/{id}/variantes?tecnica=&pais=` con rol Administrador; DTOs, no modelos de Printful.
- **RF-007** Caché en memoria de 10 min para catálogo y disponibilidad.
- **RF-008** Contrato OpenAPI antes del código.

## Criterios de éxito
- **CE-001** Un 429 con `Retry-After: 2` se reintenta y tiene éxito en la segunda llamada (prueba).
- **CE-002** Cero llamadas reales a Printful en CI.
- **CE-003** Cada regla con pruebas de éxito, error y validación.

## Supuestos y fuera de alcance
Fuera: pedidos (HU-30), mockups (HU-21), webhooks (HU-22), cotización de envío (HU-26), sincronizar el catálogo a la base (lo hace el alta de diseño, HU-16). Se asume API v2 de Printful y token privado de tienda; si Printful cambia rutas se ajusta solo el adaptador.
