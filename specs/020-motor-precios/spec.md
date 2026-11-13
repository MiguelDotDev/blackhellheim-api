# Especificación 020: Motor de precios

**Historia**: HU-20 (Sprint 5) · **Repos**: api · **Estado**: Hecha · **Creada**: 2026-11-11

**Entrada**: «Quiero un motor de precios con tipo de cambio, colchón y margen estático. Criterio: para entradas dadas, el precio coincide con la fórmula y termina en 9 (decisión posterior: entero terminado en 9); el tipo de cambio se guarda por día.»

**Bloqueado por**: HU-16 (costo base USD por variante). Token de Banxico: `[MANUAL]`. Alimenta a HU-19 (catálogo), HU-26 y HU-28 (precio congelado al pagar).

## Fórmula (constitución V)
`costo_mxn = (costo_base_usd + impresion_extra_usd) × tipo_cambio × (1 + colchon_cambiario)`
`precio = redondeo(costo_mxn ÷ (1 − margen − comision_pasarela))`, terminado en **9** (entero, siempre hacia arriba; ver HU 2).

## Escenarios de uso y pruebas

### HU 1 — Calcular el precio de una variante (P1)
1. **Dados** costo base 10.00 USD, extra 0, tipo de cambio 18.00, colchón 3 %, margen 40 %, comisión 3.6 %, **cuando** se calcula, **entonces** el resultado coincide con la fórmula aplicada con decimales exactos y termina en 9.
2. **Dados** parámetros inválidos (margen+comisión ≥ 1, costo negativo, tipo de cambio ≤ 0), **entonces** error de validación sin calcular.
3. El cálculo es determinista: mismas entradas, mismo precio.
4. El cliente nunca envía precios; solo el backend los calcula (regla 5 de AGENTS).

### HU 2 — Redondeo comercial (P1)
1. **Decisión de producto (2026-11-11)**: el precio final es un **entero terminado en 9**, redondeado **siempre hacia arriba**: el menor entero que termina en 9 y es ≥ precio bruto. Ejemplos: 412.30 → 419; 449.01 → 459; 489.99 → 499; 490.00 → 499; 499.00 → 499; 499.01 → 509; 0 → 9. No hay parámetro de formato (se descartó la terminación en .90).
2. Nunca por debajo del precio bruto (no se vende con pérdida de margen).
3. Fronteras exactas probadas (límites de cada decena y centena).

### HU 3 — Tipo de cambio por día (P1)
1. **Cuando** se pide el tipo de cambio de hoy, **entonces** se obtiene del proveedor (puerto `IProveedorTipoCambio`, Banxico FIX) una vez al día y se guarda con su fecha; las consultas posteriores del día usan lo guardado.
2. **Dado** que el proveedor falla o no hay dato (fin de semana/festivo), **entonces** se usa el último guardado, marcado como reserva (`EsReserva`), y se registra una advertencia; si no existe ninguno, error 503 y no se calcula.
3. El cálculo recibe la tasa como dato explícito y no consulta el reloj, para que HU-28 pueda congelar el precio de un pedido pagado (el congelado queda fuera de esta HU).
4. Un tipo de cambio fuera de rango razonable (±20 % del último guardado) se rechaza y conserva el anterior.

### HU 4 — Parámetros configurables y recálculo (P2)
1. Colchón, margen, comisión y extra de impresión son parámetros versionados en base de datos (valores iniciales por migración), editables solo por Administrador mediante `PUT /admin/precios/parametros`.
2. Cambiar parámetros no altera pedidos existentes.
3. `GET /admin/precios/simular?varianteId=` muestra el desglose (costo, tipo de cambio, colchón, margen, comisión, bruto, final).

### Casos límite
Costo 0; decimales largos (decimal de 28 dígitos, sin `double`); redondeo en cada 100; margen 0; tipo de cambio con 4 decimales; dos solicitudes concurrentes el mismo día (una sola fila); cambio de día en zona horaria de México (usa fecha de CDMX).

## Requisitos funcionales
- **RF-001** Calculadora pura en Dominio con `decimal`.
- **RF-002** Redondeo a entero terminado en 9 según HU 2.
- **RF-003** Puerto `IProveedorTipoCambio` + adaptador Banxico (SIE API, token) y doble para pruebas.
- **RF-004** Tabla de tipos de cambio por día con fecha única.
- **RF-005** Reglas de reserva (último valor, rango ±20 %).
- **RF-006** Parámetros de precio versionados y endpoints de administración con contrato previo.
- **RF-007** Puerto `ICalculadoraPrecios` para HU-19/26/28.
- **RF-008** Pruebas de éxito, error y validación de cada regla.

## Criterios de éxito
- **CE-001** Tabla de ≥20 casos de entrada/salida (incluidos límites) pasa al 100 %.
- **CE-002** El 100 % de los precios terminan en 9.
- **CE-003** Una sola fila de tipo de cambio por día aun con concurrencia.

## Supuestos y fuera de alcance
Moneda de venta MXN; la comisión fija de la pasarela se **no** incluye (la constitución define solo porcentaje) — decisión: solo `comision_pasarela` porcentual (3.6 %). Impuestos/IVA y CFDI fuera (spec 901). Envío no se incluye (HU-26). Parámetros iniciales propuestos: colchón 3 %, margen 40 %, comisión 3.6 %, extra 0; `[MANUAL]` confirmarlos.
