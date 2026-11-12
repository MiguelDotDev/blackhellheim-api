# Especificación 004: Contrato OpenAPI publicado y documentación interactiva

**Rama de trabajo**: `sprint/01`

**Creada**: 2026-11-11

**Estado**: Hecha

**Historia del backlog**: HU4 (componente BE)

**Entrada**: Descripción de la usuaria:

> Crea la carpeta specs/004-openapi-scalar (HU4 del backlog).
>
> Como frontend quiero el contrato OpenAPI del API publicado y una página de documentación
> interactiva, y que todos los errores del API salgan en un formato uniforme, para generar
> el cliente tipado y levantar un mock sin esperar al backend.
>
> Debe cumplirse que:
> - El documento OpenAPI esté disponible en una ruta pública del API y coincida con el
>   contrato definido primero en los archivos de contratos (contrato primero).
> - Exista una página de documentación interactiva con Scalar.
> - Un error de validación y un error inesperado devuelvan el mismo formato de problema
>   (ProblemDetails).
> - El contrato siga cubriendo el endpoint /salud ya existente.
>
> Fuera de alcance: autenticación, nuevos endpoints de negocio, CI y despliegue.
> No decidas tecnologías aquí. Escribe todo en español y respeta la constitución.

## Escenarios de uso y pruebas *(obligatorio)*

### Historia de usuario 1 — Contrato OpenAPI publicado y fiel a los contratos (Prioridad: P1)

Como integrante del frontend quiero descargar el contrato OpenAPI desde una ruta pública
del API, para generar un cliente tipado y levantar un mock sin esperar a que el backend
termine cada endpoint.

**Por qué esta prioridad**: sin un contrato publicado y confiable, el frontend no puede
trabajar en paralelo ni generar código; es la base de las otras dos historias y del
principio de contrato primero.

**Prueba independiente**: solicitar la ruta pública del documento sin credenciales y
comprobar que responde un documento OpenAPI válido que describe los endpoints
implementados, incluido `/salud`, y que una comparación con los archivos de contratos no
encuentra diferencias.

**Escenarios de aceptación**:

1. **Dado** el API en marcha, **cuando** se solicita la ruta pública del documento,
   **entonces** responde con un documento OpenAPI válido y sin referencias rotas.
2. **Dado** el documento publicado, **cuando** se compara con los archivos de contratos del
   repositorio, **entonces** las operaciones, parámetros, respuestas y esquemas coinciden.
3. **Dado** el endpoint `/salud` existente, **cuando** se revisa el documento publicado,
   **entonces** aparece con su respuesta correcta y sin cambios en su comportamiento.
4. **Dado** un endpoint implementado cuya definición no está en los contratos, **cuando** se
   ejecuta la verificación de contrato, **entonces** se informa la diferencia y no se
   publica como si estuviera acordado.
5. **Dado** un contrato versionado sin implementación todavía, **cuando** se revisa el
   documento publicado, **entonces** la diferencia se informa de forma explícita y no se
   oculta.
6. **Dado** un cambio posterior en un contrato, **cuando** se vuelve a solicitar el
   documento, **entonces** refleja la versión vigente y no una copia obsoleta.

---

### Historia de usuario 2 — Página de documentación interactiva (Prioridad: P2)

Como integrante del frontend quiero una página de documentación interactiva que muestre el
contrato y permita probar las operaciones, para entender y validar el API sin leer archivos
ni usar herramientas externas.

**Por qué esta prioridad**: el contrato publicado ya entrega valor para generar clientes,
pero la página interactiva reduce el tiempo de comprensión y permite probar respuestas
reales desde el navegador.

**Prueba independiente**: abrir la página pública de documentación en un navegador,
comprobar que lista todas las operaciones del documento y ejecutar una operación de solo
lectura, verificando que muestra la respuesta real del API.

**Escenarios de aceptación**:

1. **Dado** el API en marcha, **cuando** se abre la ruta pública de documentación en un
   navegador, **entonces** se muestra la página interactiva con el título y la versión del
   API.
2. **Dada** la página abierta, **cuando** se revisa el listado, **entonces** aparecen todas
   las operaciones del documento publicado, incluido `/salud`.
3. **Dada** una operación de solo lectura, **cuando** se ejecuta desde la página,
   **entonces** se envía la petición real y se muestra el código y el cuerpo de la respuesta.
4. **Dado** un documento que no se puede cargar, **cuando** se abre la página, **entonces**
   muestra un mensaje claro en lugar de una página en blanco.
5. **Dado** un cambio en el contrato, **cuando** se recarga la página, **entonces** muestra
   las operaciones actualizadas sin pasos manuales.

---

### Historia de usuario 3 — Errores uniformes como problema (Prioridad: P3)

Como integrante del frontend quiero que cualquier error del API, sea de validación o
inesperado, llegue en el mismo formato de problema, para manejarlo con un solo componente y
sin adivinar la forma de cada respuesta.

**Por qué esta prioridad**: la uniformidad de errores evita código defensivo en el cliente y
reduce el riesgo de mostrar detalles internos; depende de que el contrato ya esté publicado
para documentar las respuestas de error.

**Prueba independiente**: provocar un error de validación y un error inesperado, y comparar
ambas respuestas: mismo tipo de contenido, mismos campos obligatorios y mismo identificador
de correlación, sin detalles internos.

**Escenarios de aceptación**:

1. **Dado** un error de validación, **cuando** el API responde, **entonces** usa el formato
   de problema con tipo, título, estado, código estable, errores por campo e identificador
   de correlación.
2. **Dado** un error inesperado, **cuando** el API responde, **entonces** usa el mismo
   formato de problema, con un código estable y el identificador de correlación.
3. **Dado** cualquiera de los dos errores, **cuando** se revisa la respuesta, **entonces** el
   tipo de contenido es el mismo y no aparecen trazas, rutas internas, cadenas de conexión
   ni secretos.
4. **Dado** un error de validación con varios campos inválidos, **cuando** el API responde,
   **entonces** se informan todos los campos en la misma respuesta.
5. **Dado** un identificador de correlación enviado por el cliente, **cuando** ocurre un
   error, **entonces** la respuesta lo conserva y lo devuelve en la cabecera y en el cuerpo.
6. **Dado** el documento publicado, **cuando** se revisan las operaciones, **entonces** cada
   una declara sus respuestas de error con el mismo esquema de problema.

---

### Historia de usuario 4 — Contrato primero verificable (Prioridad: P4)

Como equipo queremos que el contrato versionado sea la fuente de verdad y que cualquier
desviación se detecte antes de publicar, para que el frontend nunca genere clientes contra
un contrato que el API no cumple.

**Por qué esta prioridad**: la publicación del documento y la página ya entregan valor, pero
sin una verificación la deriva entre contrato y API reaparece con cada cambio.

**Prueba independiente**: modificar temporalmente un contrato o una operación y comprobar
que la verificación falla con un mensaje que señala la diferencia; después restaurar y
comprobar que vuelve a pasar.

**Escenarios de aceptación**:

1. **Dado** el repositorio en su estado normal, **cuando** se ejecuta la verificación de
   contrato, **entonces** pasa sin diferencias.
2. **Dado** un contrato modificado sin cambiar la implementación, **cuando** se ejecuta la
   verificación, **entonces** falla e indica qué operación o esquema difiere.
3. **Dada** una implementación modificada sin cambiar el contrato, **cuando** se ejecuta la
   verificación, **entonces** falla e indica la diferencia.
4. **Dado** un fallo de verificación, **cuando** se corrige el contrato o la implementación,
   **entonces** la verificación vuelve a pasar sin pasos manuales adicionales.

---

### Casos límite

- El documento publicado se solicita con una extensión o un formato no soportado: la
  respuesta es controlada y no rompe la página de documentación.
- La ruta del documento y la de la página de documentación entran en conflicto con una ruta
  existente: se detecta y se resuelve antes de publicar, sin sombrear `/salud`.
- El contrato declara un esquema reutilizado y una referencia queda rota: la validación del
  documento falla y señala la referencia.
- Un error ocurre antes de que exista un identificador de correlación: la respuesta sigue
  siendo un problema uniforme y se genera un identificador.
- Un error inesperado ocurre dentro del manejo de errores: el cliente recibe un problema
  genérico sin detalles internos.
- El entorno es de desarrollo o de producción: en ninguno se exponen trazas ni detalles
  internos.
- El documento incluye operaciones de historias aún no implementadas: la diferencia se
  informa y no se publica como si estuviera disponible.
- La página de documentación se abre con el API detenido: muestra un error claro de carga.
- Dos versiones del documento conviven durante una transición: la ruta pública apunta a una
  sola versión vigente y lo indica.
- Un cliente envía una cabecera de correlación inválida: se genera un identificador válido y
  se devuelve en la respuesta.
- El documento se solicita muchas veces seguidas: la respuesta es consistente y no cambia
  entre solicitudes.
- El contrato incluye ejemplos con datos sensibles: la verificación los rechaza o los
  sustituye por marcadores.

## Requisitos *(obligatorio)*

### Requisitos funcionales

- **RF-001**: El API DEBE exponer el documento OpenAPI en una ruta pública y estable, sin
  autenticación, cuya ruta exacta se documenta en el contrato de esta historia.
- **RF-002**: El documento publicado DEBE ser un OpenAPI válido, sin referencias rotas y
  utilizable por herramientas estándar de generación de clientes y de mocks.
- **RF-003**: El documento publicado DEBE describir todas las operaciones implementadas en
  el momento de la consulta, incluido `/salud` con su respuesta vigente.
- **RF-004**: El documento publicado DEBE coincidir con los archivos de contratos
  versionados; cualquier diferencia DEBE detectarse e informarse.
- **RF-005**: Los archivos de contratos DEBEN seguir siendo la fuente de verdad: ningún
  endpoint se implementa antes de estar definido en ellos.
- **RF-006**: DEBE existir una verificación automática que compare el documento publicado
  con los contratos y falle señalando la diferencia.
- **RF-007**: DEBE existir una página pública de documentación interactiva que muestre el
  documento publicado y permita ejecutar sus operaciones.
- **RF-008**: La página de documentación DEBE mostrar el título, la versión y todas las
  operaciones del documento, sin pasos manuales de configuración.
- **RF-009**: Si el documento no se puede cargar, la página DEBE mostrar un mensaje claro en
  lugar de una página vacía.
- **RF-010**: Todo error de validación DEBE devolver el formato uniforme de problema con
  tipo, título, estado, código estable, errores por campo e identificador de correlación.
- **RF-011**: Todo error inesperado DEBE devolver el mismo formato uniforme de problema, con
  código estable e identificador de correlación.
- **RF-012**: Ambos tipos de error DEBEN usar el mismo tipo de contenido y el mismo esquema
  documentado.
- **RF-013**: Las respuestas de error NO DEBEN incluir trazas, rutas internas, cadenas de
  conexión, secretos ni valores de configuración.
- **RF-014**: El identificador de correlación DEBE devolverse en la cabecera y en el cuerpo
  de la respuesta de error, conservando el enviado por el cliente cuando sea válido.
- **RF-015**: Cada operación del documento DEBE declarar sus respuestas de error con el
  esquema uniforme de problema.
- **RF-016**: El endpoint `/salud` DEBE seguir funcionando igual y aparecer documentado.
- **RF-017**: Esta historia NO DEBE incluir autenticación, autorización, nuevos endpoints de
  negocio, base de datos, almacenamiento de archivos, CI ni despliegue.
- **RF-018**: No se DEBE agregar ninguna dependencia con licencia comercial; la licencia de
  cualquier dependencia nueva DEBE verificarse en el plan.
- **RF-019**: Las pruebas existentes, incluidas las de arquitectura y las de salud, DEBEN
  seguir pasando.
- **RF-020**: La página de documentación y el documento DEBEN ser accesibles en el entorno
  local sin configuración adicional más allá de arrancar el API.

### Entidades clave

- **Documento OpenAPI**: descripción legible por máquina de las operaciones, parámetros,
  respuestas y esquemas del API, publicada en una ruta pública.
- **Contrato versionado**: archivo de contrato del repositorio que define primero cada
  endpoint y es la fuente de verdad.
- **Operación documentada**: cada combinación de ruta y método descrita en el documento, con
  sus respuestas de éxito y de error.
- **Página de documentación interactiva**: interfaz web que presenta el documento y permite
  ejecutar operaciones.
- **Problema uniforme**: estructura común de error con tipo, título, estado, código, detalle,
  campos e identificador de correlación.
- **Identificador de correlación**: valor que permite seguir una petición en registros y en
  la respuesta de error.
- **Diferencia de contrato**: discrepancia detectada entre el documento publicado y los
  contratos versionados.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: El 100 % de las operaciones implementadas aparece en el documento publicado y
  la verificación de contrato reporta cero diferencias.
- **CE-002**: El documento valida contra la especificación OpenAPI con cero errores y cero
  referencias sin resolver.
- **CE-003**: Una herramienta estándar de generación de clientes produce un cliente tipado
  que compila sin ajustes manuales a partir del documento publicado.
- **CE-004**: La página de documentación muestra todas las operaciones en menos de 3 segundos
  en el entorno local y permite ejecutar al menos una operación de solo lectura.
- **CE-005**: El 100 % de los errores de validación y de los errores inesperados usa el
  mismo formato de problema y el mismo tipo de contenido.
- **CE-006**: El 0 % de las respuestas de error contiene trazas, rutas internas, secretos o
  valores de configuración.
- **CE-007**: El 100 % de las respuestas de error incluye un identificador de correlación en
  la cabecera y en el cuerpo.
- **CE-008**: El endpoint `/salud` sigue respondiendo igual y aparece documentado.
- **CE-009**: Las pruebas existentes siguen en verde y la compilación no agrega advertencias.

## Fuera de alcance

- Autenticación y autorización.
- Nuevos endpoints de negocio, incluidos catálogo, pedidos, pagos, envíos y códigos de
  creadora.
- Base de datos, migraciones y datos semilla (historia 003).
- Almacenamiento de archivos y Azurite.
- Integración continua, análisis estático y despliegue (historias 005 y 006).
- Servidor mock alojado en el repositorio; el contrato publicado debe permitir levantar uno
  con herramientas estándar, pero no se incluye su operación.
- Publicación del contrato en un portal externo o en el repositorio del frontend.
- Versionado avanzado de la API, compatibilidad hacia atrás y deprecación de operaciones.
- Documentación de proveedores externos y de flujos internos.

## Supuestos

- Scalar es la herramienta de documentación interactiva porque el backlog y la historia la
  nombran; el plan concreta el paquete, la versión y su licencia.
- Los archivos de `specs/NNN-*/contratos/openapi.yaml` son la fuente de verdad y el plan
  decide cómo se publica el documento: generación desde el código, composición de los
  contratos o comparación entre ambos.
- El endpoint `/salud` y la estructura en capas de la historia 002 existen. La correlación y
  el formato uniforme de errores de la historia 002b son la base; si 002b aún no está
  implementada, esta historia los incorpora y 002b los reutiliza, sin duplicarlos.
- El API se ejecuta en local con `dotnet run` y sin base de datos obligatoria.
- La ruta pública del documento y la de la página se fijan en el plan y se documentan en el
  contrato de esta historia; el backlog espera que la página esté en `/scalar`.
- El frontend consume el documento con herramientas estándar de generación de clientes y de
  mocks; no se agrega un cliente generado al repositorio.
- El documento publicado describe solo las operaciones implementadas; las historias futuras
  agregan sus operaciones cuando se implementen.
- El trabajo se realiza en la rama `sprint/01`, conforme a `AGENTS.md`; no se crea una rama
  por historia.
- No se agregan dependencias con licencia comercial; la licencia de cualquier dependencia
  nueva se verifica en el plan.
