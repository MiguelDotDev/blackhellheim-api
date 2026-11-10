# Modelo de datos (Fase 1): Estructura base del API

Historia 001 · Fecha: 2026-11-10

## Entidades

**No hay entidades de dominio ni persistencia en esta historia.** La fundación de datos
(SQL Server, migraciones de EF Core) llega en su historia propia. `Dominio` y
`Aplicacion` nacen vacíos como anclas de la arquitectura.

## Único dato expuesto: respuesta de `/salud`

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `estado` | cadena | sí | Valor constante `"ok"` cuando la API está en marcha |

- Sin relaciones, sin transiciones de estado y sin validación de entrada: el endpoint
  no recibe parámetros.
- La forma exacta del contrato vive en `contratos/openapi.yaml`; este archivo no la
  duplica.
- Cuando existan dependencias externas (base de datos, almacenamiento, proveedores),
  la comprobación de estado podrá crecer con estados degradados; hacerlo hoy sería
  diseño especulativo.
