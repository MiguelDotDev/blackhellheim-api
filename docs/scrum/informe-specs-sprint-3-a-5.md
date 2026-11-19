# Informe: specs, planes y tareas de los sprints 3 a 5 (API)

Fecha: 2026-11-11. Llegué hasta el final (sprint 5). No hubo `git push`, no se tocaron `sprint/01` ni `sprint/02`, no se creó código fuente ni `.env`.

## Lectura tomada
Se procesan las HU con «api» en Repos. Para «api + web» (HU-25) solo la parte API. Ignoradas por ser solo web: 11, 14 (y 18 del sprint 5). La orden «19, 18, 20» se interpretó como 19 y 20, porque la 18 es web. Para cada HU se hizo specify + plan + tasks + analyze de forma manual-secuencial siguiendo las plantillas del repo (sin `/speckit.clarify`; las ambigüedades se resolvieron y documentaron). No se generaron `checklists/` ni `investigacion.md`/`inicio-rapido.md` (salvo lo indicado en tareas).

## Ramas y commits
`sprint/03` (desde sprint/02): HU-13, HU-25. `sprint/04`: HU-15, HU-16, HU-17. `sprint/05`: HU-19, HU-20 (+ aclaración de redondeo) e informe.

## HU procesadas

| HU | Carpeta | Tareas | [MANUAL] | Bloqueada por |
|---|---|---|---|---|
| 13 | specs/013-autenticacion-jwt | 18 | T016 (clave JWT y admin inicial) | — |
| 25 (API) | specs/025-despliegue-azure | 19 | T003, T007, T009, T014, T016 | HU-5, HU-6; falta parte web |
| 15 | specs/015-cliente-printful | 13 | T003 (token Printful) | HU-13 (endpoints) |
| 16 | specs/016-dominio-diseno | 11 | T010 (términos bloqueados) | HU-13, HU-15 |
| 17 | specs/017-subida-arte | 13 | T012 (Storage en Azure) | HU-13, HU-16 |
| 19 | specs/019-api-catalogo | 8 | — | HU-16, HU-20 |
| 20 | specs/020-motor-precios | 12 | T003 (token Banxico, parámetros, redondeo) | HU-16 |

HU-25 cubre los huecos pedidos: aprovisionamiento con `az` (HU 1), OIDC (HU 2), migraciones antes del tráfico con job `efbundle` (HU 4), secretos como secretos de Container Apps (HU 5) y rollback por revisiones (HU 6). Sin Terraform.

## Hallazgos del análisis (corregidos salvo indicación)
1. **Medio, corregido** HU-13: RF-011 (purga de refresh) sin tarea → T011a.
2. **Alto, corregido** HU-20: el criterio «termina en 9 o 90» era ambiguo y mis ejemplos iniciales eran incoherentes. Se adoptó `Entero9` por omisión y `Centavos90` configurable. **Necesita tu confirmación.**
3. **Medio, abierto** HU-16: ninguna HU API cubre el CRUD de diseños que necesita la HU-18 (web). Se incluyó en HU-16 (puede exceder 8 h).
4. **Medio, abierto** HU-17: decodificador PNG sin decidir (ImageSharp descartado por licencia dividida; se resuelve en T002).
5. **Bajo** Orden del backlog: HU-19 depende de HU-20; implementar 20 antes que 19.
6. **Bajo** Las rutas actuales no usan prefijo `/api`; el proxy debe quitarlo (documentado en HU-25).
7. **Bajo** Los commits de HU-16 y HU-17 reparten sus filas del backlog en el commit de la HU-17 (ambas filas quedaron en un mismo commit).

## Cambios a specs previas
Ninguno. Solo se editó `docs/scrum/backlog.md` (columnas Spec/Estado de las 7 HU: «Tareas listas»).

## Decisiones técnicas y licencias
| Decisión | Licencia |
|---|---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore, Authentication.JwtBearer 10.0.x (HU-13) | MIT (a reverificar al agregar) |
| Azure Container Apps + GHCR + Azure SQL (HU-25) | Servicios; sin paquetes |
| `azure/login`, `docker/*-action`, `actions/checkout` | MIT / Apache-2.0 (verificar SHA en T002) |
| Reintentos propios para Printful (HU-15) | Sin dependencia |
| Azure.Storage.Blobs (HU-17) | MIT |
| SkiaSharp (+NativeAssets.Linux.NoDependencies) o lector PNG propio (HU-17) | MIT; ImageSharp rechazado |
| Banxico SIE vía HttpClient (HU-20) | Sin dependencia |

## Qué no verifiqué
Rutas exactas de las APIs de Printful y Banxico, y versiones/SHA de acciones (quedaron como tareas T002). Nada se compiló ni se ejecutó.
