# Tareas 026: Cotizar el envío con Printful

Etiquetas: [BE] [MANUAL]. Pruebas primero (constitución III). Formato: `- [ ] Tnnn [P] [USn] [etiqueta] descripción con ruta`.

## Fase 1: Preparación
- [ ] T001 [BE] Prueba de contrato de `contratos/openapi.yaml` en `src/Pruebas/Api` (falla hasta tener el endpoint); al final quitar `x-pendiente`.
- [ ] T002 [BE] Anotar en `specs/026-cotizar-envio/investigacion.md` los supuestos de Printful (ruta `/v2/shipping-rates`, campos, moneda) como NO verificados; el razonamiento está en `research.md`.
- [ ] T003 [MANUAL] Verificar con token real de Printful una cotización y decidir la política de aviso aduanal y de margen sobre el envío.

## Fase 2: Base común
- [ ] T004 [BE] Ampliar `IProveedorImpresion` (`src/Aplicacion/Impresion`) con `CotizarEnvioAsync(SolicitudCotizacion)` y los tipos `SolicitudCotizacion` y `TarifaProveedor` (id, nombre, importe, moneda, días mínimo y máximo); actualizar el adaptador `src/Infraestructura/Printful` y todos los dobles de prueba existentes.
- [ ] T005 [BE] Opciones `Envio__AvisoAduanal` (por omisión true) y `Envio__MinutosCache` (por omisión 5) validadas al arrancar y `src/Api/Comun/RegistroEnvios.cs`.

## Fase 3: US1 cotizar (P1)
Criterio independiente: con Printful doble, un domicilio y líneas válidas devuelven métodos con costo en MXN entero.
- [ ] T006 [P] [BE] [US1] Pruebas de `CotizarEnvio`: éxito, conversión con `ceil(importeUsd × tipoCambio × (1 + colchón))`, aviso aduanal, caché de 5 min (segunda llamada sin tocar a Printful), lista vacía, código postal que no sea de 5 dígitos → 400, sin líneas → 400, más de 20 líneas → 400, cantidad fuera de 1 a 10 → 400, variante inexistente o inactiva → 400, sin sesión → 401.
- [ ] T007 [BE] [US1] Adaptador `CotizarEnvioAsync` en `src/Infraestructura/Printful` con JSON de ejemplo y sus pruebas (moneda distinta de USD → proveedor con fallo).
- [ ] T008 [BE] [US1] Caso de uso `CotizarEnvio` en `src/Aplicacion/Envios` (valida, traduce `varianteId` a id de Printful, convierte con `ServicioTipoCambio` y el colchón de HU-20, cachea) y `src/Api/Endpoints/EnviosEndpoints.cs` con `POST /envios/cotizar`.

## Fase 4: US2 fallas (P1)
- [ ] T009 [P] [BE] [US2] Pruebas: Printful caído → 502, saturado → 503 con `retryAfter`, sin respuesta → 504, sin tipo de cambio → 503, nunca un costo inventado.
- [ ] T010 [BE] [US2] Mapeo de errores del proveedor y del tipo de cambio en `CotizarEnvio` y el endpoint.

## Fase 5: Cierre
- [ ] T011 [BE] Quitar `x-pendiente` del contrato, `dotnet build`/`dotnet test` en verde, tareas marcadas, fila de `docs/scrum/backlog.md` = Hecha y commit `HU-26: cotizar envío`.

## Dependencias y paralelismo
T004 bloquea las demás de código; T006 y T009 son pruebas en paralelo. MVP = fases 1–3.

**Trazabilidad**: RF-001→T006,T008; RF-002→T004,T007; RF-003→T006,T008; RF-004→T006,T008; RF-005→T005–T006; RF-006→todas; CE-001→T006; CE-002→T006; CE-003→T006.
