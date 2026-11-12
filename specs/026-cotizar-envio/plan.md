# Plan 026: Cotizar el envío con Printful

**Spec**: [spec.md](spec.md) · **Investigación**: [research.md](research.md) · **Modelo**: [modelo-datos.md](modelo-datos.md) · **Contrato**: [contratos/openapi.yaml](contratos/openapi.yaml) · **Validación**: [quickstart.md](quickstart.md)

## Contexto técnico
.NET 10, Mediator, `IMemoryCache`, xUnit; patrón de HU-15 (adaptador Printful) y HU-20 (tipo de cambio). **Dependencias nuevas: ninguna.**

## Revisión de la constitución
| Principio | Resultado |
|---|---|
| I | Cumple: `Aplicacion/Envios` (caso de uso, conversión), adaptador en Infraestructura, DTO en la API. |
| II | Cumple: contrato completo antes del código. |
| III | Cumple (ver Pruebas). |
| IV | Cumple. |
| V | Cumple: el costo lo calcula el servidor; el cliente no envía montos. |
| VI | No aplica. |
| VII | Cumple: Printful solo por `IProveedorImpresion`, ampliado con `CotizarEnvioAsync`. |
| VIII | Cumple: requiere sesión; sin secretos nuevos. |
| IX–XI | No aplica / cumple. |

## Decisiones
1. Ampliar `IProveedorImpresion` con `CotizarEnvioAsync` y los tipos `SolicitudCotizacion`/`TarifaProveedor`; actualizar el adaptador y todos los dobles existentes.
2. Caso de uso `CotizarEnvio`: valida (CP de 5 dígitos, ≤20 líneas, cantidad 1–10, variantes existentes y activas), traduce `varianteId`→id de variante de Printful, llama al puerto, convierte a MXN con `ServicioTipoCambio` y el colchón de `ParametrosPrecio` (HU-20) y arma `MetodoEnvioDto`.
3. Caché de 5 min por huella; opciones `Envio__AvisoAduanal` y `Envio__MinutosCache` validadas al arrancar.
4. Errores de proveedor con el mapeo existente (502/503/504); sin tipo de cambio → 503.

## Estructura
`Aplicacion/Envios`, `Infraestructura/Printful` (adaptador), `Api/Endpoints/EnviosEndpoints.cs`, `Api/Comun/RegistroEnvios.cs`.

## Pruebas
Caso de uso con proveedor y tipo de cambio dobles (éxito, redondeo hacia arriba, validaciones, sin tipo de cambio, proveedor caído, lista vacía, caché), adaptador con JSON de ejemplo, endpoint (200/400/401/502/503), contrato.

## Bloqueos y [MANUAL]
Bloqueado por HU-15, 16 y 20 (implementadas). `[MANUAL]`: token de Printful, formato real de tarifas, política de aviso aduanal y de margen sobre el envío.
