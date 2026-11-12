# Especificación 022: Webhooks de Printful con firma verificada

**Historia**: HU-22 (Sprint 6) · **Repos**: api · **Estado**: Spec lista · **Creada**: 2026-11-11

**Entrada**: «Quiero un endpoint de webhooks de Printful con firma verificada. Criterio: una firma inválida se rechaza; un evento repetido no se procesa dos veces.»

**Bloqueado por**: HU-15 (cliente Printful, implementada). Registro real del webhook en Printful requiere una URL pública HTTPS (HU-25/HU-40, no implementadas): `[MANUAL]`. Alimenta a HU-21 (`mockup_task_finished`) y HU-32 (envíos).

## Escenarios de uso y pruebas

### HU 1 — Aceptar un evento legítimo (P1)
1. **Dado** un evento con firma válida, **cuando** llega, **entonces** responde `200` en menos de 2 s, queda registrado una sola vez y se entrega a los consumidores interesados en su tipo.
2. **Dado** un tipo de evento sin consumidor, **entonces** se registra y responde `200` (Printful no debe reintentar).

### HU 2 — Rechazar lo no verificable (P1)
1. Firma ausente, mal formada o incorrecta → `401` ProblemDetails, sin registrar ni procesar nada.
2. Cuerpo que no es JSON válido o sin identificador/tipo de evento → `400`.
3. La firma se calcula sobre el cuerpo crudo y se compara en tiempo constante.

### HU 3 — Idempotencia (P1)
1. **Dado** el mismo evento (mismo identificador) recibido dos o más veces, incluso en paralelo, **entonces** se procesa una sola vez y las repeticiones responden `200`.
2. Si un consumidor falla, el evento queda `Fallido` y se puede reintentar sin duplicar efectos de los consumidores que sí terminaron.

### Casos límite
Cuerpo muy grande (>1 MB → 413); reloj desfasado si Printful envía marca de tiempo (se rechaza fuera de 5 min); secreto no configurado (el endpoint responde 503 y no acepta nada); mismo evento con cuerpo distinto (se conserva el primero y se advierte).

## Requisitos funcionales
- **RF-001** `POST /webhooks/printful` público (sin JWT), solo firma.
- **RF-002** Verificación HMAC-SHA256 del cuerpo crudo con el secreto `Printful__SecretoWebhook`.
- **RF-003** Tabla de eventos recibidos con clave única (proveedor, id de evento).
- **RF-004** Despacho a consumidores registrados por tipo de evento (puerto de Aplicación).
- **RF-005** Estados del evento: Recibido, Procesado, Fallido.
- **RF-006** Pruebas de éxito, error y validación; Printful nunca se llama.

## Criterios de éxito
- **CE-001** El 100 % de las firmas inválidas de la tabla de pruebas se rechaza.
- **CE-002** 20 entregas paralelas del mismo evento producen un único procesamiento.

## Supuestos y fuera de alcance
El esquema exacto de firma de Printful (cabecera, algoritmo, formato) no está verificado: se asume HMAC-SHA256 hexadecimal con cabecera configurable y se confirma en T002 (`[MANUAL]` con cuenta real). Los efectos de negocio de cada evento (mockups, envíos) pertenecen a HU-21/HU-32. Registrar el webhook en el panel de Printful: `[MANUAL]`.
