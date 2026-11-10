# Inicio rápido (Fase 1): Estructura base del API

Historia 001 · Fecha: 2026-11-10

Cómo validar de extremo a extremo que la historia funciona. Los detalles de
implementación viven en `tasks.md` (por generarse con `/speckit.tasks`).

## Requisitos previos

- SDK de .NET 10 instalado (`dotnet --version` reporta 10.x).
- Nada más: sin base de datos, almacenamiento ni proveedores externos.

## Validación con un solo comando

```bash
dotnet test BlackHellHeim.sln
```

Resultado esperado:
- La solución completa compila (los cinco proyectos).
- Todas las pruebas pasan: la prueba de arquitectura sobre la matriz de referencias,
  sus casos sintéticos de violación y la prueba en proceso del endpoint `/salud`.
- El comando termina en verde; cualquier referencia fuera de la matriz o prueba en
  rojo lo rompe.

## Validación del arranque y la comprobación de estado

```bash
dotnet run --project src/Api
```

Con la API en marcha (el puerto se reporta al arrancar):

```bash
curl http://localhost:<puerto>/salud
```

Resultado esperado: estado HTTP 200 y cuerpo:

```json
{ "estado": "ok" }
```

La forma del contrato está en `contratos/openapi.yaml` y el dato expuesto, en
`modelo-datos.md`.
