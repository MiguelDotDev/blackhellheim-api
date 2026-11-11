# Plan 016: Dominio de diseño, producto base y variante

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, V, X.

## Resumen
Entidades en Dominio con invariantes; configuración EF y migración `AgregarDominioDisenos` en Infraestructura; casos de uso Mediator y endpoints de administración. Dependencias nuevas: ninguna.

## Decisiones
1. El alta recibe los datos de variante (copiados del catálogo de la HU-15 por la web) y hace «upsert» de `ProductoBase`/`Variante` por id de Printful. No llama a Printful (evita acoplar el guardado a un tercero); la veracidad se re-valida en una mejora futura.
2. `Version` = `rowversion` expuesto como ETag (`If-Match`); mismatch → 412.
3. Slug: minúsculas, sin acentos, guiones; colisión → 409.
4. `Disenos.ArchivoArteId` nullable hasta publicar; la tabla `ArchivosArte` se crea aquí y la usa HU-17 sin migración adicional.
5. Términos de terceros: lista en configuración (`Diseños__TerminosBloqueados`), aviso no bloqueante (la decisión final es humana, constitución X).
6. Sin precio en tablas (principio V).

## Estructura
`Dominio/Disenos/{Diseno,ProductoBase,Variante,ArchivoArte,EstadoDiseno}`, `Aplicacion/Disenos/{Comandos,Consultas,IRepositorioDisenos}`, `Infraestructura/Persistencia/Configuraciones/*`, `Api/Endpoints/DisenosAdminEndpoints.cs`.

## Pruebas
Dominio (invariantes, publicación, mismo producto base), manejadores con repositorio doble, persistencia con Testcontainers (migración aplica, índices únicos, concurrencia), endpoints y contrato.

## Bloqueos
HU-13 (roles) y coordinación de ids con HU-15; HU-17 llena `ArchivosArte`.
