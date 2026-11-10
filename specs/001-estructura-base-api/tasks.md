# Tareas: Estructura base del API

**Entrada**: Documentos de diseño en `specs/001-estructura-base-api/` (spec.md, plan.md,
investigacion.md, modelo-datos.md, contratos/openapi.yaml, inicio-rapido.md)

**Requisitos previos**: plan.md (obligatorio), spec.md (obligatorio, historias de usuario),
investigacion.md (decisiones), modelo-datos.md, contratos/openapi.yaml

**Pruebas**: excluidas de esta ronda por decisión de la usuaria (sin pruebas unitarias, ni
prueba de arquitectura, ni pruebas del endpoint). Ver «Fases diferidas»: la historia no puede
cerrarse sin ellas (principio III de la constitución).

**Organización**: tareas agrupadas por historia de usuario, en el orden pedido: estructura de
proyectos y solución → endpoint `/salud` → verificación (`dotnet build` y `dotnet run`).

## Formato: `[ID] [BE|DB|CI] [P?] [HU?] Descripción`

- **[BE]/[DB]/[CI]**: etiqueta para importación a Jira (proyecto BHH); en esta historia casi
  todas son [BE]
- **[P]**: ejecutable en paralelo (archivos distintos, sin dependencias)
- **[HU]**: historia de usuario a la que pertenece la tarea (HU1, HU4)
- Toda tarea incluye la ruta exacta del archivo que toca

## Convención de rutas

Estructura según plan.md: cinco proyectos bajo `src/` y `BlackHellHeim.sln` en la raíz del
repositorio.

## Fase 1: Configuración (infraestructura compartida)

**Propósito**: inicialización del proyecto

- [ ] T001 [BE] Crear la solución en la raíz del repositorio con `dotnet new sln -n BlackHellHeim` (formato `.sln`). Ruta: `BlackHellHeim.sln`

---

## Fase 2: Fundacional (requisitos previos bloqueantes)

**Propósito**: estructura de proyectos que DEBE existir antes que cualquier otra tarea

**⚠️ CRÍTICO**: nada puede comenzar hasta completar esta fase

- [ ] T002 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Dominio` (destino `net10.0`) con `dotnet new classlib -o src/Dominio -n BlackHellHeim.Dominio -f net10.0` y eliminar el marcador `src/Dominio/Class1.cs`. Ruta: `src/Dominio/BlackHellHeim.Dominio.csproj`
- [ ] T003 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Aplicacion` (destino `net10.0`) con `dotnet new classlib -o src/Aplicacion -n BlackHellHeim.Aplicacion -f net10.0` y eliminar el marcador `src/Aplicacion/Class1.cs`. Ruta: `src/Aplicacion/BlackHellHeim.Aplicacion.csproj`
- [ ] T004 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Infraestructura` (destino `net10.0`) con `dotnet new classlib -o src/Infraestructura -n BlackHellHeim.Infraestructura -f net10.0` y eliminar el marcador `src/Infraestructura/Class1.cs`. Ruta: `src/Infraestructura/BlackHellHeim.Infraestructura.csproj`
- [ ] T005 [BE] [P] Crear el proyecto de API mínima `BlackHellHeim.Api` con `dotnet new web -o src/Api -n BlackHellHeim.Api -f net10.0` (genera `src/Api/Program.cs`, `src/Api/Properties/launchSettings.json` y `src/Api/appsettings.json`). Ruta: `src/Api/BlackHellHeim.Api.csproj`
- [ ] T006 [BE] [P] Crear la biblioteca de clases `BlackHellHeim.Pruebas` (destino `net10.0`) con `dotnet new classlib -o src/Pruebas -n BlackHellHeim.Pruebas -f net10.0` y eliminar el marcador `src/Pruebas/Class1.cs`. Sin paquetes de pruebas aún (ver «Fases diferidas»). Ruta: `src/Pruebas/BlackHellHeim.Pruebas.csproj`
- [ ] T007 [BE] Agregar los cinco proyectos a la solución con `dotnet sln BlackHellHeim.sln add src/Dominio/BlackHellHeim.Dominio.csproj src/Aplicacion/BlackHellHeim.Aplicacion.csproj src/Infraestructura/BlackHellHeim.Infraestructura.csproj src/Api/BlackHellHeim.Api.csproj src/Pruebas/BlackHellHeim.Pruebas.csproj`. Ruta: `BlackHellHeim.sln`

**Punto de control**: fundación lista; puede comenzar el trabajo por historia

---

## Fase 3: Historia de usuario 1 — Solución en capas con dependencias hacia adentro (Prioridad: P1) 🎯 MVP

**Meta**: cablear la matriz de referencias hacia adentro (RF-001 a RF-004 de la spec)

**Prueba independiente**: inspeccionar los `ProjectReference` de los cinco `.csproj` contra la
matriz de plan.md y compilar con `dotnet build BlackHellHeim.sln`

### Implementación de la historia de usuario 1

- [ ] T008 [BE] [P] [HU1] Agregar en `src/Aplicacion/BlackHellHeim.Aplicacion.csproj` la referencia de proyecto a `src/Dominio/BlackHellHeim.Dominio.csproj` (Aplicación depende solo de Dominio, RF-003)
- [ ] T009 [BE] [P] [HU1] Agregar en `src/Infraestructura/BlackHellHeim.Infraestructura.csproj` las referencias de proyecto a `src/Aplicacion/BlackHellHeim.Aplicacion.csproj` y `src/Dominio/BlackHellHeim.Dominio.csproj`
- [ ] T010 [BE] [P] [HU1] Agregar en `src/Api/BlackHellHeim.Api.csproj` la referencia de proyecto a `src/Aplicacion/BlackHellHeim.Aplicacion.csproj` (raíz de composición)
- [ ] T011 [BE] [P] [HU1] Agregar en `src/Pruebas/BlackHellHeim.Pruebas.csproj` las referencias de proyecto a las cuatro capas: `src/Dominio/BlackHellHeim.Dominio.csproj`, `src/Aplicacion/BlackHellHeim.Aplicacion.csproj`, `src/Infraestructura/BlackHellHeim.Infraestructura.csproj` y `src/Api/BlackHellHeim.Api.csproj`

**Punto de control**: la matriz de plan.md queda cableada; `Dominio` queda sin referencias
(RF-002) por omisión

---

## Fase 4: Historia de usuario 4 — Arranque de la API con comprobación de estado (Prioridad: P4)

**Meta**: la API responde la comprobación de estado según el contrato (RF-007, RF-008)

**Prueba independiente**: `dotnet run --project src/Api` y una petición `GET /salud` debe
responder `200` con `{ "estado": "ok" }`

### Implementación de la historia de usuario 4

- [ ] T012 [BE] [HU4] Implementar en `src/Api/Program.cs` el endpoint `GET /salud` que responde `200` con `application/json` y cuerpo `{ "estado": "ok" }` según `specs/001-estructura-base-api/contratos/openapi.yaml`; retirar la ruta de ejemplo `/` que trae la plantilla. Ruta: `src/Api/Program.cs`

**Punto de control**: la API arranca y la sonda `/salud` responde

---

## Fases diferidas: HU2 y HU3 (tareas de pruebas, excluidas de esta ronda)

Por decisión de la usuaria, esta ronda no incluye tareas de pruebas. Quedan pendientes para
cerrar la historia 001:

- **HU2 (P2)**: convertir `BlackHellHeim.Pruebas` en proyecto xUnit (paquetes `xunit` y
  `xunit.runner.visualstudio`, Apache 2.0; `Microsoft.NET.Test.Sdk`, MIT) e implementar la
  prueba de arquitectura de la matriz de referencias con casos sintéticos de violación
  (`investigacion.md`, D1)
- **HU3 (P3)**: el comando único de verificación pasa de `dotnet build` a
  `dotnet test BlackHellHeim.sln` cuando existan pruebas (RF-006, `investigacion.md`, D3)

**⚠️ Constitución (principio III): sin pruebas no hay «Hecho». La historia 001 no puede
marcarse como terminada hasta agregarlas (RF-005, RF-006, CE-001 a CE-004).**

---

## Fase final: Verificación y detalles transversales

**Propósito**: comprobar la solución completa de extremo a extremo

- [ ] T013 [BE] Ejecutar `dotnet build BlackHellHeim.sln` y confirmar que los cinco proyectos compilan sin errores (HU1, escenario 3). Solución: `BlackHellHeim.sln`
- [ ] T014 [BE] Arrancar la API con `dotnet run --project src/Api` y comprobar que `GET /salud` responde `200` con `{ "estado": "ok" }`, siguiendo `specs/001-estructura-base-api/inicio-rapido.md` (HU4). Proyecto: `src/Api/BlackHellHeim.Api.csproj`
- [ ] T015 [BE] [P] Crear `.gitignore` en la raíz del repositorio con las rutas de artefactos de .NET (`bin/`, `obj/`, `.vs/`) para que la primera compilación no ensucie el repositorio. Ruta: `.gitignore`

**Punto de control**: la estructura base queda verificada y el repositorio limpio

---

## Dependencias y orden de ejecución

### Dependencias de fases

- **Fase 1 (Configuración)**: sin dependencias; comenzar de inmediato
- **Fase 2 (Fundacional)**: depende de la Fase 1; BLOQUEA todas las demás
- **Fase 3 (HU1)**: depende de la Fase 2
- **Fase 4 (HU4)**: depende de la Fase 3 (necesita la estructura cableada y compilable)
- **Fase final**: depende de la Fase 4
- **Fases diferidas (HU2, HU3)**: ronda posterior; HU2 depende de la Fase 3, y HU3 de HU2 y
  de la Fase 4

### Dependencias entre historias

- **HU1 (P1)**: puede comenzar tras la Fase 2; sin dependencias de otras historias
- **HU4 (P4)**: tras la Fase 3; usa la estructura de HU1
- **HU2 y HU3**: diferidas en esta ronda (pruebas)

### Dentro de cada historia

- Creación antes que cableado; cableado antes que verificación
- La historia completa antes de pasar a la siguiente prioridad

### Oportunidades en paralelo

- T002–T006 en paralelo (directorios distintos)
- T008–T011 en paralelo (`.csproj` distintos)
- T015 en paralelo con T013/T014 (archivos distintos)

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
5. (Ronda posterior) HU2 y HU3 → pruebas y comando único `dotnet test`; ahí la historia puede
   cerrarse conforme a la constitución

---

## Notas

- [P] = archivos distintos, sin dependencias entre tareas pendientes
- La etiqueta [HU] da trazabilidad hacia las historias de spec.md
- Commitear tras cada tarea o grupo lógico, con mensajes en español (principio XI)
- Evitar: tareas vagas, conflictos sobre el mismo archivo, dependencias cruzadas entre
  historias
