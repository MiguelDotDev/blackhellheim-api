# Plan 021: Mockups asíncronos

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, VI, VII. **Dependencias nuevas**: ninguna.

## Decisiones
1. Entidad `TareaMockup` (Pendiente/Completada/Fallida) e `ImagenMockup`; migración `AgregarMockups`.
2. `IProveedorImpresion` gana `CrearTareaMockupAsync` y `ConsultarTareaMockupAsync` (adaptador Printful + doble).
3. Caso de uso único `FinalizarTareaMockup`, llamado por el manejador del evento (HU-22) y por el `BackgroundService` de polling; concurrencia resuelta con actualización condicional de estado (`Pendiente`→`Completada`).
4. Descarga con `HttpClient` y límites (20 MB, tipos permitidos), subida con `IAlmacenArchivos` (HU-17).
5. Opciones `Mockups__IntervaloPollingSegundos` (30), `EsperaMinimaSegundos` (120), `TiempoMaximoMinutos` (30).

## Estructura
`Dominio/Mockups`, `Aplicacion/Mockups`, `Infraestructura/Mockups` (repositorio, polling), `Api/Endpoints/MockupsAdminEndpoints.cs`.

## Pruebas
Casos de uso con dobles (éxito, repetido, falla de descarga, tiempo agotado, concurrencia), polling con reloj falso, endpoints, SQL Server para la concurrencia, contrato.

## Bloqueos y [MANUAL]
HU-15, 16, 17, 22. `[MANUAL]` verificación con Printful real.
