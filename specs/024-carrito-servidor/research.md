# Investigación 024 (Fase 0)

| Tema | Decisión | Motivo / alternativas |
|---|---|---|
| Identificar la variante | Por `varianteId` (Guid de `Variante`). El catálogo (HU-19) hoy no lo expone: se agrega `varianteId` a `VarianteCatalogoDto` y a su contrato (cambio compatible) | Sin id estable la web no puede decir qué agrega. Alternativa (diseño+color+talla) descartada: frágil y más validación. |
| Fusión | Suma con tope 10, líneas inválidas omitidas e informadas, no idempotente | Es lo que pide el criterio («se fusiona»); la web vacía su copia local. Alternativa «gana el servidor» pierde lo que la clienta eligió como visitante. |
| Precios | `ICalculadoraPrecios.PrepararAsync` una vez por solicitud; sin tipo de cambio → 503 | Principio V y comportamiento de HU-19/20. |
| Concurrencia | Última escritura gana por línea; índice único (CarritoId, VarianteId) y upsert condicionado | Dos pestañas no deben duplicar líneas. |
| Límites | 20 líneas, 10 unidades, en `Carrito__MaxLineas` y `Carrito__MaxCantidad` validados al arrancar | Supuestos de la spec, ajustables. |
| Dependencias | Ninguna nueva | Principio IV. |
