# Plan de implementación: Estructura base del API

**Rama**: `002-estructura-base-api` | **Fecha**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de funcionalidad en `specs/002-estructura-base-api/spec.md`

## Resumen

La historia 002 crea la solución .NET 10 con Clean Architecture: cinco proyectos
(`Dominio`, `Aplicacion`, `Infraestructura`, `Api`, `Pruebas`) bajo `src/` en
`BlackHellHeim.sln`, con la matriz de dependencias hacia adentro vigilada por una
prueba de arquitectura que lee las referencias de los `.csproj`. La API mínima expone
el endpoint de salud `GET /salud`, definido primero en `contratos/openapi.yaml`
(principio II). La verificación completa ocurre con un solo comando:
`dotnet test` sobre la solución.

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (SDK .NET 10; destino `net10.0`)

**Dependencias principales**:
- ASP.NET Core (API mínima; framework compartido, MIT) para el proyecto `Api`.
- Pruebas: `xunit` y `xunit.runner.visualstudio` (Apache 2.0, verificado),
  `Microsoft.NET.Test.Sdk` (MIT, verificado) y `Microsoft.AspNetCore.Mvc.Testing`
  (MIT, verificado) para la prueba en proceso del endpoint.
- Sin MediatR 13+, sin AutoMapper y sin FluentAssertions (principio IV). Ningún
  paquete adicional; licencias anotadas en `investigacion.md`.

**Almacenamiento**: N/A en esta historia (sin base de datos; llega en su historia propia).

**Pruebas**: xUnit, tres frentes: (1) prueba de arquitectura que valida la matriz de
referencias entre proyectos con casos sintéticos de violación (Dominio hacia otra capa,
ciclo, proyecto desconocido); (2) prueba en proceso del endpoint `/salud` con
`WebApplicationFactory`; (3) la compilación de la solución completa dentro del comando
único. Comando único: `dotnet test BlackHellHeim.sln`.

**Plataforma destino**: API HTTP en servidor Linux; desarrollo local.

**Tipo de proyecto**: servicio web (API).

**Metas de rendimiento**: N/A (la historia no fija métricas de rendimiento; los criterios
de éxito son funcionales).

**Restricciones**: un solo comando de verificación; solo software libre; español en
código y artefactos.

**Escala/Alcance**: 5 proyectos, 1 endpoint (`/salud`), entre 5 y 7 pruebas automatizadas.

## Verificación de la constitución

*COMPUERTA: debe pasar antes de la investigación de la Fase 0. Se vuelve a comprobar
tras el diseño de la Fase 1.*

| # | Principio | Estado | Nota |
|---|-----------|--------|------|
| I | Arquitectura limpia | ✅ | Cinco capas bajo `src/`; matriz de dependencias hacia adentro vigilada por prueba automática. CQRS/Mediator: sin casos de uso aún, no aplica en esta historia. |
| II | Contrato primero | ✅ | `contratos/openapi.yaml` define `GET /salud` antes de implementar el endpoint. La infraestructura OpenAPI completa (Scalar, servidor mock, publicación al frontend) llega en su historia propia de la fundación. |
| III | Reglas de negocio probadas | ✅ | La regla de dependencias tiene pruebas de caso válido y de violación (sintéticas); `/salud` tiene prueba de respuesta exitosa. |
| IV | Solo software libre | ✅ | xUnit (Apache 2.0), Microsoft.NET.Test.Sdk (MIT), Microsoft.AspNetCore.Mvc.Testing (MIT), .NET 10/ASP.NET Core (MIT). Licencias verificadas y anotadas en `investigacion.md`. |
| V | El precio lo calcula el backend | N/A | Sin precios en esta historia. |
| VI | Pedidos idempotentes y webhooks | N/A | Sin pedidos ni webhooks. |
| VII | Puertos y adaptadores | N/A | Sin proveedores externos aún. |
| VIII | Seguridad y secretos | ✅ | Ningún secreto en el repositorio; `/salud` no expone datos sensibles. |
| IX | Código de creadora | N/A | Sin código de creadora. |
| X | Diseños originales | N/A | Sin diseños. |
| XI | Idioma y estilo | ✅ | Especificaciones, plan, código y commits en español; marca **BlackHellHeim**. |

Recomprobación tras la Fase 1: sin cambios; el diseño (contrato, matriz, pruebas)
mantiene todas las compuertas en verde. Sin violaciones que justificar: la tabla de
complejidad queda vacía.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/002-estructura-base-api/
├── plan.md               # Este archivo (salida de /speckit.plan)
├── investigacion.md      # Salida de la Fase 0 (/speckit.plan)
├── modelo-datos.md       # Salida de la Fase 1 (/speckit.plan)
├── inicio-rapido.md      # Salida de la Fase 1 (/speckit.plan)
├── contratos/
│   └── openapi.yaml      # Contrato de GET /salud (Fase 1)
├── checklists/
│   └── requirements.md   # Lista de verificación de la especificación
└── tasks.md              # Salida de /speckit.tasks (no lo crea este comando)
```

*Nombres en español conforme a AGENTS.md y al principio XI: `investigacion.md`
corresponde a `research.md` y `inicio-rapido.md` a `quickstart.md` de la plantilla.*

### Código fuente (raíz del repositorio)

```text
BlackHellHeim.sln
src/
├── Dominio/            # BlackHellHeim.Dominio → sin referencias
├── Aplicacion/         # BlackHellHeim.Aplicacion → Dominio
├── Infraestructura/    # BlackHellHeim.Infraestructura → Aplicacion, Dominio
├── Api/                # BlackHellHeim.Api → Aplicacion (API mínima; GET /salud)
└── Pruebas/            # BlackHellHeim.Pruebas → las cuatro capas (xUnit)
```

**Decisión de estructura**: Clean Architecture con cinco proyectos bajo `src/` y
solución `BlackHellHeim.sln` en la raíz. Matriz de referencias permitidas, que es lo
que vigila la prueba de arquitectura:

| Proyecto | Referencias permitidas |
|----------|------------------------|
| `BlackHellHeim.Dominio` | ninguna |
| `BlackHellHeim.Aplicacion` | `Dominio` |
| `BlackHellHeim.Infraestructura` | `Aplicacion`, `Dominio` |
| `BlackHellHeim.Api` | `Aplicacion`, `Dominio`, `Infraestructura` (raíz de composición) |
| `BlackHellHeim.Pruebas` | las cuatro capas |

La prueba de arquitectura enumera todos los `.csproj` bajo `src/`: un proyecto fuera de
la matriz falla la prueba (sin puntos ciegos; caso límite de la especificación).

## Seguimiento de complejidad

> Solo se llena si la verificación de la constitución tuviera violaciones que justificar.

Sin violaciones: tabla vacía.
