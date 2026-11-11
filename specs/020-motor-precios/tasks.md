# Tareas 020: Motor de precios

Etiquetas: [BE] [DB] [MANUAL]. Pruebas primero.

## Fase 1
- [ ] T001 [BE] Prueba de contrato contra `contratos/openapi.yaml` (falla).
- [ ] T002 [BE] Verificar ruta, serie y términos de la API SIE de Banxico; anotar en `investigacion.md`. Sin dependencias nuevas (licencia: N/A).
- [ ] T003 [MANUAL] Confirmar la lectura del redondeo «9 o 90» (spec HU 2); obtener token de Banxico; `Banxico__Token` en `.env` local y en secretos de Azure (HU-25); confirmar parámetros iniciales (colchón, margen, comisión).
## Fase 2: Dominio
- [ ] T004 [P] [BE] Pruebas del redondeo (tabla con límites, ambos formatos: 412.30→419, 449.01→459, 489.99→499, 490.00→499, 499.00→499, 499.01→509, 0→9; y 412.30→419.90, 419.91→429.90) y de la fórmula (≥20 casos, validación de margen+comisión ≥1, costos negativos, tipo de cambio ≤0).
- [ ] T005 [BE] `RedondeoComercial` y `CalculadoraPrecios` con `decimal`.
## Fase 3: Tipo de cambio
- [ ] T006 [P] [BE] Pruebas de `ObtenerTipoCambioDelDia`: éxito y guarda, reutiliza el del día, falla→reserva obsoleta, rango ±20 %, sin dato→503, concurrencia.
- [ ] T007 [DB] Entidades, configuración EF y migración `AgregarPrecios` (índice único por fecha, semilla de parámetros).
- [ ] T008 [BE] Puerto, adaptador Banxico (con pruebas de mapeo sobre JSON de ejemplo y sin red) y servicio.
## Fase 4: Aplicación y API
- [ ] T009 [P] [BE] Pruebas de `Simular` y `ActualizarParametros` (éxito, validación, 403, 404, 503).
- [ ] T010 [BE] Manejadores, `ICalculadoraPrecios` y `PreciosAdminEndpoints`.
## Fase 5: Cierre
- [ ] T011 [BE] Verificar CE-002 con una propiedad aleatoria (1000 entradas) y que `build`/`test` están en verde.
- [ ] T012 [BE] Backlog = Hecha; commit `HU-20: motor de precios`.

**Trazabilidad**: RF-001/002→T004-5; RF-003→T006,T008; RF-004→T007; RF-005→T006; RF-006→T009-10; RF-007→T010; RF-008→todas; CE-001→T004; CE-002→T011; CE-003→T006-7.
