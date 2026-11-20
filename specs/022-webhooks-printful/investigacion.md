# Investigación 022 (T002): formato de firma de Printful

Contrastado con la documentación v2 de Printful (sin llamadas reales). **Sin verificar: solo el nombre de la cabecera de firma.**

## Formato del evento (documentación v2)
Cada evento trae `type`, `occurred_at`, `retries`, `store_id` y `data`. **No hay `id` en la raíz.**

## Implementado
- **Id de idempotencia** (derivado, determinista): `type|id|occurred_at`, donde `id` es el de `data.mockup_task`, `data.task` o `data` (el primero presente). Sin id en `data` (o si excede 100 caracteres): `sha256:` + SHA-256 del cuerpo crudo. Un reintento con el mismo contenido produce el mismo id; `retries` no interviene en el caso con id. Límite del respaldo por hash: si cambia `retries` en el cuerpo, cambia el hash. Índice único (`Proveedor`, `IdExterno`) conservado.
- **Firma**: HMAC-SHA256 del cuerpo crudo, hexadecimal (64 caracteres), comparada con `CryptographicOperations.FixedTimeEquals`.
- **Secreto**: `Printful__SecretoWebhook` es la cadena hexadecimal del `secret_key` del webhook; se decodifica a bytes antes del HMAC. Si no es hexadecimal válido → `503` y error de configuración en el log (sin el valor).
- **Cabecera**: `X-Printful-Signature` por defecto, configurable con `Printful__CabeceraFirma`. **SIN VERIFICAR.** Cada entrega registra (información) los nombres de todas las cabeceras recibidas y la usada para la firma, para confirmarla con una entrega real.
- Respuestas: `401` firma inválida, `503` sin secreto o secreto no hexadecimal, `413` cuerpo > 1 MB, `400` cuerpo no JSON objeto o sin `type`.

## Pendiente `[MANUAL]` (T003)
1. Confirmar con una entrega real el nombre de la cabecera de firma (revisar el log).
2. Registrar el webhook requiere URL pública HTTPS (HU-25/HU-40, no implementadas).
