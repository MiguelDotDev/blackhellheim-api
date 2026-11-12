# Investigación 026 (Fase 0)
Sin llamadas reales; lo de Printful es **suposición a verificar** (`[MANUAL]`).

| Tema | Decisión | Motivo / alternativas |
|---|---|---|
| Tarifas | `POST /v2/shipping-rates` con destinatario (país MX, estado, ciudad, código postal) y `order_items` (id de variante de catálogo, cantidad); respuesta con lista de tarifas (método, nombre, importe, moneda, días mínimo y máximo) | API v2 que ya usa HU-15. Alternativa: tarifa fija propia, descartada por no reflejar el costo real. |
| Puerto | `IProveedorImpresion.CotizarEnvioAsync(SolicitudCotizacion)` → lista de `TarifaProveedor` (moneda original) | Principio VII; la conversión a MXN va en Aplicación. |
| Conversión | `costoMxn = ceil(importeUsd × tipoCambio × (1 + colchón))`, sin margen ni comisión | Reutiliza `ServicioTipoCambio` y el colchón de HU-20; si el proveedor responde en otra moneda distinta de USD → 502 (igual que el catálogo). Margen sobre el envío queda como decisión `[MANUAL]`. |
| Aviso aduanal | Bandera `Envio__AvisoAduanal` (por omisión `true`) y texto fijo en español | No se sabe de dónde despacha cada producto; avisar de más es lo seguro. |
| Caché | `IMemoryCache` 5 min por huella (código postal + líneas ordenadas) | Evita repetir llamadas y respeta límites de Printful. |
| Sesión | Requerida | El checkout exige sesión (principio VIII). |
| Dependencias | Ninguna nueva | Principio IV. |
