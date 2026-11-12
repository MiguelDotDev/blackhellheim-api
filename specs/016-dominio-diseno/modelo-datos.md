# Modelo de datos 016
| Tabla | Campos clave | Restricciones |
|---|---|---|
| `Disenos` | Id (Guid), Nombre, Slug, Descripcion?, Estado (Borrador/Publicado/Archivado), ArchivoArteId? (FK), Version (rowversion), CreadoUtc, ActualizadoUtc | Nombre y Slug únicos (case-insensitive). |
| `ProductosBase` | Id, PrintfulProductoId (int), Nombre, Tipo | PrintfulProductoId único. |
| `Variantes` | Id, ProductoBaseId (FK), PrintfulVarianteId (int), Color, CodigoColor, Talla, CostoBaseUsd decimal(10,2), Activa | PrintfulVarianteId único. |
| `DisenosVariantes` | DisenoId, VarianteId | PK compuesta. |
| `ArchivosArte` | Id, Url, NombreBlob, TipoContenido, TamanoBytes, Ancho, Alto, HashSha256 char(64) (único), CreadoUtc | La llena HU-17 (la tabla se crea aquí). |

Migración: `AgregarDominioDisenos`. Todas las variantes de un diseño comparten `ProductoBaseId` (regla de dominio).
