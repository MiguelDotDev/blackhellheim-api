# Especificación 026: Cotizar el envío con Printful

**Historia**: HU-26 (Sprint 7) · **Repos**: api · **Estado**: Spec lista · **Creada**: 2026-11-12

**Entrada**: «Quiero cotizar el envío con Printful. Criterio: devuelve métodos con días estimados, costo en MXN y aviso de posibles cargos aduanales.»

**Bloqueado por**: HU-15 (cliente Printful, implementada), HU-16 (variantes, implementada), HU-20 (tipo de cambio y parámetros, implementada). La verificación contra Printful real es `[MANUAL]` (cuenta y token). Alimenta a HU-27 (checkout, web) y HU-28 (pedido: guarda el método y el costo elegido).

## Escenarios de uso y pruebas

### HU 1 — Cotizar el envío de un carrito (P1)
1. **Dado** una clienta con sesión, un domicilio mexicano (código postal, estado, ciudad) y líneas de variante y cantidad, **cuando** pide la cotización, **entonces** recibe la lista de métodos de envío con nombre, días estimados (mínimo y máximo), costo en MXN y el aviso de posibles cargos aduanales.
2. Los costos se redondean hacia arriba al peso entero y se calculan siempre en el servidor; el cliente no envía montos.
3. Domicilio incompleto o con código postal inválido (no 5 dígitos) → `400`; variante inexistente o inactiva → `400`; sin líneas → `400`; sin sesión → `401`.

### HU 2 — Fallas del proveedor y del tipo de cambio (P1)
Si Printful no responde, está saturado o no ofrece métodos para ese domicilio, la respuesta es `502`/`503`/`504` (o lista vacía con un mensaje claro cuando simplemente no hay métodos); sin tipo de cambio disponible → `503`. Nunca se inventa un costo.

### Casos límite
Mismas líneas y domicilio repetidos en pocos minutos (se reutiliza la cotización en memoria, sin volver a llamar a Printful); línea con cantidad fuera de 1 a 10; más de 20 líneas; envío internacional (fuera de alcance: el contrato no tiene campo de país y siempre se cotiza para México).

## Requisitos funcionales
- **RF-001** `POST /envios/cotizar` con sesión; recibe domicilio mexicano y líneas (varianteId, cantidad).
- **RF-002** El servidor consulta a Printful por el puerto `IProveedorImpresion` (ampliado con una operación de cotización).
- **RF-003** Cada método incluye `id`, `nombre`, `diasMinimos`, `diasMaximos`, `costoMxn` entero y `avisoAduanal` (bool) con su texto.
- **RF-004** Conversión a MXN con el tipo de cambio del día y el colchón cambiario de los parámetros de precio (`ParametrosPrecio`, HU-20); sin margen ni comisión sobre el envío.
- **RF-005** Caché corta (5 min) por huella de domicilio y líneas.
- **RF-006** Pruebas de éxito, error y validación con Printful doble; nunca red real.

## Criterios de éxito
- **CE-001** El 100 % de los costos devueltos está en MXN enteros y proviene del cálculo del servidor.
- **CE-002** Una cotización repetida dentro de 5 min no genera una segunda llamada a Printful.
- **CE-003** El 100 % de los casos inválidos de la tabla de pruebas se rechaza con `400` antes de llamar a Printful.

## Entidades
Domicilio de envío (valor), línea a cotizar (valor) y método de envío cotizado (valor); no se persiste nada.

## Supuestos y fuera de alcance
Solo México. El formato real de la API de tarifas de Printful (ruta, campos, moneda) y qué productos disparan cargos aduanales no están verificados: se asume que toda cotización lleva el aviso (configurable) hasta confirmarlo con la cuenta real (`[MANUAL]`). Decidir si el envío debe llevar margen queda como `[MANUAL]` de la titular; esta versión lo traslada al costo más el colchón cambiario. Elegir el método y guardarlo es de HU-28; la pantalla es de HU-27 (web).
