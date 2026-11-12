# Especificación 024: Carrito de compras (parte API)

**Historia**: HU-24 (Sprint 7) · **Repos**: api + web (**este documento cubre solo la parte API**) · **Estado**: Spec lista · **Creada**: 2026-11-12

**Entrada**: «Como visitante, quiero un carrito que se conserve. Criterio: sin sesión el carrito vive en el navegador; al iniciar sesión se fusiona con el del servidor.»

**Bloqueado por**: HU-13 (autenticación, implementada), HU-16 (diseños y variantes, implementada), HU-20 (motor de precios, implementada). **Parte faltante en `blackhellheim-web`**: el carrito de visitante en el navegador (almacenamiento local) y la llamada a la fusión tras iniciar sesión. Alimenta a HU-26 (cotización), HU-28 (pedido) y HU-27 (checkout, web).

## Escenarios de uso y pruebas

### HU 1 — Ver y editar mi carrito con sesión (P1)
1. **Dado** una clienta con sesión, **cuando** consulta su carrito, **entonces** recibe sus líneas (variante, diseño, color, talla, cantidad), el precio unitario y el subtotal calculados por el servidor y el total.
2. **Cuando** fija la cantidad de una variante (1 a 10), **entonces** la línea se crea o se actualiza; con cantidad 0 se elimina (si la línea no existía, responde 200 con el carrito sin cambios); también puede vaciar el carrito.
3. Una variante inexistente, inactiva o de un diseño no publicado → `400`; cantidad fuera de rango → `400`; más de 20 líneas distintas → `400`; sin sesión → `401`.

### HU 2 — Fusionar el carrito del navegador al iniciar sesión (P1)
1. **Dado** un carrito de visitante (lista de variante y cantidad), **cuando** la clienta inicia sesión y lo envía a fusionar, **entonces** se suma a su carrito del servidor (misma variante: se suman las cantidades con tope de 10) y se devuelve el carrito resultante.
2. Las líneas inválidas (variante inexistente o inactiva) se omiten y se informan en la respuesta sin impedir el resto de la fusión.
3. Fusionar dos veces el mismo contenido no es idempotente por diseño (suma); la web debe vaciar su copia local tras una fusión correcta.

### HU 3 — El precio lo decide siempre el servidor (P1)
El cliente nunca envía precios; cualquier campo de precio en la solicitud se ignora. Si el tipo de cambio no está disponible, consultar el carrito responde `503` con el motivo (no se muestran precios inventados).

### Casos límite
Variante que se desactiva después de agregarla (la línea se conserva marcada como no disponible y no suma al total); dos pestañas editando a la vez (gana la última escritura por línea); carrito vacío (lista vacía, total 0).

## Requisitos funcionales
- **RF-001** `GET /carrito`, `PUT /carrito/lineas/{varianteId}` (cantidad 0 a 10), `DELETE /carrito` y `POST /carrito/fusionar`, todos con sesión.
- **RF-002** Un carrito por usuaria, máximo 20 líneas distintas y 10 unidades por línea.
- **RF-003** Precios y totales calculados por el servidor con el motor de precios (principio V); el cliente solo envía variante y cantidad.
- **RF-004** La fusión suma cantidades con tope y omite líneas inválidas informándolas.
- **RF-005** Una línea cuya variante deja de estar activa se marca `disponible: false` y no suma al total.
- **RF-006** Pruebas de éxito, error y validación; ningún servicio externo se llama.
- **RF-007** El catálogo (HU-19) expone el `varianteId` de cada variante para que la web pueda agregarla al carrito (cambio compatible al contrato de la 019).

## Criterios de éxito
- **CE-001** El 100 % de las líneas devueltas por el servidor trae precio calculado por el servidor y ninguna depende de datos enviados por el cliente.
- **CE-002** Consultar un carrito de 20 líneas responde en menos de 500 ms.
- **CE-003** Una fusión con líneas válidas e inválidas conserva el 100 % de las válidas y lista todas las omitidas.

## Entidades
Carrito (una por usuaria) y Línea de carrito (variante, cantidad).

## Supuestos y fuera de alcance
El carrito de visitante (navegador), la interfaz y el checkout son de `blackhellheim-web` (`[MANUAL]` coordinarlo allá). No hay expiración ni carritos abandonados. Disponibilidad de stock de Printful no se consulta (igual que HU-19). Los límites 20 y 10 son supuestos ajustables por configuración.
