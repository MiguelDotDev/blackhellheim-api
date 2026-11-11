# Especificación 002b: Casos de uso con CQRS, mediador y pipeline

**Rama de trabajo**: `sprint/01`

**Creada**: 2026-11-11

**Estado**: Plan cerrado

**Complementa**: `specs/002-estructura-base-api` (HU2 del backlog)

**Entrada**: Descripción de la usuaria:

> Como desarrolladora quiero casos de uso con CQRS, un mediador y comportamientos de
> pipeline (validación, registro de ejecución y manejo uniforme de errores) para que toda
> la lógica de negocio futura nazca con una forma común y probada.
>
> Debe cumplirse que:
> - Una consulta de ejemplo cruce API, Aplicación y Dominio sin violar la regla de
>   dependencias.
> - Una solicitud con datos inválidos sea rechazada por el comportamiento de validación
>   antes de llegar a su manejador, y el error salga en un formato uniforme.
> - Cada ejecución de caso de uso quede registrada.
> - Las pruebas de arquitectura existentes sigan pasando.
>
> Fuera de alcance: base de datos, OpenAPI con Scalar, transacciones, CI y despliegue.
> No decidas tecnologías aquí. Escribe todo en español y respeta la constitución.

## Escenarios de uso y pruebas *(obligatorio)*

### Historia de usuario 1 — Consulta de ejemplo de punta a punta (Prioridad: P1)

Como desarrolladora quiero una consulta de ejemplo de solo lectura que cruce API,
Aplicación y Dominio a través del mediador, para comprobar de extremo a extremo que el
mecanismo común de casos de uso funciona y que la regla de dependencias se mantiene.

**Por qué esta prioridad**: es la rebanada mínima que demuestra que el mediador, la capa
de Aplicación y la capa de Dominio colaboran sin que la API conozca los manejadores
concretos. Sin esta prueba, los demás comportamientos no tendrían una base sobre la que
actuar.

**Prueba independiente**: enviar la consulta de ejemplo a través de la API y verificar que
la respuesta es exitosa y que los datos provienen de Dominio; después, ejecutar las pruebas
de arquitectura existentes y confirmar que siguen en verde.

**Escenarios de aceptación**:

1. **Dada** la API en marcha y una consulta de ejemplo válida, **cuando** se envía,
   **entonces** el mediador la despacha, atraviesa la capa de Aplicación, obtiene los datos
   de la capa de Dominio y devuelve una respuesta exitosa.
2. **Dado** el código de la consulta de ejemplo, **cuando** se inspecciona el grafo de
   dependencias, **entonces** API depende de Aplicación, Aplicación depende solo de Dominio
   y Dominio no depende de ninguna otra capa.
3. **Dada** una solicitud de caso de uso, **cuando** se despacha, **entonces** la API no
   instancia ni referencia al manejador concreto.
4. **Dada** la consulta de ejemplo, **cuando** no existe información para el valor
   solicitado, **entonces** la respuesta es un resultado controlado (vacío o no encontrado),
   no un error no controlado.
5. **Dado** el endpoint de comprobación de estado existente, **cuando** se integra el nuevo
   código, **entonces** sigue funcionando y sus pruebas continúan pasando.

---

### Historia de usuario 2 — Validación antes del manejador (Prioridad: P2)

Como desarrolladora quiero que las solicitudes con datos inválidos sean rechazadas por el
comportamiento de validación antes de llegar a su manejador, para que ningún caso de uso se
ejecute con datos inválidos y para que el error tenga una forma uniforme.

**Por qué esta prioridad**: la validación protege a todos los casos de uso futuros y fija el
contrato de error que verán la API y el frontend. Es más importante que el registro porque
impide ejecutar lógica con datos incorrectos.

**Prueba independiente**: enviar una solicitud inválida y comprobar que el manejador no se
invoca (con un doble de prueba) y que la respuesta usa el formato uniforme de error con el
campo afectado identificado.

**Escenarios de aceptación**:

1. **Dada** una solicitud a la que le falta un campo obligatorio, **cuando** se despacha,
   **entonces** la validación la rechaza antes del manejador y devuelve un error uniforme
   que identifica el campo.
2. **Dada** una solicitud con un formato inválido, **cuando** se despacha, **entonces** la
   validación la rechaza antes del manejador y devuelve un error uniforme que identifica el
   campo.
3. **Dada** una solicitud con varios errores de validación, **cuando** se despacha,
   **entonces** todos los errores se informan juntos en la estructura uniforme.
4. **Dada** una solicitud válida, **cuando** se despacha, **entonces** la validación la deja
   pasar y el manejador se ejecuta con normalidad.
5. **Dada** una solicitud inválida, **cuando** se observa el manejador, **entonces** este
   nunca fue invocado.

---

### Historia de usuario 3 — Registro de cada ejecución (Prioridad: P3)

Como desarrolladora quiero que cada ejecución de un caso de uso quede registrada, para poder
rastrear qué se ejecutó, cuánto tardó y cómo terminó.

**Por qué esta prioridad**: la observabilidad es necesaria para diagnosticar los casos de uso
de negocio que se construirán después. Depende de que el despacho y la validación ya
funcionen.

**Prueba independiente**: ejecutar un caso de uso exitoso y uno fallido, y verificar que en
ambos casos queda un registro con el tipo de solicitud, el resultado, la duración y el
identificador de correlación.

**Escenarios de aceptación**:

1. **Dada** una ejecución exitosa, **cuando** termina, **entonces** el registro incluye el
   tipo de solicitud, el resultado exitoso, la duración y el identificador de correlación.
2. **Dada** una ejecución rechazada por validación, **cuando** termina, **entonces** el
   registro incluye el tipo de solicitud, el resultado fallido, la duración y el
   identificador de correlación.
3. **Dada** una ejecución que falla en el manejador, **cuando** termina, **entonces** el
   registro incluye el tipo de solicitud, el resultado fallido, la duración y el
   identificador de correlación.
4. **Dada** una solicitud sin identificador de correlación, **cuando** se despacha,
   **entonces** se genera uno y se incluye tanto en el registro como en la respuesta.
5. **Dada** una solicitud con datos sensibles, **cuando** se registra, **entonces** los
   valores sensibles no quedan escritos en el registro.
6. **Dado** un fallo del propio registro, **cuando** ocurre, **entonces** el resultado del
   caso de uso no se altera.

---

### Historia de usuario 4 — Manejo uniforme de errores (Prioridad: P4)

Como desarrolladora quiero que los errores no controlados de los manejadores se conviertan
en respuestas de error uniformes, para que la API no exponga detalles internos y para que
todas las fallas tengan la misma forma.

**Por qué esta prioridad**: completa el pipeline y evita que los casos de uso futuros filtren
detalles de implementación. Se apoya en la validación y el registro ya definidos.

**Prueba independiente**: forzar que un manejador lance un error no controlado y comprobar
que la respuesta es un error uniforme sin trazas ni detalles internos, y que el registro
guarda la falla con su identificador de correlación.

**Escenarios de aceptación**:

1. **Dado** un manejador que lanza un error no controlado, **cuando** se despacha,
   **entonces** el error se convierte al formato uniforme y la API devuelve una falla
   controlada.
2. **Dado** un error del manejador, **cuando** se inspecciona la respuesta, **entonces** no
   contiene trazas, rutas internas ni datos sensibles.
3. **Dado** un error del manejador, **cuando** se inspecciona el registro, **entonces** la
   falla queda registrada con el identificador de correlación y sin datos sensibles.
4. **Dado** un error de validación, **cuando** se devuelve, **entonces** usa la misma
   estructura uniforme que los errores del manejador.
5. **Dada** una operación cancelada, **cuando** ocurre, **entonces** se informa como una
   falla controlada, nunca como un éxito.

---

### Casos límite

- Una solicitud acumula varios errores de validación: se informan todos juntos.
- El manejador devuelve un resultado vacío o no encontrado: se responde de forma controlada,
  sin error no controlado.
- El manejador lanza una excepción de dominio esperada frente a una inesperada: ambas deben
  terminar en el formato uniforme, sin filtrar detalles internos.
- El propio comportamiento de validación falla: el fallo se convierte en error uniforme, no
  en una caída sin control.
- El registro falla: la ejecución del caso de uso no cambia su resultado.
- La solicitud no trae identificador de correlación: se genera uno y se propaga.
- La solicitud contiene datos sensibles: no se escriben en el registro.
- Dos solicitudes concurrentes: sus identificadores de correlación no se mezclan.
- Se agrega un proyecto o una capa nueva: las pruebas de arquitectura siguen cubriendo la
  regla de dependencias.
- El endpoint `/salud` y sus pruebas existentes siguen funcionando sin cambios.

## Requisitos *(obligatorio)*

### Requisitos funcionales

- **RF-001**: La capa de Aplicación DEBE distinguir consultas (lectura) de comandos
  (escritura) y despachar ambos mediante un mecanismo común, de modo que la API no dependa
  de los manejadores concretos.
- **RF-002**: Una consulta de ejemplo de solo lectura DEBE cruzar API, Aplicación y Dominio y
  devolver un resultado.
- **RF-003**: La regla de dependencias DEBE mantenerse: API depende de Aplicación, Aplicación
  depende solo de Dominio y Dominio no depende de ninguna otra capa.
- **RF-004**: Toda solicitud DEBE pasar por el comportamiento de validación antes de llegar a
  su manejador.
- **RF-005**: Una solicitud inválida DEBE rechazarse antes de ejecutar el manejador; el
  manejador NO DEBE invocarse.
- **RF-006**: Los errores de validación DEBEN devolverse en el formato uniforme de error e
  identificar el o los campos afectados.
- **RF-007**: Cada ejecución de un caso de uso DEBE producir un registro con el tipo de
  solicitud, el resultado, la duración y el identificador de correlación.
- **RF-008**: El registro NO DEBE contener datos sensibles.
- **RF-009**: Un error no controlado del manejador DEBE convertirse al formato uniforme de
  error, sin trazas, rutas internas ni datos sensibles.
- **RF-010**: Los errores de validación y los errores del manejador DEBEN usar la misma
  estructura uniforme de error.
- **RF-011**: La respuesta DEBE incluir el identificador de correlación que corresponde al
  registro de la ejecución.
- **RF-012**: Las pruebas de arquitectura existentes DEBEN seguir pasando.
- **RF-013**: Cada comportamiento nuevo DEBE tener pruebas unitarias de caso exitoso, caso de
  error y validación, conforme al principio III de la constitución.
- **RF-014**: La consulta de ejemplo DEBE funcionar sin base de datos ni proveedores externos.
- **RF-015**: El contrato de la consulta de ejemplo DEBE definirse antes de su implementación,
  conforme al principio II de la constitución; la infraestructura de documentación con Scalar
  queda fuera de alcance.
- **RF-016**: Esta historia NO DEBE incluir base de datos, transacciones, integración
  continua, despliegue, autenticación ni reglas de negocio del catálogo.
- **RF-017**: No se DEBE agregar ninguna dependencia con licencia comercial; la licencia de
  cualquier dependencia nueva DEBE verificarse en el plan, conforme al principio IV.

### Entidades clave

- **Solicitud de caso de uso**: petición con un tipo (consulta o comando), sus datos y un
  identificador de correlación.
- **Consulta de ejemplo**: solicitud de solo lectura con un criterio de búsqueda y un modelo
  de lectura como resultado.
- **Resultado de caso de uso**: éxito con datos o falla con el formato uniforme de error.
- **Error uniforme**: estructura común con código, mensaje, campos afectados e identificador
  de correlación.
- **Registro de ejecución**: anotación con tipo de solicitud, resultado, duración,
  identificador de correlación y momento de la ejecución.
- **Comportamiento de pipeline**: validación, registro y manejo de errores que rodean al
  manejador.
- **Manejador de caso de uso**: componente que ejecuta la lógica del caso de uso y que la API
  no referencia directamente.
- **Validador de solicitud**: reglas que determinan si una solicitud puede llegar al
  manejador.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: El 100 % de las solicitudes inválidas se rechazan antes del manejador; en las
  pruebas, el contador de invocaciones del manejador es 0.
- **CE-002**: La consulta de ejemplo devuelve una respuesta exitosa a través de la API con
  datos provenientes de Dominio, y las pruebas de arquitectura pasan.
- **CE-003**: El 100 % de las ejecuciones (éxito, rechazo por validación y falla del
  manejador) producen un registro con tipo de solicitud, resultado, duración e identificador
  de correlación.
- **CE-004**: El 100 % de los errores de validación y de los errores del manejador usan la
  misma estructura uniforme.
- **CE-005**: Ninguna respuesta expone trazas, rutas internas ni datos sensibles.
- **CE-006**: Las pruebas existentes, incluidas las de arquitectura, siguen en verde.
- **CE-007**: Cada comportamiento nuevo tiene al menos una prueba de éxito, una de error y una
  de validación.
- **CE-008**: La consulta de ejemplo funciona sin base de datos ni proveedores externos.

## Fuera de alcance

- Base de datos y persistencia.
- OpenAPI con Scalar (la infraestructura completa de documentación); solo se define el
  contrato mínimo de la consulta de ejemplo si el plan lo requiere.
- Transacciones.
- Integración continua.
- Despliegue.
- Autenticación y autorización.
- Reglas de negocio del catálogo, pedidos, pagos, envíos y demás historias posteriores.
- Caché, mensajería, reintentos y tareas en segundo plano.
- Proveedores externos.

## Supuestos

- La estructura en capas de `specs/002-estructura-base-api` existe y es la base de esta
  historia.
- Las pruebas de arquitectura existentes son la garantía de la regla de dependencias.
- La consulta de ejemplo es de solo lectura y no requiere persistencia; su fuente de datos se
  decide en el plan.
- El formato uniforme de error es el que ya adopta el proyecto conforme a la constitución.
- El mediador y los comportamientos de pipeline viven en la capa de Aplicación; la API solo
  conoce el mecanismo de despacho.
- El tema exacto de la consulta de ejemplo y sus reglas de validación son ilustrativos y se
  detallan en el plan; no representan reglas del catálogo ni del negocio.
- El trabajo se realiza en la rama del sprint `sprint/01`, conforme a `AGENTS.md`; no se crea
  una rama por historia.
- Esta especificación complementa la HU2 y no reemplaza `specs/002-estructura-base-api`.
- La numeración `002b` responde a la solicitud explícita de la usuaria; la HU2 del backlog
  sigue siendo la historia padre.
- No se agregan dependencias con licencia comercial; la licencia de cualquier dependencia
  nueva se verifica en el plan.
- El endpoint `/salud` y sus pruebas actuales se mantienen sin cambios.
