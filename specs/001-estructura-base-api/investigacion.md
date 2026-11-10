# Investigación (Fase 0): Estructura base del API

Historia 001 · Fecha: 2026-11-10

El contexto técnico llegó resuelto en la descripción de la usuaria (.NET 10, Clean
Architecture, xUnit, API mínima con `/salud`, `dotnet test`); no quedan
NEEDS CLARIFICATION. Esta fase documenta las decisiones derivadas con su justificación
y alternativas.

## D1. Mecanismo de la prueba de arquitectura

**Decisión**: prueba propia en `BlackHellHeim.Pruebas` que lee los `.csproj` bajo
`src/` (nodos `ProjectReference`) y valida el grafo real contra la matriz permitida;
incluye casos sintéticos de violación (Dominio hacia otra capa, ciclo entre capas,
proyecto desconocido bajo `src/`) para probar el validador sin romper la compilación.

**Justificación**: la regla a vigilar es solo «referencias entre proyectos»; una prueba
de un archivo la cubre por completo sin dependencias nuevas (el principio IV exige
verificar la licencia de cada paquete nuevo; cero paquetes es el mínimo riesgo). Los
casos sintéticos cubren los escenarios de aceptación 2 y 3 de la historia de usuario 2
sin tocar los archivos reales.

**Alternativas descartadas**: NetArchTest (MIT) y ArchUnitNET (Apache 2.0), ambas
libres pero orientadas a analizar tipos y ensamblados; sobran para validar referencias
entre proyectos y añadirían dependencias que vigilar.

## D2. Contrato primero para `/salud`

**Decisión**: definir `GET /salud` en
`specs/001-estructura-base-api/contratos/openapi.yaml` antes de implementar el
endpoint, dentro de esta historia.

**Justificación**: el principio II exige que todo endpoint se defina en OpenAPI antes
de implementarse; como `/salud` se implementa aquí, su contrato se define aquí. La
infraestructura de OpenAPI (Scalar, servidor mock, publicación al frontend, pipeline de
ProblemDetails) sigue fuera de alcance y llega en su historia propia de la fundación.

**Alternativas descartadas**: implementar `/salud` sin contrato (viola el principio II)
o diferir el endpoint a la historia de OpenAPI (deja la especificación 001 sin su
RF-007).

## D3. Comando único de verificación

**Decisión**: `dotnet test BlackHellHeim.sln` (restaura, compila y ejecuta todas las
pruebas).

**Justificación**: `dotnet test` compila y prueba en un solo paso, cumple el RF-006 y
sirve de base para la integración continua futura (historia separada).

**Alternativas descartadas**: `dotnet build` + `dotnet test` por separado (dos pasos,
innecesario) y un script contenedor (indirección extra sin valor ahora).

## D4. Prueba del endpoint `/salud`

**Decisión**: prueba en proceso con `WebApplicationFactory` del paquete
`Microsoft.AspNetCore.Mvc.Testing` (MIT, verificado) que arranca la API y verifica la
respuesta 200 con cuerpo `{ "estado": "ok" }`.

**Justificación**: automatiza el criterio CE-004 dentro del comando único; no requiere
red ni puertos reales.

**Alternativas descartadas**: validación manual con `curl` (queda en
`inicio-rapido.md`, pero no sustituye a la prueba) y `Microsoft.AspNetCore.TestHost`
directo (misma función con más ceremonia).

## D5. Licencias verificadas (principio IV)

| Paquete | Licencia | Verificación |
|---------|----------|--------------|
| `xunit`, `xunit.runner.visualstudio` | Apache 2.0 | xunit.net / NuGet |
| `Microsoft.NET.Test.Sdk` | MIT | NuGet (repositorio vstest) |
| `Microsoft.AspNetCore.Mvc.Testing` | MIT | NuGet (repositorio aspnetcore) |
| .NET 10 SDK / ASP.NET Core | MIT | repositorios dotnet y aspnetcore |

Ningún paquete comercial; sin MediatR 13+, AutoMapper ni FluentAssertions.

## D6. Detalles menores

- **Respuesta de `/salud`**: 200 con `{ "estado": "ok" }`; propiedad en español y valor
  constante. Sin estados degradados: no hay dependencias que comprobar aún.
- **Nombres**: solución `BlackHellHeim.sln` en la raíz; proyectos
  `BlackHellHeim.{Capa}` bajo `src/`, sin acentos en rutas (`Aplicacion`), conforme a
  AGENTS.md.
- **Referencias reales en esta historia**: `Aplicacion → Dominio`,
  `Infraestructura → Aplicacion y Dominio`, `Api → Aplicacion`, `Pruebas → las cuatro`;
  todas dentro de lo que permite la matriz.
