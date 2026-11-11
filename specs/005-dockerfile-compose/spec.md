# Especificación 005: Imagen de contenedor del API y entorno local con un solo comando

**Rama de trabajo**: `sprint/01`

**Creada**: 2026-11-11

**Estado**: En progreso (pendientes solo tareas [MANUAL]: T025, T047, T051)

**Historia del backlog**: HU5 (componentes BE y CI)

**Entrada**: Descripción de la usuaria:

> Crea la carpeta specs/005-dockerfile-compose (HU5 del backlog).
>
> Como desarrolladora quiero empaquetar el API en una imagen de contenedor y arrancar API y
> base juntas con un solo comando, con verificaciones de salud y sin secretos en el
> repositorio, para ejecutar el mismo artefacto en local y en el despliegue.
>
> Debe cumplirse que:
> - Un solo comando levante el API y la base de datos, ambos reportándose sanos.
> - El API espere a que la base esté lista antes de declararse sana.
> - Toda credencial llegue por variables de entorno; el repositorio solo contiene un ejemplo
>   sin valores reales.
> - La imagen del API se construya de forma reproducible.
>
> Fuera de alcance: despliegue a Azure, publicación de imágenes y CI.
> No decidas tecnologías aquí. Escribe todo en español y respeta la constitución.

## Escenarios de uso y pruebas *(obligatorio)*

### Historia de usuario 1 — API y base sanos con un solo comando (Prioridad: P1)

Como desarrolladora quiero arrancar el API y la base de datos juntos con un solo comando, y
que ambos informen que están sanos, para tener el entorno completo listo sin recordar pasos,
órdenes ni configuraciones manuales.

**Por qué esta prioridad**: es el objetivo central de la historia y la base para que cualquier
persona del equipo pueda ejecutar el sistema completo desde el primer día. Sin esto, las demás
garantías (espera de la base, secretos y reproducibilidad) no tienen dónde aplicarse.

**Prueba independiente**: en un entorno limpio, ejecutar el comando único documentado y
comprobar que el API y la base quedan en ejecución y que cada uno responde que está sano; el
valor se entrega aunque todavía no exista ninguna operación de negocio en contenedores.

**Escenarios de aceptación**:

1. **Dado** un entorno limpio sin contenedores ni imágenes previas, **cuando** se ejecuta el
   comando único documentado, **entonces** el API y la base quedan en ejecución y ambos se
   reportan sanos.
2. **Dado** el entorno ya levantado, **cuando** se consulta el estado de cada componente,
   **entonces** el API y la base responden que están sanos.
3. **Dado** el comando en ejecución, **cuando** termina, **entonces** informa de forma clara
   qué componentes quedaron sanos y en qué dirección se accede al API.
4. **Dado** un entorno ya levantado, **cuando** se ejecuta el mismo comando otra vez,
   **entonces** termina sin error y deja el entorno sano.
5. **Dado** un componente que no alcanza el estado sano, **cuando** el comando termina,
   **entonces** informa el fallo y no declara el entorno como listo.

---

### Historia de usuario 2 — El API espera a la base antes de declararse sana (Prioridad: P2)

Como desarrolladora quiero que el API no se declare sano hasta que la base esté lista, para
que un arranque simultáneo no produzca errores intermitentes ni un estado sano engañoso.

**Por qué esta prioridad**: es la condición que hace confiable el comando único. Sin la espera,
el API puede quedar marcado como sano mientras la base todavía inicia, y cualquier operación
que dependa de la base falla de forma intermitente.

**Prueba independiente**: arrancar el entorno desde cero y observar que el API no se reporta
sano mientras la base no lo está; después, comprobar que en cuanto la base queda sana el API
también pasa a sano sin reiniciar el comando. Repetir con la base detenida y comprobar que el
API no se declara sano.

**Escenarios de aceptación**:

1. **Dado** un arranque simultáneo de API y base, **cuando** la base todavía está iniciando,
   **entonces** el API no se declara sano y no responde como listo para atender operaciones
   que dependen de la base.
2. **Dado** el API esperando a la base, **cuando** la base queda sana, **entonces** el API
   pasa a sano de forma automática, sin reiniciar el comando.
3. **Dada** la base detenida, **cuando** se consulta la salud del API, **entonces** el API no
   se declara sano y la comprobación de vida del API sigue respondiendo.
4. **Dada** una base que tarda más de lo esperado, **cuando** el API espera, **entonces** la
   espera es acotada y el resultado se informa, sin dejar el proceso colgado.
5. **Dado** el API sano, **cuando** la base deja de estar disponible, **entonces** el API deja
   de reportarse sano y el cambio se refleja sin reiniciar los contenedores.

---

### Historia de usuario 3 — Credenciales solo por variables de entorno (Prioridad: P3)

Como responsable del repositorio público quiero que toda credencial llegue desde fuera del
control de versiones y que el repositorio solo contenga un ejemplo sin valores reales, para no
exponer secretos y poder cambiar credenciales sin tocar el código.

**Por qué esta prioridad**: el repositorio es público; una credencial filtrada compromete la
base y obliga a rotarla. La seguridad es condición para compartir el entorno con el equipo.

**Prueba independiente**: buscar credenciales en todos los archivos versionados y confirmar
que no hay ninguna; después, proporcionar la configuración por el mecanismo externo definido y
comprobar que el entorno arranca; finalmente, omitir una variable obligatoria y comprobar que
el arranque falla con un mensaje claro sin imprimir el valor.

**Escenarios de aceptación**:

1. **Dado** el repositorio clonado, **cuando** se buscan contraseñas, cadenas de conexión con
   credenciales o claves, **entonces** no hay ninguna en archivos versionados.
2. **Dado** el archivo de ejemplo de configuración, **cuando** se inspecciona, **entonces**
   solo contiene marcadores de posición y la lista de variables necesarias, nunca valores
   reales.
3. **Dada** la configuración externa completa, **cuando** se ejecuta el comando único,
   **entonces** el entorno arranca y ambos componentes quedan sanos.
4. **Dada** la configuración externa incompleta, **cuando** se intenta arrancar, **entonces**
   el fallo indica qué variable falta, sin mostrar su valor ni otros secretos.
5. **Dado** un cambio de credencial en el entorno, **cuando** se reinicia el entorno,
   **entonces** toma la nueva credencial sin cambios en el código ni en la imagen.
6. **Dados** los registros del comando y de los contenedores, **cuando** se revisan,
   **entonces** no contienen credenciales ni cadenas de conexión completas.

---

### Historia de usuario 4 — Imagen reproducible y mismo artefacto en local y despliegue (Prioridad: P4)

Como desarrolladora quiero que la imagen del API se construya de forma reproducible y que sea
el mismo artefacto que se ejecute en local y en el despliegue, para que lo probado en local sea
lo que se publica y para poder reconstruir una versión idéntica cuando haga falta.

**Por qué esta prioridad**: sin reproducibilidad, dos construcciones del mismo código pueden
diferir y las pruebas locales pierden valor frente al despliegue. La imagen es el vehículo del
API, así que su consistencia sostiene las otras tres historias.

**Prueba independiente**: construir la imagen dos veces desde el mismo estado del repositorio y
comprobar que el resultado funcional es el mismo (misma versión y dependencias), que la
configuración no está incrustada y que el contenedor arranca igual en local sin reconstruir.

**Escenarios de aceptación**:

1. **Dado** el mismo estado del repositorio, **cuando** se construye la imagen dos veces,
   **entonces** el resultado funcional es idéntico y verificable.
2. **Dada** una imagen construida, **cuando** se ejecuta en local, **entonces** usa la
   configuración inyectada en tiempo de ejecución y no valores incrustados en la imagen.
3. **Dada** una imagen construida, **cuando** se inspecciona, **entonces** no contiene
   secretos, archivos de configuración con credenciales ni artefactos de compilación
   innecesarios.
4. **Dado** un cambio de configuración entre entornos, **cuando** se reutiliza la misma
   imagen, **entonces** no hace falta reconstruirla.
5. **Dada** una construcción sin acceso a las dependencias externas, **cuando** se intenta,
   **entonces** el fallo se informa de forma clara y distinta de un fallo de la aplicación.

---

### Casos límite

- El puerto del API o el de la base ya está ocupado por otro proceso: el comando falla con un
  mensaje claro y no deja contenedores a medias.
- La base tarda en aceptar conexiones: el API espera de forma acotada y no falla de inmediato
  por una carrera de arranque.
- No hay conexión para descargar las imágenes: el error se informa y no se confunde con un
  fallo de la aplicación ni con un problema de migraciones.
- Falta una variable de entorno obligatoria: el arranque falla indicando cuál falta y nunca
  imprime el valor.
- Una credencial se agrega por error a un archivo versionado: la búsqueda automatizada la
  detecta y la historia no se considera terminada.
- Se ejecuta el comando dos veces seguidas: no hay error, no se duplican datos semilla y el
  entorno queda sano.
- Se reconstruye la imagen sin cambios en el código: el resultado funcional se mantiene y la
  versión se identifica de forma consistente.
- La base conserva un volumen con un esquema anterior: se aplican las migraciones pendientes o
  se informa la incompatibilidad sin borrar datos por cuenta propia.
- Se detiene la base mientras el API sigue en marcha: el API deja de reportarse sano y la
  comprobación de vida sigue respondiendo.
- Un contenedor se reinicia solo: el entorno recupera el estado sano sin pasos manuales.
- La máquina de desarrollo usa una arquitectura distinta de la documentada: el fallo se
  informa con claridad o el soporte se declara en el plan.
- El runtime de contenedores no está instalado o es demasiado antiguo: el comando lo detecta y
  explica qué falta antes de intentar arrancar.
- Los registros de un componente incluyen la cadena de conexión: se considera un defecto y se
  corrige antes de dar la historia por terminada.
- El API arranca en el entorno de ejecución del contenedor y no en desarrollo: los endpoints
  exclusivos de pruebas no existen y el comportamiento sigue siendo el acordado.

## Requisitos *(obligatorio)*

### Requisitos funcionales

- **RF-001**: Un único comando documentado DEBE construir la imagen si hace falta y arrancar el
  API y la base de datos juntos.
- **RF-002**: Al terminar el comando, el API y la base DEBEN estar en ejecución y reportarse
  sanos; el comando DEBE informar el resultado y la dirección de acceso al API.
- **RF-003**: El comando único DEBE ser idempotente: ejecutarlo de nuevo no DEBE fallar ni
  duplicar datos semilla.
- **RF-004**: El API DEBE empaquetarse como una imagen de contenedor que contenga solo lo
  necesario para ejecutarse.
- **RF-005**: La imagen del API DEBE ejecutarse con un usuario sin privilegios y sin
  herramientas de compilación ni archivos fuente innecesarios.
- **RF-006**: La imagen del API DEBE construirse de forma reproducible: las mismas entradas
  DEBEN producir el mismo artefacto funcional, con referencias fijadas y sin descargas no
  deterministas.
- **RF-007**: La misma imagen DEBE poder ejecutarse en local y en el despliegue; la
  configuración DEBE inyectarse en tiempo de ejecución y no incrustarse en la imagen.
- **RF-008**: El contenedor del API DEBE exponer una verificación de salud que distinga entre
  estar vivo y estar listo para atender operaciones que dependen de la base.
- **RF-009**: La verificación de salud del API DEBE declararse sana solo cuando la base esté
  disponible y su esquema esté al día.
- **RF-010**: Mientras la base no esté lista, el API DEBE esperar de forma acotada y no DEBE
  fallar de inmediato ni entrar en un ciclo de reinicio sin control.
- **RF-011**: La base DEBE tener su propia verificación de salud y el arranque del API DEBE
  depender de que la base esté sana.
- **RF-012**: Toda credencial DEBE llegar por variables de entorno o por un mecanismo externo
  al repositorio; ningún archivo versionado DEBE contener valores reales.
- **RF-013**: El repositorio DEBE incluir un archivo de ejemplo con solo marcadores de posición
  y la lista de variables necesarias.
- **RF-014**: Si falta una variable obligatoria o su valor es inválido, el arranque DEBE fallar
  con un mensaje claro que indique qué falta, sin imprimir el valor del secreto.
- **RF-015**: Los registros del comando y de los contenedores NO DEBEN mostrar credenciales,
  cadenas de conexión completas ni otros secretos.
- **RF-016**: El comando DEBE dejar la base migrada y con los datos semilla de la historia 003
  antes de declarar el entorno listo, sin duplicarlos.
- **RF-017**: El entorno local DEBE conservar los datos de la base entre reinicios y DEBE
  existir un procedimiento explícito y destructivo para recrearlo desde cero.
- **RF-018**: Si un puerto está ocupado, si falta el runtime de contenedores o si una imagen no
  se puede descargar, el comando DEBE fallar con un mensaje claro y no dejar recursos a medias.
- **RF-019**: El entorno DEBE funcionar sin conexión una vez descargadas las imágenes, y DEBE
  distinguir el fallo de descarga del fallo de la aplicación.
- **RF-020**: El API dentro del contenedor DEBE ejecutarse en el entorno de ejecución definido
  para el despliegue, sin endpoints exclusivos de pruebas.
- **RF-021**: Esta historia NO DEBE incluir despliegue a Azure, publicación de imágenes, CI,
  cambios de lógica de negocio, nuevos endpoints ni cambios de esquema más allá de la historia
  003.
- **RF-022**: No se DEBE agregar ninguna dependencia con licencia comercial; la licencia de
  cualquier dependencia nueva DEBE verificarse en el plan.
- **RF-023**: Las pruebas existentes, incluidas las de arquitectura y las de salud, DEBEN seguir
  pasando; la historia DEBE agregar verificaciones automatizadas de la imagen y del entorno.

### Entidades clave

- **Imagen del API**: artefacto ejecutable que contiene el API y lo necesario para correr, sin
  herramientas de compilación ni secretos.
- **Entorno local orquestado**: conjunto de componentes (API y base de datos) que se levantan,
  se enlazan y se detienen juntos con un solo comando.
- **Verificación de salud**: comprobación que informa si un componente está vivo y si está
  listo para atender operaciones.
- **Dependencia de arranque**: relación por la cual el API no se declara listo hasta que la
  base está sana.
- **Configuración externa**: conjunto de valores, incluidos los secretos, que se proporcionan
  fuera del repositorio y se inyectan en tiempo de ejecución.
- **Archivo de ejemplo de configuración**: documento versionado con solo marcadores de posición
  y la lista de variables necesarias.
- **Artefacto reproducible**: imagen que, construida desde el mismo estado del repositorio,
  produce el mismo resultado funcional.
- **Estado del entorno**: resultado conjunto que indica si el API y la base están sanos y si el
  entorno está listo para usarse.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En un entorno limpio, el comando único deja el API y la base sanos en menos de 10
  minutos, incluidas las descargas iniciales; en caliente, en menos de 3 minutos.
- **CE-002**: El 100 % de las ejecuciones repetidas del comando termina sin error y deja
  exactamente la misma cantidad de registros semilla.
- **CE-003**: El 0 % de los archivos versionados contiene credenciales, contraseñas o claves;
  la búsqueda automatizada no encuentra coincidencias.
- **CE-004**: El 100 % de los casos de variable obligatoria faltante o inválida produce un
  error claro sin revelar el valor.
- **CE-005**: Dos construcciones de la imagen desde el mismo estado del repositorio producen un
  artefacto funcionalmente idéntico y verificable en versión y dependencias.
- **CE-006**: La verificación de salud del API refleja el estado de la base en el 100 % de los
  casos: no se declara sano con la base detenida y pasa a sano cuando la base está disponible.
- **CE-007**: Con la base detenida, el 100 % de las consultas a la salud de vida del API sigue
  respondiendo, y el API no se reporta listo.
- **CE-008**: El 0 % de los registros del comando y de los contenedores contiene credenciales o
  cadenas de conexión completas.
- **CE-009**: Las pruebas existentes siguen en verde y la compilación no agrega advertencias.

## Fuera de alcance

- Despliegue a Azure y a cualquier entorno remoto.
- Publicación de imágenes en un registro o catálogo.
- Integración continua, análisis estático y automatización de pipelines (historia 006).
- Cambios de lógica de negocio, nuevos endpoints, autenticación y autorización.
- Cambios de esquema más allá de la migración y los datos semilla de la historia 003.
- Orquestación de producción: alta disponibilidad, escalado, reinicio administrado y
  programación de contenedores.
- TLS, proxy inverso, dominio, certificados y redes expuestas a internet.
- Copias de seguridad, restauración y recuperación ante desastres.
- Observabilidad: métricas, trazas distribuidas, paneles y alertas.
- Almacenamiento de archivos y Azurite.
- OpenAPI, Scalar y la documentación interactiva (historia 004).
- Endurecimiento de seguridad más allá del usuario sin privilegios, la separación de secretos y
  la ausencia de artefactos de compilación en la imagen.
- Soporte de arquitecturas distintas de la documentada para la máquina de desarrollo.

## Supuestos

- El backlog fija el empaquetado del API como imagen de contenedor y el arranque conjunto con
  la base mediante un comando de orquestación local; el plan concreta el formato del archivo de
  imagen, las versiones, las referencias inmutables y el comando exacto.
- La historia 003 ya define el servicio de base de datos, su configuración, su archivo de
  ejemplo y el comando que la levanta y migra; esta historia lo extiende con el servicio del
  API sin duplicar la base.
- Los nombres de las variables de entorno de la historia 003 se reutilizan; el plan confirma
  que no se renombran ni se duplican.
- La comprobación de vida `/salud` y la comprobación de la base `/salud/base` de las historias
  002 y 003 existen. Esta historia agrega la verificación de salud del contenedor que combina
  vida y disponibilidad de la base, sin cambiar el comportamiento acordado de esas rutas.
- La imagen del API se construye una vez y se promueve entre entornos; la configuración se
  inyecta en tiempo de ejecución.
- El contenedor del API se ejecuta en el entorno de ejecución del despliegue, no en desarrollo;
  los endpoints exclusivos de pruebas no existen ahí.
- La historia 004 publica el documento OpenAPI y la página interactiva en todos los entornos;
  esta historia no cambia esa decisión y el plan define si el contenedor los expone igual que
  en local.
- La base local es desechable y solo para desarrollo; no contiene datos reales.
- La máquina de desarrollo tiene el runtime de contenedores que defina el plan y espacio
  suficiente para las imágenes del API y de la base.
- El trabajo se realiza en la rama `sprint/01`, conforme a `AGENTS.md`; no se crea una rama por
  historia.
- No se agregan dependencias con licencia comercial; la licencia de cualquier dependencia nueva
  se verifica en el plan.
