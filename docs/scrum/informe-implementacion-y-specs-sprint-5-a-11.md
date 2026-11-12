# Informe: implementación sprint 5 y specs sprints 6 a 11

## Parte A · Implementadas (todas con pruebas en verde, build Release sin advertencias)
| HU | Rama | Commit | Pruebas |
|---|---|---|---|
| 17 Subida de arte | sprint/04 | 6c91b34 | 333 |
| (fusión sprint/04 → sprint/05, sin conflictos) | sprint/05 | ff9b8ba | — |
| 19 (contrato marcado x-pendiente) | sprint/05 | 8ce4567 | — |
| 20 Motor de precios | sprint/05 | 4afcaa3 | 425 |
| 19 API de catálogo | sprint/05 | d63f390 | 473 |

- Prueba de contrato: pasa, sin ningún `x-pendiente` activo.
- HU-17: T002 cerrada; cabecera PNG a mano y **SkiaSharp** (MIT) porque la spec exige medir la transparencia real de los píxeles. Otras dependencias: Azure.Storage.Blobs 12.30.1 (MIT), SkiaSharp.NativeAssets.Linux.NoDependencies (MIT), Testcontainers.Azurite 4.16.0 (MIT, solo pruebas). Imagen Docker no reconstruida (la nativa de Skia no se verificó en contenedor).
- HU-20: redondeo = entero terminado en 9 hacia arriba (spec, plan, tareas y modelo actualizados). **Hallazgo**: la constitución V sigue diciendo «9 o 90»; conviene enmendarla en una actualización aparte (no se tocó). Banxico: ruta verificada (responde 400 «token inválido» sin token); forma de la respuesta con token no verificada.
- HU-19: `ICalculadoraPrecios` ganó `PrepararAsync`; `disponible` = variante activa (sin stock de Printful). Sin migración de índices (CE-001 < 300 ms con 1000 diseños).
- Compose: servicio Azurite (`--skipApiVersionCheck`), variables `BHH_ALMACEN_CADENA` y `BHH_BANXICO_TOKEN` opcionales.

## Parte B · Specs (solo api) — INCOMPLETA
Rama `sprint/06` creada desde sprint/05. Hechas con los skills de Spec Kit:
| HU | Commit | Notas |
|---|---|---|
| 22 Webhooks de Printful | 05258f2 | specify, plan, tasks y analyze ejecutados; 2 hallazgos medios corregidos (marca de tiempo, cuerpo distinto) |
| 21 Mockups asíncronos | 0e3aa81 | specify ejecutado como skill; plan, tasks y análisis hechos a mano (sin los skills) |

**No hechas** (por límite de esta sesión, no por bloqueo): sprint/07 (HU-24 api, 26), sprint/08 (28, 29 api, 30), sprint/09 (31 api, 32, 34 api, 38 api), sprint/10 (36, 35, 37; al final), sprint/11 (40, 42 api, 43, 44, 45 api). Las ramas sprint/07 a sprint/11 no se crearon. Quedan fuera por ser solo web: 23, 27, 33, 39, 41.

## [MANUAL] y bloqueos
- Definir `BHH_JWT_CLAVE`; token de Printful, `Printful__SecretoWebhook` y registro del webhook (necesita URL pública: HU-25/HU-40, sin implementar); token de Banxico; parámetros de precio iniciales; curar términos bloqueados; Storage Account de producción (T012 de HU-17); resolución mínima de arte (T003a).
- HU-22 bloqueada para uso real por HU-25/40; HU-21 por HU-22; el esquema de firma de Printful y las rutas de mockups están sin verificar.

## Licencias nuevas
Azure.Storage.Blobs, SkiaSharp (+ NativeAssets.Linux.NoDependencies), Testcontainers.Azurite: todas MIT (nuspec). Nadie hizo push.
