# Plan 022: Webhooks de Printful

**Spec**: [spec.md](spec.md) · **Constitución**: I, II, III, VI, VII, VIII. **Dependencias nuevas**: ninguna (`System.Security.Cryptography`).

## Resumen
Endpoint público que lee el cuerpo crudo, verifica la firma HMAC-SHA256 con tiempo constante, inserta el evento en `EventosWebhook` (índice único proveedor+id) y lo despacha a los `IManejadorEventoProveedor` registrados para su tipo. La unicidad en base de datos resuelve la concurrencia.

## Decisiones
1. Puerto `IManejadorEventoProveedor { string Proveedor; IReadOnlyCollection<string> Tipos; Task ManejarAsync(EventoRecibido, ct) }` en Aplicación; HU-21 y HU-32 solo agregan implementaciones. HU-29 reutiliza la tabla para Stripe.
2. Procesamiento síncrono dentro de la petición (eventos pequeños); un consumidor que falla deja el evento `Fallido` y el endpoint responde `500` para que el proveedor reintente; al reintento se reprocesan solo los eventos `Recibido`/`Fallido`.
3. Cabecera y formato de firma configurables (`Printful:CabeceraFirma`, por omisión `X-Printful-Signature`); secreto en `Printful__SecretoWebhook`, nunca en el repo.
4. Límite de cuerpo 1 MB.
5. Sin secreto configurado el endpoint responde 503.

## Estructura
`Dominio/Webhooks/EventoWebhook`, `Aplicacion/Webhooks/{IManejadorEventoProveedor,RecibirWebhook}`, `Infraestructura/Webhooks/{VerificadorFirmaHmac,RepositorioEventos}`, `Api/Endpoints/WebhooksEndpoints.cs`. Migración `AgregarEventosWebhook`.

## Pruebas
Verificador (válida, alterada, ausente, mal formada, tiempo constante), manejador con repositorio doble (primera vez, repetido, consumidor que falla, sin consumidor), integración con SQL Server (20 entregas paralelas, CE-002) y endpoint (200/400/401/413/503), contrato.

## Bloqueos y [MANUAL]
HU-15 integrada. `[MANUAL]` secreto y registro del webhook en Printful con URL pública (HU-25/HU-40); confirmar formato de firma real (T002).
