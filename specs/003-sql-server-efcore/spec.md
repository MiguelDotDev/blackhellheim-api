# Especificación 003: Base de datos local con migraciones y datos semilla

**Rama de trabajo**: `sprint/01`

**Creada**: 2026-11-11

**Estado**: Borrador

**Historia del backlog**: HU3 (componentes BE y DB)

**Entrada**: Descripción de la usuaria:

> Crea la carpeta specs/003-sql-server-efcore (HU3 del backlog).
>
> Como desarrolladora quiero una base de datos SQL Server local que se levante con un solo
> comando, con el esquema gestionado por migraciones y datos semilla mínimos, para trabajar
> el resto de historias con persistencia desde el primer día.
>
> Debe cumplirse que:
> - Con un solo comando se cree la base y se aplique la migración inicial con sus datos semilla.
> - Las migraciones se puedan aplicar y revertir.
> - El API se conecte a la base sin ninguna credencial escrita en el repositorio.
> - La comprobación de salud siga funcionando sin la base, o indique su estado de forma separada.
>
> Fuera de alcance: Dockerfile del API, almacenamiento de archivos, OpenAPI y despliegue.
> No decidas tecnologías aquí. Escribe todo en español y respeta la constitución.

## Escenarios de uso y pruebas *(obligatorio)*

### Historia de usuario 1 — Base local lista con un solo comando (Prioridad: P1)

Como desarrolladora quiero levantar la base de datos local, aplicar la migración inicial y
cargar los datos semilla con un solo comando, para empezar a trabajar con persistencia sin
pasos manuales ni conocimiento previo del esquema.

**Por qué esta prioridad**: es la base de todas las historias con persistencia. Sin la base
creada, migrada y sembrada, las demás historias no pueden guardar ni consultar datos.

**Prueba independiente**: en un entorno limpio, ejecutar el comando único y comprobar que la
base existe, que la migración inicial está registrada como aplicada y que los datos semilla
están presentes; el valor se entrega aunque no exista todavía ninguna consulta de negocio.

**Escenarios de aceptación**:

1. **Dado** un entorno limpio sin la base creada, **cuando** se ejecuta el comando único
   documentado, **entonces** la base se crea, se aplica la migración inicial y quedan los
   datos semilla.
2. **Dado** un entorno donde la base ya existe y está migrada, **cuando** se ejecuta el mismo
   comando otra vez, **entonces** termina sin error y no duplica los datos semilla.
3. **Dado** el comando en ejecución, **cuando** termina, **entonces** informa de forma clara
   el resultado y deja la base lista para recibir conexiones.
4. **Dada** una base ya sembrada, **cuando** se reinicia el contenedor o el servicio local,
   **entonces** los datos persisten.

---

### Historia de usuario 2 — Migraciones aplicables y reversibles (Prioridad: P2)

Como desarrolladora quiero aplicar y revertir las migraciones del esquema, para evolucionar
la base con seguridad y volver atrás cuando un cambio no funcione.

**Por qué esta prioridad**: sin reversibilidad, un cambio de esquema defectuoso obliga a
borrar la base y perder el trabajo. Es la garantía de que el esquema se gestiona de forma
ordenada desde el primer día.

**Prueba independiente**: aplicar la migración inicial, revertirla y comprobar que el
esquema vuelve al estado anterior; después, volver a aplicarla y comprobar que el esquema y
los datos semilla quedan otra vez completos.

**Escenarios de aceptación**:

1. **Dada** una base vacía, **cuando** se aplica la migración inicial, **entonces** el
   esquema queda creado y la migración figura como aplicada.
2. **Dada** una base con la migración inicial aplicada, **cuando** se revierte, **entonces**
   el esquema vuelve al estado anterior sin dejar objetos a medias.
3. **Dada** una base revertida, **cuando** se vuelve a aplicar la migración, **entonces** el
   esquema y los datos semilla quedan restaurados.
4. **Dada** una migración nueva agregada al repositorio, **cuando** se aplica, **entonces**
   solo se ejecutan las migraciones pendientes.
5. **Dada** una migración que falla a mitad de camino, **cuando** ocurre, **entonces** el
   error se informa y la base no queda marcada como migrada.

---

### Historia de usuario 3 — Conexión del API sin credenciales en el repositorio (Prioridad: P3)

Como desarrolladora quiero que el API se conecte a la base con credenciales que vivan fuera
del control de versiones, para no exponer secretos en un repositorio público.

**Por qué esta prioridad**: una credencial filtrada compromete la base y obliga a rotarla.
La conexión segura es condición para usar la base en cualquier entorno.

**Prueba independiente**: buscar credenciales en los archivos versionados y confirmar que no
hay ninguna; después, proporcionar la configuración por el mecanismo externo definido y
comprobar que el API arranca y se conecta; finalmente, omitir la configuración y comprobar
que el API falla con un mensaje claro sin imprimir el secreto.

**Escenarios de aceptación**:

1. **Dado** el repositorio clonado, **cuando** se buscan contraseñas, cadenas de conexión con
   credenciales o claves, **entonces** no hay ninguna en archivos versionados.
2. **Dada** la configuración externa completa, **cuando** el API arranca, **entonces** se
   conecta a la base local correctamente.
3. **Dada** la configuración externa incompleta, **cuando** el API arranca, **entonces**
   falla con un mensaje que indica qué falta, sin mostrar el valor del secreto.
4. **Dado** el archivo de ejemplo de configuración, **cuando** se inspecciona, **entonces**
   solo contiene marcadores de posición, nunca credenciales reales.
5. **Dado** un cambio de credencial en el entorno, **cuando** se reinicia el API, **entonces**
   toma la nueva credencial sin cambios en el código.

---

### Historia de usuario 4 — Salud independiente de la base (Prioridad: P4)

Como desarrolladora quiero que la comprobación de salud del API siga respondiendo aunque la
base no esté disponible, y que el estado de la base se informe por separado, para distinguir
si el API está vivo de si puede atender operaciones que dependen de la base.

**Por qué esta prioridad**: si la salud del API dependiera de la base, una caída de la base
haría parecer caído al API y dificultaría el diagnóstico. La separación permite reiniciar o
reparar cada pieza sin confundirlas.

**Prueba independiente**: con la base detenida, llamar a la comprobación de salud existente y
comprobar que responde correctamente; llamar a la comprobación de la base y comprobar que
informa «no disponible». Después, levantar la base y comprobar que la comprobación de la base
informa «disponible».

**Escenarios de aceptación**:

1. **Dada** la base detenida, **cuando** se consulta la comprobación de salud existente,
   **entonces** responde que el API está vivo.
2. **Dada** la base detenida, **cuando** se consulta el estado de la base, **entonces**
   informa «no disponible» sin exponer credenciales ni detalles internos.
3. **Dada** la base en marcha, **cuando** se consulta el estado de la base, **entonces**
   informa «disponible».
4. **Dada** una base que no responde, **cuando** se consulta su estado, **entonces** la
   respuesta llega dentro de un tiempo máximo acotado y no deja la petición colgada.
5. **Dado** el API recién arrancado con la base aún iniciando, **cuando** se consulta el
   estado, **entonces** el resultado es controlado y no una caída del API.

---

### Casos límite

- El puerto de la base ya está ocupado por otro proceso: el comando falla con un mensaje
  claro y no deja un contenedor a medias.
- El volumen local contiene una base con un esquema viejo: el comando aplica las migraciones
  pendientes o informa de la incompatibilidad sin borrar datos por su cuenta.
- La base tarda en aceptar conexiones: el comando y el API esperan de forma acotada y no
  fallan de inmediato por una carrera de arranque.
- Se ejecuta el comando dos veces seguidas: no hay error ni datos semilla duplicados.
- Se revierte la migración con datos semilla: el esquema vuelve atrás y los datos se tratan
  según lo que defina la migración, sin dejar objetos huérfanos.
- La configuración externa tiene un valor incorrecto: el error no revela el valor.
- La base se detiene mientras el API está en marcha: la salud del API sigue respondiendo y el
  estado de la base cambia a «no disponible».
- No hay conexión a internet para descargar la imagen: el error se informa y no se confunde
  con un fallo de migración.
- Se elimina el volumen local de forma explícita: el siguiente comando recrea la base y las
  semillas desde cero.
- Dos desarrolladoras ejecutan el comando en la misma máquina: los nombres y puertos no
  chocan o el conflicto se informa con claridad.

## Requisitos *(obligatorio)*

### Requisitos funcionales

- **RF-001**: Un único comando documentado DEBE crear la base si no existe, aplicar todas las
  migraciones pendientes y cargar los datos semilla.
- **RF-002**: El comando único DEBE ser idempotente: ejecutarlo de nuevo no DEBE fallar ni
  duplicar datos semilla.
- **RF-003**: El esquema DEBE gestionarse exclusivamente mediante migraciones versionadas en
  el repositorio; no se permiten cambios manuales ni creación automática destructiva.
- **RF-004**: La migración inicial DEBE crear el esquema mínimo necesario para los datos
  semilla y registrar su aplicación.
- **RF-005**: Las migraciones DEBEN poder aplicarse y revertirse; tras revertir y volver a
  aplicar, el esquema y los datos semilla DEBEN quedar consistentes.
- **RF-006**: Los datos semilla DEBEN ser mínimos, deterministas, idempotentes y de ejemplo;
  no DEBEN representar reglas de negocio del catálogo ni de pedidos.
- **RF-007**: El API DEBE obtener la cadena de conexión y las credenciales solo de
  configuración externa al repositorio (variables de entorno o almacén local de secretos).
- **RF-008**: Ningún archivo versionado DEBE contener contraseñas, cadenas de conexión con
  credenciales ni claves.
- **RF-009**: El archivo de configuración de ejemplo DEBE usar únicamente marcadores de
  posición y DEBE documentar las variables necesarias.
- **RF-010**: Si la configuración de conexión falta o es inválida, el API DEBE fallar con un
  mensaje claro que indique qué falta, sin imprimir el valor del secreto.
- **RF-011**: La comprobación de salud existente DEBE seguir funcionando sin depender de la
  base de datos.
- **RF-012**: DEBE existir una comprobación separada que informe si la base está disponible,
  sin exponer credenciales ni detalles internos.
- **RF-013**: La comprobación del estado de la base DEBE tener un tiempo máximo de espera
  acotado para no dejar peticiones colgadas.
- **RF-014**: La base local DEBE conservar sus datos entre reinicios y DEBE existir un
  procedimiento explícito y destructivo para recrearla desde cero.
- **RF-015**: El comando único DEBE informar de forma clara el resultado y los errores, y no
  DEBE dejar contenedores, volúmenes ni migraciones a medias.
- **RF-016**: Esta historia NO DEBE incluir el Dockerfile del API, almacenamiento de archivos,
  OpenAPI, despliegue, autenticación ni reglas de negocio del catálogo o de pedidos.
- **RF-017**: No se DEBE agregar ninguna dependencia con licencia comercial; la licencia de
  cualquier dependencia nueva DEBE verificarse en el plan.
- **RF-018**: Las pruebas existentes, incluidas las de arquitectura y la de salud, DEBEN seguir
  pasando.

### Entidades clave

- **Tienda de ejemplo**: registro semilla con código, nombre y moneda; datos ilustrativos ya
  definidos en la historia 002b (`BHH` y `DEM`), sin reglas de negocio.
- **Migración**: unidad versionada que describe un cambio de esquema y registra si fue
  aplicada.
- **Base de datos local**: instancia de desarrollo que contiene el esquema y los datos
  semilla, con persistencia entre reinicios.
- **Configuración de conexión**: conjunto de valores externos al repositorio que permiten al
  API conectarse; nunca se versiona con credenciales reales.
- **Estado de la base**: resultado de la comprobación separada que indica disponibilidad o
  no disponibilidad.
- **Datos semilla**: conjunto mínimo y repetible de registros de ejemplo que deja la base
  utilizable desde el primer arranque.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En un entorno limpio, el comando único termina con éxito y deja la base creada,
  migrada y sembrada en menos de 10 minutos, incluida la preparación inicial del entorno.
- **CE-002**: El 100 % de las migraciones se aplican y se revierten sin pasos manuales; tras
  revertir y volver a aplicar, el esquema coincide con el estado esperado.
- **CE-003**: Ejecutar el comando único tres veces seguidas deja exactamente la misma
  cantidad de registros semilla.
- **CE-004**: El 0 % de los archivos versionados contiene credenciales, contraseñas o claves;
  la búsqueda automatizada no encuentra coincidencias.
- **CE-005**: El 100 % de los casos de configuración faltante o inválida producen un error
  claro sin revelar el valor del secreto.
- **CE-006**: La comprobación de salud existente responde correctamente en el 100 % de los
  casos con la base detenida.
- **CE-007**: La comprobación del estado de la base responde en menos de 5 segundos cuando la
  base está detenida o no responde.
- **CE-008**: Las pruebas existentes siguen en verde y la compilación no agrega advertencias.

## Fuera de alcance

- Dockerfile del API y despliegue de la API en contenedores (historia 005).
- Almacenamiento de archivos y Azurite.
- OpenAPI, Scalar y la documentación interactiva (historia 004).
- Despliegue a Azure y cualquier entorno que no sea local.
- Autenticación y autorización.
- Reglas de negocio del catálogo, pedidos, pagos, envíos y códigos de creadora.
- Migrar la consulta de ejemplo de la historia 002b para que lea de la base; esta historia
  deja la persistencia lista para las historias posteriores.
- Copias de seguridad, alta disponibilidad, ajuste de rendimiento y datos de producción.
- Integración continua (historia 006) y análisis estático.
- Proveedores externos y servicios de terceros.

## Supuestos

- La estructura en capas y el mecanismo de casos de uso de las historias 002 y 002b existen
  y son la base de esta historia.
- El motor de base de datos es SQL Server y el acceso se gestiona con migraciones de EF Core,
  conforme al backlog y al nombre de la carpeta; el plan concreta versiones, imágenes y
  comandos.
- El comando único esperado por el backlog es `docker compose up`; el plan puede envolverlo en
  un script documentado si hace falta para aplicar la migración sin el Dockerfile del API.
- La base local es desechable y solo para desarrollo; no contiene datos reales.
- Las credenciales se proporcionan por variables de entorno o por el almacén local de
  secretos del equipo; el repositorio solo incluye un ejemplo con marcadores de posición.
- Los datos semilla son las dos tiendas de ejemplo de la historia 002b (`BHH` y `DEM`), sin
  reglas de negocio; si el plan necesita otra entidad mínima, lo justifica sin ampliar el
  alcance.
- La comprobación de salud existente se mantiene como comprobación de vida del API; el estado
  de la base se informa en una comprobación separada.
- El trabajo se realiza en la rama `sprint/01`, conforme a `AGENTS.md`; no se crea una rama
  por historia.
- La máquina de desarrollo tiene el runtime de contenedores que defina el plan y espacio
  suficiente para la imagen de la base.
- No se agregan dependencias con licencia comercial; la licencia de cualquier dependencia
  nueva se verifica en el plan.
