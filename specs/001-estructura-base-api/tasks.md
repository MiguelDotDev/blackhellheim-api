# Tareas: Estructura base del API

**Entrada**: Documentos de diseño en `specs/001-estructura-base-api/` (spec.md, plan.md,
investigacion.md, modelo-datos.md, contratos/openapi.yaml, inicio-rapido.md)

**Requisitos previos**: plan.md (obligatorio), spec.md (obligatorio, historias de usuario),
investigacion.md (decisiones), modelo-datos.md, contratos/openapi.yaml

**Pruebas**: excluidas de la ronda 1 (T001–T015) por decisión de la usuaria. La ronda 2
(T016–T026) agrega las fases diferidas: prueba de arquitectura (HU2), comando único
`dotnet test` (HU3) y prueba en proceso del endpoint (HU4); con ellas la historia puede
cerrarse (principio III de la constitución).

**Organización**: tareas agrupadas por historia de usuario en dos rondas: ronda 1 (T001–T015:
estructura → endpoint `/salud` → verificación) y ronda 2 (T016–T026: pruebas, «pruebas
primero»).

## Formato: `[ID] [BE|DB|CI] [P?] [HU?] Descripción`

- **[BE]/[DB]/[CI]**: etiqueta para importación a Jira (proyecto BHH); en esta historia casi
  todas son [BE]
- **[P]**: ejecutable en paralelo (archivos distintos, sin dependencias)
- **[HU]**: historia de usuario a la que pertenece la tarea (HU1–HU4)
- Toda tarea incluye la ruta exacta del archivo que toca

## Convención de rutas

Estructura según plan.md: cinco proyectos bajo `src/` y `BlackHellHeim.sln` en la raíz del
repositorio.

## Fase 1: Configuración (infraestructura compartida)

**Propósito**: inicialización del proyecto

- [x] T001 [BE] Crear la solución en la raíz del repositorio con `dotnet new sln -n BlackHellHeim` (formato `.sln`). Ruta: `BlackHellHeim.sln`

---

## Fase 2: Fundacional (requisitos previos bloqueantes)

**Propósito**: estructura de proyectos que DEBE existir antes que cualquier otra tarea

**⚠️ CRÍTICO**: nada puede comenzar hasta completar esta fase

- [x] T002 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Dominio` (destino `net10.0`) con `dotnet new classlib -o src/Dominio -n BlackHellHeim.Dominio -f net10.0` y eliminar el marcador `src/Dominio/Class1.cs`. Ruta: `src/Dominio/BlackHellHeim.Dominio.csproj`
- [x] T003 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Aplicacion` (destino `net10.0`) con `dotnet new classlib -o src/Aplicacion -n BlackHellHeim.Aplicacion -f net10.0` y eliminar el marcador `src/Aplicacion/Class1.cs`. Ruta: `src/Aplicacion/BlackHellHeim.Aplicacion.csproj`
- [x] T004 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Infraestructura` (destino `net10.0`) con `dotnet new classlib -o src/Infraestructura -n BlackHellHeim.Infraestructura -f net10.0` y eliminar el marcador `src/Infraestructura/Class1.cs`. Ruta: `src/Infraestructura/BlackHellHeim.Infraestructura.csproj`
- [x] T005 [BE] [P] Crear el proyecto de API mínima `BlackHellHeim.Api` con `dotnet new web -o src/Api -n BlackHellHeim.Api -f net10.0` (genera `src/Api/Program.cs`, `src/Api/Properties/launchSettings.json` y `src/Api/appsettings.json`). Ruta: `src/Api/BlackHellHeim.Api.csproj`
- [x] T006 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Pruebas` (destino `net10.0`) con `dotnet new classlib -o src/Pruebas -n BlackHellHeim.Pruebas -f net10.0` y eliminar el marcador `src/Pruebas/Class1.cs`. Sin paquetes de pruebas aún (ver «Fases diferidas»). Ruta: `src/Pruebas/BlackHellHeim.Pruebas.csproj`
- [x] T007 [BE] Agregar los cinco proyectos a la solución con `dotnet sln BlackHellHeim.sln add src/Dominio/BlackHellHeim.Dominio.csproj src/Aplicacion/BlackHellHeim.Aplicacion.csproj src/Infraestructura/BlackHellHeim.Infraestructura.csproj src/Api/BlackHellHeim.Api.csproj src/Pruebas/BlackHellHeim.Pruebas.csproj`. Ruta: `BlackHellHeim.sln`

**Punto de control**: fundación lista; puede comenzar el trabajo por historia

---

## Fase 3: Historia de usuario 1 — Solución en capas con dependencias hacia adentro (Prioridad: P1) 🎯 MVP

**Meta**: cablear la matriz de referencias hacia adentro (RF-001 a RF-004 de la spec)

**Prueba independiente**: inspeccionar los `ProjectReference` de los cinco `.csproj` contra la
matriz de plan.md y compilar con `dotnet build BlackHellHeim.sln`

### Implementación de la historia de usuario 1

- [x] T008 [BE] [P] [HU1] Agregar en `src/Aplicacion/BlackHellHeim.Aplicacion.csproj` la referencia de proyecto a `src/Dominio/BlackHellHeim.Dominio.csproj` (Aplicación depende solo de Dominio, RF-003)
- [x] T009 [BE] [P] [HU1] Agregar en `src/Infraestructura/BlackHellHeim.Infraestructura.csproj` las referencias de proyecto a `src/Aplicacion/BlackHellHeim.Aplicacion.csproj` y `src/Dominio/BlackHellHeim.Dominio.csproj`
- [x] T010 [BE] [P] [HU1] Agregar en `src/Api/BlackHellHeim.Api.csproj` la referencia de proyecto a `src/Aplicacion/BlackHellHeim.Aplicacion.csproj` (raíz de composición)
- [x] T011 [BE] [P] [HU1] Agregar en `src/Pruebas/BlackHellHeim.Pruebas.csproj` las referencias de proyecto a las cuatro capas: `src/Dominio/BlackHellHeim.Dominio.csproj`, `src/Aplicacion/BlackHellHeim.Aplicacion.csproj`, `src/Infraestructura/BlackHellHeim.Infraestructura.csproj` y `src/Api/BlackHellHeim.Api.csproj`

**Punto de control**: la matriz de plan.md queda cableada; `Dominio` queda sin referencias
(RF-002) por omisión

---

## Fase 4: Historia de usuario 4 — Arranque de la API con comprobación de estado (Prioridad: P4)

**Meta**: la API responde la comprobación de estado según el contrato (RF-007, RF-008)

**Prueba independiente**: `dotnet run --project src/Api` y una petición `GET /salud` debe
responder `200` con `{ "estado": "ok" }`

### Implementación de la historia de usuario 4

- [x] T012 [BE] [HU4] Implementar en `src/Api/Program.cs` el endpoint `GET /salud` que responde `200` con `application/json` y cuerpo `{ "estado": "ok" }` según `specs/001-estructura-base-api/contratos/openapi.yaml`; retirar la ruta de ejemplo `/` que trae la plantilla. Ruta: `src/Api/Program.cs`

**Punto de control**: la API arranca y la sonda `/salud` responde

---

## Fases diferidas → ronda 2 planificada

Las HU2 y HU3 se difereron en la ronda 1 por decisión de la usuaria, junto con la prueba en
proceso del endpoint. Ya están planificadas al final de este archivo (ronda 2: Fases 5–7 y
fase final, tareas T016–T026). **⚠️ Constitución (principio III): sin pruebas no hay
«Hecho»; la historia 001 se cierra hasta completar esa ronda (RF-005, RF-006, CE-001 a
CE-004).**

---

## Fase final: Verificación y detalles transversales

**Propósito**: comprobar la solución completa de extremo a extremo

- [x] T013 [BE] Ejecutar `dotnet build BlackHellHeim.sln` y confirmar que los cinco proyectos compilan sin errores (HU1, escenario 3). Solución: `BlackHellHeim.sln`
- [x] T014 [BE] Arrancar la API con `dotnet run --project src/Api` y comprobar que `GET /salud` responde `200` con `{ "estado": "ok" }`, siguiendo `specs/001-estructura-base-api/inicio-rapido.md` (HU4). Proyecto: `src/Api/BlackHellHeim.Api.csproj`
- [x] T015 [BE] [P] Crear `.gitignore` en la raíz del repositorio con las rutas de artefactos de .NET (`bin/`, `obj/`, `.vs/`) para que la primera compilación no ensucie el repositorio. Ruta: `.gitignore`

**Punto de control**: la estructura base queda verificada y el repositorio limpio

---

## Ronda 2 · Fase 5: Historia de usuario 2 — Prueba automática de la regla de dependencias (Prioridad: P2)

**Meta**: una prueba que falle cuando cualquier capa rompa la regla de dependencias,
identificando la dependencia infractora (RF-005, escenarios de aceptación 1–3 de HU2 y
casos límite de ciclos y capas nuevas)

**Prueba independiente**: con la solución válida las pruebas de arquitectura pasan
(escenario 1, CE-003); los casos sintéticos demuestran que una violación real haría fallar
la prueba señalando al infractor (escenarios 2 y 3, CE-002), sin tocar los `.csproj` reales
(`investigacion.md`, D1)

**⚠️ Pruebas primero (TDD)**: T017–T018 se escriben antes que T019–T020; la solución no
compila hasta completar la fase (rojo → verde)

### Pruebas de la historia de usuario 2

- [x] T016 [BE] [HU2] Convertir `BlackHellHeim.Pruebas` en proyecto xUnit agregando a `src/Pruebas/BlackHellHeim.Pruebas.csproj` los paquetes `xunit`, `xunit.runner.visualstudio` (Apache 2.0) y `Microsoft.NET.Test.Sdk` (MIT), uno por comando `dotnet add src/Pruebas/BlackHellHeim.Pruebas.csproj package <paquete>`; licencias ya verificadas en `investigacion.md` (D5). Ruta: `src/Pruebas/BlackHellHeim.Pruebas.csproj`
- [x] T017 [BE] [P] [HU2] Crear la prueba del grafo real en `src/Pruebas/Arquitectura/MatrizReferenciasPruebas.cs`: un [Fact] que llame a `LectorGrafoProyectos.LeerGrafo()` (devuelve `IReadOnlyDictionary<string, IReadOnlyCollection<string>>` de nombre de proyecto → nombres referenciados) y afirme que `MatrizReferencias.Validar(grafo)` no reporta violaciones y que el grafo contiene exactamente los cinco proyectos `BlackHellHeim.Dominio`, `BlackHellHeim.Aplicacion`, `BlackHellHeim.Infraestructura`, `BlackHellHeim.Api` y `BlackHellHeim.Pruebas` (sin puntos ciegos: caso límite de capa nueva). Ruta: `src/Pruebas/Arquitectura/MatrizReferenciasPruebas.cs`
- [x] T018 [BE] [P] [HU2] Crear los casos sintéticos de violación en `src/Pruebas/Arquitectura/ViolacionesSinteticasPruebas.cs`: una [Theory] con grafos construidos en memoria que afirme que `MatrizReferencias.Validar` reporta la dependencia infractora en cada caso: (1) `Dominio` → `Aplicacion` (escenario 2 de HU2); (2) `Aplicacion` → `Infraestructura`, capa distinta de `Dominio` (escenario 3 de HU2); (3) ciclo `Dominio` ↔ `Aplicacion` (caso límite de referencias mutuas); (4) proyecto desconocido bajo `src/` (caso límite de capa nueva). Ruta: `src/Pruebas/Arquitectura/ViolacionesSinteticasPruebas.cs`

### Implementación de apoyo de la historia de usuario 2 (pone las pruebas en verde)

- [x] T019 [BE] [P] [HU2] Implementar el lector del grafo en `src/Pruebas/Arquitectura/LectorGrafoProyectos.cs`: clase estática con `LeerGrafo()` que localiza la raíz del repositorio buscando `BlackHellHeim.sln` hacia arriba desde `AppContext.BaseDirectory`, enumera los `.csproj` bajo `src/` y devuelve, por proyecto (nombre del `.csproj` sin extensión), los nombres de sus `ProjectReference`, leyendo el XML con `XDocument` y sin paquetes nuevos. Ruta: `src/Pruebas/Arquitectura/LectorGrafoProyectos.cs`
- [x] T020 [BE] [P] [HU2] Implementar la matriz y el validador en `src/Pruebas/Arquitectura/MatrizReferencias.cs`: constante `ReferenciasPermitidas` con la matriz exacta de plan.md — `BlackHellHeim.Dominio`: ninguna; `BlackHellHeim.Aplicacion`: `Dominio`; `BlackHellHeim.Infraestructura`: `Aplicacion` y `Dominio`; `BlackHellHeim.Api`: `Aplicacion`, `Dominio` e `Infraestructura`; `BlackHellHeim.Pruebas`: las cuatro capas — y `Validar(grafo)` que devuelva la lista de violaciones: «proyecto desconocido: X» por cada proyecto fuera de la matriz y «referencia no permitida: X → Y» por cada referencia no permitida, señalando siempre la dependencia infractora (RF-005). Ruta: `src/Pruebas/Arquitectura/MatrizReferencias.cs`

**Punto de control**: `dotnet test BlackHellHeim.sln` en verde con las pruebas de arquitectura pasando

---

## Ronda 2 · Fase 6: Historia de usuario 3 — Compilación y pruebas con un solo comando (Prioridad: P3)

**Meta**: `dotnet test BlackHellHeim.sln` compila la solución completa y ejecuta todas sus
pruebas sin pasos manuales (RF-006, escenarios 1–3 de HU3); sustituye a `dotnet build`
(T013) como verificación final de la historia (`investigacion.md`, D3)

**Prueba independiente**: ejecutar el comando único y observar que compila y ejecuta todas
las pruebas; con al menos una en rojo termina con estado de error, sin éxito parcial

- [x] T021 [BE] [HU3] Ejecutar `dotnet test BlackHellHeim.sln` y confirmar que compila los cinco proyectos y ejecuta en verde todas las pruebas existentes hasta este punto, sin pasos previos (el comando restaura y compila por sí mismo: escenarios 1 y 3 de HU3). Solución: `BlackHellHeim.sln`
- [x] T022 [BE] [HU3] Verificar el escenario 2 de HU3 (sin éxito parcial): crear temporalmente `src/Pruebas/PruebaTemporalEnRojo.cs` con una prueba xUnit que siempre falla (`Assert.True(false)`), ejecutar `dotnet test BlackHellHeim.sln` y confirmar que termina con estado de error, **eliminar** el archivo temporal y re-ejecutar el comando para volver al verde. Ruta: `src/Pruebas/PruebaTemporalEnRojo.cs` (archivo temporal)

**Punto de control**: el comando único se comporta según los tres escenarios de aceptación de HU3

---

## Ronda 2 · Fase 7: Historia de usuario 4 — Prueba en proceso de la comprobación de estado (Prioridad: P4)

**Meta**: automatizar CE-004 dentro del comando único con una prueba en proceso que arranca
la API y verifica la respuesta `200` con `{ "estado": "ok" }` (`investigacion.md`, D4;
escenarios 1 y 2 de HU4)

**Prueba independiente**: la prueba del endpoint pasa dentro de `dotnet test
BlackHellHeim.sln` sin red ni puertos reales

**⚠️ Pruebas primero (TDD)**: T024 se escribe antes que T025; hasta completar T025 la
solución no compila

- [x] T023 [BE] [HU4] Agregar el paquete `Microsoft.AspNetCore.Mvc.Testing` (MIT, verificado en `investigacion.md`, D5) a `src/Pruebas/BlackHellHeim.Pruebas.csproj` con `dotnet add src/Pruebas/BlackHellHeim.Pruebas.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.15` (la versión 11.x no es compatible con `net10.0`). Ruta: `src/Pruebas/BlackHellHeim.Pruebas.csproj`
- [x] T024 [BE] [P] [HU4] Crear la prueba en proceso en `src/Pruebas/Api/SaludPruebas.cs`: con `WebApplicationFactory<Program>` obtener un cliente HTTP, solicitar `GET /salud` y afirmar estado `200`, encabezado `Content-Type: application/json` y cuerpo `{ "estado": "ok" }` conforme a `specs/001-estructura-base-api/contratos/openapi.yaml`. Ruta: `src/Pruebas/Api/SaludPruebas.cs`
- [x] T025 [BE] [P] [HU4] Exponer `Program` al proyecto de pruebas agregando al final de `src/Api/Program.cs` la declaración `public partial class Program { }`, requerida por `WebApplicationFactory<Program>` con API mínima; no altera el comportamiento del endpoint. Ruta: `src/Api/Program.cs`

**Punto de control**: la comprobación de estado queda probada en proceso, sin red ni puertos

---

## Ronda 2 · Fase final: Comando único de cierre

**Propósito**: verificar la historia completa con el comando único definitivo, que sustituye
a `dotnet build` (T013) como verificación final (RF-006, CE-001)

- [x] T026 [BE] Ejecutar `dotnet test BlackHellHeim.sln` como comando único de cierre: compila los cinco proyectos y ejecuta todas las pruebas (grafo real, casos sintéticos y comprobación de estado) con resultado exitoso. Solución: `BlackHellHeim.sln`

**Punto de control**: con todas las pruebas en verde el principio III queda satisfecho y la
historia 001 puede cerrarse

---

## Dependencias y orden de ejecución

### Dependencias de fases

- **Fase 1 (Configuración)**: sin dependencias; comenzar de inmediato
- **Fase 2 (Fundacional)**: depende de la Fase 1; BLOQUEA todas las demás
- **Fase 3 (HU1)**: depende de la Fase 2
- **Fase 4 (HU4)**: depende de la Fase 3 (necesita la estructura cableada y compilable)
- **Fase final (ronda 1)**: depende de la Fase 4; completada con T001–T015
- **Ronda 2 · Fase 5 (HU2)**: depende de T001–T015 (estructura cableada); BLOQUEA el resto
  de la ronda 2
- **Ronda 2 · Fase 6 (HU3)**: depende de la Fase 5 (necesita pruebas que ejecutar)
- **Ronda 2 · Fase 7 (HU4)**: depende de la Fase 5 (paquetes xUnit); va tras la Fase 6 por
  orden de prioridad (P4)
- **Ronda 2 · fase final**: depende de las Fases 5–7 (todas las pruebas existentes)

### Dependencias entre historias

- **HU1 (P1)**: ronda 1; usa la fundación de la Fase 2
- **HU2 (P2)**: ronda 2; vigila con la prueba de arquitectura la matriz que HU1 cableó
- **HU3 (P3)**: ronda 2; requiere pruebas existentes (HU2) para el comando único
- **HU4 (P4)**: implementación en la ronda 1; su prueba en proceso se agrega en la ronda 2

### Dentro de cada historia

- Creación antes que cableado; cableado antes que verificación
- La historia completa antes de pasar a la siguiente prioridad
- En la ronda 2: pruebas antes que el código de apoyo (TDD, rojo → verde por fase)

### Oportunidades en paralelo

- T002–T006 en paralelo (directorios distintos)
- T008–T011 en paralelo (`.csproj` distintos)
- T015 en paralelo con T013/T014 (archivos distintos)
- T017–T020 en paralelo (archivos distintos; compilan juntas al cierre de la fase)
- T024 y T025 en paralelo tras T023 (archivos distintos)

---

## Ejemplo en paralelo: Fase 2

```bash
# Lanzar juntas las creaciones de proyectos (directorios distintos):
Task: "T002 [BE] Crear BlackHellHeim.Dominio en src/Dominio/BlackHellHeim.Dominio.csproj"
Task: "T003 [BE] Crear BlackHellHeim.Aplicacion en src/Aplicacion/BlackHellHeim.Aplicacion.csproj"
Task: "T004 [BE] Crear BlackHellHeim.Infraestructura en src/Infraestructura/BlackHellHeim.Infraestructura.csproj"
Task: "T005 [BE] Crear BlackHellHeim.Api en src/Api/BlackHellHeim.Api.csproj"
Task: "T006 [BE] Crear BlackHellHeim.Pruebas en src/Pruebas/BlackHellHeim.Pruebas.csproj"
```

## Ejemplo en paralelo: Ronda 2 · Fase 5

```bash
# Tras T016, lanzar juntas las tareas de archivos distintos (compilan juntas al cierre):
Task: "T017 [BE] Prueba del grafo real en src/Pruebas/Arquitectura/MatrizReferenciasPruebas.cs"
Task: "T018 [BE] Casos sintéticos de violación en src/Pruebas/Arquitectura/ViolacionesSinteticasPruebas.cs"
Task: "T019 [BE] Lector del grafo en src/Pruebas/Arquitectura/LectorGrafoProyectos.cs"
Task: "T020 [BE] Matriz y validador en src/Pruebas/Arquitectura/MatrizReferencias.cs"
```

## Estrategia de implementación

### MVP primero (solo HU1)

1. Completar la Fase 1 (Configuración)
2. Completar la Fase 2 (Fundacional — crítica, bloquea todo)
3. Completar la Fase 3 (HU1: matriz de dependencias)
4. **DETENERSE Y VALIDAR**: `dotnet build BlackHellHeim.sln` en verde e inspección de la matriz

### Entrega incremental

1. Fases 1–2 → fundación lista
2. + Fase 3 (HU1) → estructura verificada (MVP)
3. + Fase 4 (HU4) → API ejecutable con `/salud`
4. + Fase final → verificación de extremo a extremo y repositorio limpio
5. Ronda 2 (T016–T026) → pruebas de arquitectura (HU2), comando único `dotnet test` (HU3) y
   prueba del endpoint (HU4); ahí la historia se cierra conforme a la constitución

---

## Notas

- [P] = archivos distintos, sin dependencias entre tareas pendientes
- Ronda 2 (T016–T026) amplía este archivo sin rehacer ni modificar T001–T015, ya completadas
- La etiqueta [HU] da trazabilidad hacia las historias de spec.md
- Commitear tras cada tarea o grupo lógico, con mensajes en español (principio XI)
- Evitar: tareas vagas, conflictos sobre el mismo archivo, dependencias cruzadas entre
  historias
