# Investigación 028 (Fase 0)
| Tema | Decisión | Motivo / alternativas |
|---|---|---|
| Idempotencia | Cabecera `Idempotency-Key` (1 a 64 caracteres) obligatoria; índice único (UsuarioId, ClaveIdempotencia) y huella SHA-256 del cuerpo normalizado | Reintentos de red no duplican pedidos; misma clave con otro cuerpo → 409. Alternativa: clave en el cuerpo, descartada por convención HTTP. |
| Montos | El servidor calcula con `ICalculadoraPrecios` y recotiza el envío con el puerto de HU-26 (método elegido por `id`); si el método ya no existe → 400 | El cliente no confía su precio (principio V). Alternativa: aceptar el costo de envío del cliente, descartada. |
| Congelación | `Pagado` hace inmutables líneas, envío y total (método de dominio que lo impide) | Principio V: el precio se congela al confirmarse el pago. |
| Outbox | Tabla `MensajesSalida` (Tipo, Carga JSON, CreadoUtc, ProcesadoUtc?, Intentos, Error?) escrita con `SaveChanges` en la misma transacción | Constitución VI. Despachador en cada consumidor (HU-30 el primero), con reclamo condicional como en HU-22. |
| Estados | `PendientePago, Pagado, EnProduccion, Enviado, Entregado, Cancelado, Reembolsado` como texto en base de datos | Legibles y estables; transiciones en el dominio. |
| Concurrencia | Violación del índice único → releer y devolver el existente | Mismo patrón que HU-22 y HU-17. |
| Dependencias | Ninguna nueva | Principio IV. |
