# Informe: análisis de specs de los sprints 3 a 5

Fecha: 2026-11-11. Análisis de consistencia entre `spec.md`, `plan.md` y `tasks.md` de cada HU, con corrección solo de hallazgos MEDIOS o ALTOS. No hubo hallazgos críticos (ninguna violación de la constitución). Sin `git push`; sin código; no se tocaron `sprint/01` ni `sprint/02`. Quedan pendientes, sin cambiarlas: el redondeo «9 o 90» (HU-20) y el decodificador PNG (HU-17, T002).

## Hallazgos por HU

| HU | Sev. | Hallazgo | Acción |
|---|---|---|---|
| 13 | M | La spec decía «429/423» para el bloqueo; el contrato dice 423. | Spec ajustada a 423. |
| 13 | M | Las cabeceras reenviadas aparecían en HU-13 y HU-25 (duplicado) y la cookie `Secure` no contemplaba el desarrollo por HTTP. | HU-25 es dueña única; `Secure` siempre activo salvo `Development` (con prueba). |
| 25 | A | Doble ejecución de la CI (`workflow_call` + disparo propio en `main`). | Disparo por `workflow_run` del flujo existente; no se modifica la CI. |
| 25 | M | Credencial federada por rama inconsistente con `environment: produccion` (el subject es por entorno). | Una sola credencial por entorno; entorno restringido a `main`. |
| 25 | M | El primer `crear-recursos.sh` referenciaba imágenes aún inexistentes o privadas en GHCR. | Imagen de arranque pública (`mcr.microsoft.com/k8se/quickstart`) en app y job. |
| 25 | M | Referencias a un `investigacion` inexistente (CE-005, T002). | Apuntan a `plan.md` / `inicio-rapido.md`. |
| 15 | M | «Categoría playeras» en la spec sin parámetro en el contrato. | Parámetro `categoria` en OpenAPI y spec. |
| 16 | M | «409/400» ambiguo al publicar sin arte o variantes. | Fijado en 409. |
| 16/17 | M | `HashSha256` se agregaba «si hace falta» en HU-17 (dueño de la tabla poco claro). | HU-16 lo incluye (único); HU-17 solo verifica. |
| 17 | M | Resolución mínima inventada (4500×5400) como si fuera del backlog. | Marcada «a confirmar», tarea `[MANUAL]` T003a. |
| 17 | M | Dimensión máxima de 20 000 px por lado permitía ~1,6 GB al decodificar. | Límite de 100 megapíxeles configurable. |
| 17 | M | El plan «elegía» decodificador y a la vez lo dejaba para T002. | Redactado como decisión pendiente en T002. |
| 19 | A | Filtrar y ordenar por precio es imposible en SQL (el precio no se persiste). | Cálculo en memoria con `ICalculadoraPrecios` y paginación posterior; tareas y pruebas ajustadas. |
| 20 | M | La spec obligaba a «no cambiar el precio de un pedido pagado», que es de HU-28. | Reescrito: la tasa entra como dato explícito. |
| 20 | M | Términos `ObsoletoDesde` y `EsReserva` mezclados; CE-002 atado a «9 o 90». | `EsReserva`; CE-002 sigue el formato vigente (decisión pendiente intacta). |
| 20 | M | Sin prueba del cambio de día en zona de Ciudad de México. | Agregada a T006. |

Hallazgos bajos no corregidos: rutas sin prefijo `/api` (el proxy lo quita), orden del backlog 19 antes que 20.

## Separación de HU-16 y HU-17
En `sprint/04` se separaron con `git reset --soft` y dos commits (`d03d0e4`, `590399b`). Los árboles antes y después son idénticos: sprint/04 `8f2f531ca3ae` y sprint/05 `9cc225170c1a` (comparados con `git rev-parse <rama>^{tree}`). `sprint/05` se re-aplicó con `git rebase --onto` no interactivo.

## Hashes finales (sin contar este informe)
- **sprint/03**: `22f1726` HU-13 spec · `3f2f4c5` HU-25 spec · `3af1c14` HU-13 ajustes · `7a247a2` HU-25 ajustes.
- **sprint/04**: `c0697fc` HU-15 · `d03d0e4` HU-16 · `590399b` HU-17 (specs); `f274549` HU-15 · `f99463c` HU-16 · `06818d5` HU-17 (ajustes).
- **sprint/05**: `2cb9719` HU-19 · `0b93892` HU-20 · `c788c8e` aclaración de redondeo · `1b8e5bf` informe previo · `b0874c0` HU-19 ajustes · `5547e26` HU-20 ajustes.

## Nota sobre las ramas
Las ramas se crearon encadenadas, así que `sprint/04` y `sprint/05` conservan las versiones previas a los ajustes de las ramas anteriores. Al fusionar en `developer` en orden (03, 04, 05) no hay conflicto: cada archivo se modificó en una sola rama después del punto común.
