# Modelo de datos 020
| Tabla | Campos | Restricciones |
|---|---|---|
| `TiposCambioDiarios` | Id, Fecha (date, CDMX), Valor decimal(10,4), Fuente, ObtenidoUtc, EsReserva bool | Índice único en `Fecha`. |
| `ParametrosPrecio` | Id, ColchonCambiario decimal(5,4), Margen decimal(5,4), ComisionPasarela decimal(5,4), ImpresionExtraUsd decimal(10,2), VigenteDesdeUtc, ActualizadoPor | Versionado: el vigente es el último; semilla inicial por migración. |

Migración: `AgregarPrecios`.
