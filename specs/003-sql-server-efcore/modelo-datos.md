# Modelo de datos: Base de datos local con migraciones y datos semilla

Historia 003 · Fecha: 2026-11-11

Esta historia crea el esquema mínimo de persistencia: una tabla de tiendas de referencia.
No incluye catálogo, pedidos, pagos ni archivos; esas tablas llegan con sus historias.

## Base de datos

| Elemento | Valor |
|----------|-------|
| Motor | SQL Server 2022 Developer Edition (contenedor oficial) |
| Nombre | `BlackHellHeim` |
| Puerto local | `1433` (configurable en `.env` si está ocupado) |
| Volumen | `datos-sqlserver` (persistente entre reinicios) |
| Esquema | `dbo` |
| Historial de migraciones | `dbo.__EFMigrationsHistory` (lo gestiona EF Core) |

## Tabla `dbo.Tiendas`

| Columna | Tipo SQL | Nulable | Regla |
|---------|----------|---------|-------|
| `Codigo` | `char(3)` no Unicode | No | Clave primaria; exactamente tres letras mayúsculas de la A a la Z |
| `Nombre` | `nvarchar(120)` | No | No vacío |
| `Moneda` | `char(3)` no Unicode | No | Código ISO de tres letras mayúsculas; `MXN` en las semillas |

- **Clave primaria**: `PK_Tiendas (Codigo)`.
- **Índices**: ninguno adicional; la clave primaria cubre la consulta por código.
- **Relaciones**: ninguna en esta historia.
- **Borrado**: no hay borrado en cascada porque no hay dependencias.

### Mapeo en `TiendaConfiguracion`

| Propiedad de dominio | Columna | Configuración EF Core |
|----------------------|---------|------------------------|
| `Codigo` (`CodigoTienda`) | `Codigo` | `HasConversion` a `string`, `HasMaxLength(3)`, `IsUnicode(false)`, `IsFixedLength()`, `ValueGeneratedNever()` |
| `Nombre` | `Nombre` | `IsRequired()`, `HasMaxLength(120)` |
| `Moneda` | `Moneda` | `IsRequired()`, `HasMaxLength(3)`, `IsUnicode(false)`, `IsFixedLength()` |

- La entidad `Tienda` y el objeto de valor `CodigoTienda` viven en `Dominio` y provienen de
  la historia 002b; esta historia no los redefine.
- `Infraestructura` es la única capa que conoce EF Core y el proveedor SQL Server.

## Datos semilla

| `Codigo` | `Nombre` | `Moneda` | Origen |
|----------|----------|----------|--------|
| `BHH` | `BlackHellHeim` | `MXN` | Tienda de referencia de la historia 002b |
| `DEM` | `Tienda de demostración` | `MXN` | Tienda de referencia de la historia 002b |

- Se declaran con `HasData` en `TiendaConfiguracion`, así que forman parte de la migración
  inicial.
- Son deterministas: no usan fechas, aleatorios ni datos del entorno.
- Son idempotentes: aplicar la migración dos veces no inserta filas repetidas porque la
  segunda ejecución no vuelve a correr la migración.
- Revertir la migración inicial elimina las dos filas junto con la tabla.

## Migración inicial

| Elemento | Valor |
|----------|-------|
| Nombre | `MigracionInicial` |
| Ubicación | `src/Infraestructura/Persistencia/Migraciones` |
| Crea | Tabla `dbo.Tiendas`, clave primaria y las dos filas semilla |
| Revierte | Elimina la tabla y, con ella, las semillas |
| Registro | Fila en `dbo.__EFMigrationsHistory` con `MigrationId` de la migración |

### Estados de una migración

```text
Pendiente ──aplicar──▶ Aplicada ──revertir──▶ Pendiente
     ▲                                            │
     └──────────────── aplicar ───────────────────┘
```

- **Pendiente**: existe en el código y no figura en el historial.
- **Aplicada**: figura en `__EFMigrationsHistory` y su esquema está en la base.
- **Fallo a mitad**: EF Core ejecuta cada migración en una transacción; si falla, no se
  registra como aplicada y la base queda en el estado anterior.
- Aplicar solo ejecuta las migraciones pendientes, en orden.

## Configuración de conexión

| Variable | Dónde se define | Contenido versionado | Uso |
|----------|-----------------|----------------------|-----|
| `BHH_SQL_SA_PASSWORD` | `.env` (ignorado por git) | Solo el marcador en `.env.example` | Contraseña de `sa` para el contenedor |
| `ConnectionStrings__BlackHellHeim` | `.env` (ignorado por git) | Solo el marcador en `.env.example` | Cadena de conexión de la API, la fábrica de diseño y las pruebas |

Cadena de ejemplo con marcadores (la del `.env.example`):

```text
Server=localhost,1433;Database=BlackHellHeim;User Id=sa;Password=__DEFINIR_CLAVE_LOCAL__;Encrypt=True;TrustServerCertificate=True
```

- `Encrypt=True` con `TrustServerCertificate=True` es aceptable solo en local, porque el
  contenedor usa un certificado autofirmado.
- La cadena real nunca se versiona ni se imprime en errores o registros.
- Si la variable falta o está vacía, la API y la fábrica de diseño fallan con un mensaje que
  nombra la variable y remite a `.env.example`.

## Contrato de la comprobación de la base

`GET /salud/base` no expone el esquema; solo informa disponibilidad.

| Situación | Código | Cuerpo |
|-----------|--------|--------|
| La base responde | 200 | `{ "estado": "disponible" }` |
| La base no responde, falla o supera 3 s | 503 | ProblemDetails con `codigo: base_no_disponible` e `idCorrelacion` |

El detalle del contrato está en `contratos/openapi.yaml`.
