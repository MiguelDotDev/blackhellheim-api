# Investigación 022 (T002): formato de firma de Printful

**Estado: NO verificado contra Printful real.** No se hicieron llamadas externas (regla de la sesión). Se implementó la suposición de la spec y queda `[MANUAL]` confirmarla.

## Implementado (suposiciones)
- Firma: HMAC-SHA256 del **cuerpo crudo** (bytes recibidos), en hexadecimal (64 caracteres, mayúsculas o minúsculas), comparada con `CryptographicOperations.FixedTimeEquals`.
- Cabecera: `X-Printful-Signature`, configurable con `Printful__CabeceraFirma`. Secreto: `Printful__SecretoWebhook`.
- Identificador de evento: propiedad `id` de la raíz del JSON (cadena o número); tipo: propiedad `type`.
- Sin marca de tiempo: no se valida la ventana de 5 min (la spec la condiciona a que Printful la envíe).

## Riesgos a confirmar `[MANUAL]` (T003)
1. La documentación pública de webhooks de Printful (v1) describe el cuerpo como `{type, created, retries, store, data}` y **no** garantiza un `id` de evento ni una firma HMAC propia (v1 se protege con URL secreta). Si es así, hay que decidir el id (p. ej. derivarlo de `type` + identificador dentro de `data`) y el esquema de autenticación, y actualizar spec y contrato (HU-22 es dueña del contrato).
2. Hasta confirmarlo, un evento real sin `id` recibiría `400`.
3. Registrar el webhook en Printful requiere URL pública HTTPS (HU-25/HU-40, no implementadas).
