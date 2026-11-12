# Especificación 002: Estructura base del API

**Rama de funcionalidad**: `002-estructura-base-api`

**Creada**: 2026-11-10

**Estado**: Completada

**Entrada**: Descripción de la usuaria:

> Estructura base del API de BlackHellHeim (primera historia de la fundación, tomada de
> `specs/000-fundacion/spec.md`).
>
> Como desarrolladora quiero una solución de backend con capas separadas (Dominio, Aplicación,
> Infraestructura, Api y Pruebas) para respetar la regla de que las dependencias apuntan hacia
> adentro y poder construir las demás historias sobre una base ordenada.
>
> Debe cumplirse que:
> - Dominio no dependa de ninguna otra capa y Aplicación dependa solo de Dominio.
> - Una prueba automática falle si alguna capa rompe esa regla de dependencias.
> - El proyecto compile y sus pruebas pasen con un solo comando.
> - La API arranque y responda una comprobación de estado básica.
>
> Fuera de alcance en esta historia: integración continua, base de datos, almacenamiento de
> archivos, documentación OpenAPI y despliegue, que van en historias separadas.
>
> No decidas tecnologías aquí; eso va en el plan. Escribe todo en español y respeta
> `.specify/memory/constitution.md`.

## Escenarios de uso y pruebas *(obligatorio)*

### Historia de usuario 1 — Solución en capas con dependencias hacia adentro (Prioridad: P1)

Como desarrolladora quiero una solución de backend organizada en las capas Dominio, Aplicación,
Infraestructura, Api y Pruebas, con las dependencias apuntando hacia adentro, para construir las
demás historias sobre una base ordenada.

**Por qué esta prioridad**: sin la estructura y su regla de dependencias no existe la base del
proyecto; todas las historias posteriores se construyen sobre ella.

**Prueba independiente**: se verifica por completo inspeccionando las referencias entre las capas
de la solución recién creada y compilándola con éxito; entrega valor por sí sola como base
ordenada aunque ninguna otra historia exista todavía.

**Escenarios de aceptación**:

1. **Dada** la solución recién creada con sus cinco capas, **cuando** se revisan las dependencias
   entre capas, **entonces** Dominio no referencia ninguna otra capa y Aplicación referencia solo
   a Dominio.
2. **Dada** una capa exterior que requiere algo de una capa interior, **cuando** se declara la
   dependencia, **entonces** la referencia queda de la exterior hacia la interior, nunca al revés.
3. **Dada** la solución completa, **cuando** se compila, **entonces** la compilación es exitosa.

---

### Historia de usuario 2 — Prueba automática de la regla de dependencias (Prioridad: P2)

Como desarrolladora quiero una prueba automática que falle cuando alguna capa rompa la regla de
dependencias, para detectar regresiones de arquitectura sin depender de revisión manual.

**Por qué esta prioridad**: una regla de arquitectura sin vigilancia se erosiona con cada cambio;
la constitución exige que toda regla quede probada (principio III).

**Prueba independiente**: introduciendo deliberadamente una referencia que rompa la regla, la
prueba falla; retirándola, la prueba pasa.

**Escenarios de aceptación**:

1. **Dada** una solución que cumple la regla de dependencias, **cuando** se ejecuta la prueba de
   arquitectura, **entonces** pasa.
2. **Dada** una referencia de Dominio hacia otra capa, **cuando** se ejecuta la prueba de
   arquitectura, **entonces** falla señalando la capa infractora.
3. **Dada** una referencia de Aplicación hacia una capa distinta de Dominio, **cuando** se ejecuta
   la prueba de arquitectura, **entonces** falla señalando la capa infractora.

---

### Historia de usuario 3 — Compilación y pruebas con un solo comando (Prioridad: P3)

Como desarrolladora quiero compilar el proyecto y ejecutar todas sus pruebas con un solo comando,
para conocer el estado de la solución sin pasos manuales.

**Por qué esta prioridad**: es la verificación cotidiana de la Definición de Hecho y el cimiento
de la integración continua, que llega en una historia separada.

**Prueba independiente**: en un entorno de desarrollo estándar, ejecutar el comando único sobre la
solución y observar que compila y ejecuta todas las pruebas.

**Escenarios de aceptación**:

1. **Dada** la solución en un estado válido, **cuando** se ejecuta el comando único, **entonces**
   compila la solución completa y todas las pruebas pasan.
2. **Dada** al menos una prueba que falla, **cuando** se ejecuta el comando único, **entonces**
   termina con estado de error, sin reportar un éxito parcial.
3. **Dada** la solución sin compilación previa, **cuando** se ejecuta el comando único,
   **entonces** este compila y prueba sin requerir pasos previos.

---

### Historia de usuario 4 — Arranque de la API con comprobación de estado (Prioridad: P4)

Como desarrolladora quiero que la API arranque y responda una comprobación de estado básica, para
confirmar que la base está viva antes de construir las historias siguientes.

**Por qué esta prioridad**: las historias posteriores (autenticación, catálogo, pagos) necesitan
una API ejecutable; la comprobación de estado es su prueba de vida mínima.

**Prueba independiente**: arrancar la API en un entorno de desarrollo estándar e invocar la
comprobación de estado; la respuesta indica que está en marcha.

**Escenarios de aceptación**:

1. **Dada** la API arrancada en un entorno de desarrollo estándar, **cuando** se invoca la
   comprobación de estado, **entonces** responde de forma exitosa indicando que está en marcha.
2. **Dado** que la base de datos, el almacenamiento de archivos y los proveedores externos aún no
   existen (fuera de alcance), **cuando** se invoca la comprobación de estado, **entonces**
   responde igualmente, sin depender de ellos.
3. **Dada** la API sin arrancar, **cuando** se invoca la comprobación de estado, **entonces** no
   se obtiene respuesta exitosa.

---

### Casos límite

- ¿Qué ocurre cuando alguien agrega una referencia que rompe la regla después de terminada la
  historia? La prueba de arquitectura debe fallar en la siguiente ejecución del comando único.
- ¿Qué ocurre cuando dos capas se referencian mutuamente? La regla lo prohíbe; la prueba de
  arquitectura debe fallar.
- ¿Qué ocurre cuando se agrega un proyecto o capa nueva a la solución? También queda sujeto a la
  regla; la verificación no debe dejar capas fuera de su cobertura.
- ¿Qué ocurre si la comprobación de estado se invoca antes de que la API termine de arrancar? No
  debe reportarse como exitosa hasta que la API esté en marcha.

## Requisitos *(obligatorio)*

### Requisitos funcionales

- **RF-001**: La solución DEBE organizarse en cinco capas: Dominio, Aplicación, Infraestructura,
  Api y Pruebas.
- **RF-002**: Dominio NO DEBE depender de ninguna otra capa de la solución.
- **RF-003**: Aplicación DEBE depender, entre las capas de la solución, únicamente de Dominio.
- **RF-004**: Las dependencias entre capas DEBEN apuntar siempre hacia adentro: ninguna capa puede
  depender de una capa más externa que ella misma.
- **RF-005**: Una prueba automática DEBE fallar cuando cualquier capa rompa la regla de
  dependencias, identificando la dependencia infractora.
- **RF-006**: Un único comando DEBE compilar la solución completa y ejecutar todas sus pruebas,
  sin pasos manuales intermedios.
- **RF-007**: La API DEBE arrancar en un entorno de desarrollo estándar y responder una
  comprobación de estado básica que indique que está en marcha.
- **RF-008**: La comprobación de estado DEBE funcionar sin base de datos, almacenamiento de
  archivos ni proveedores externos.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En un entorno de desarrollo estándar, un solo comando compila la solución completa
  y ejecuta todas las pruebas con resultado exitoso, sin pasos manuales intermedios.
- **CE-002**: Al introducir deliberadamente una dependencia que rompa la regla (por ejemplo, de
  Dominio hacia otra capa), la prueba de arquitectura falla en esa misma ejecución.
- **CE-003**: Con la solución en estado válido, la prueba de arquitectura pasa siempre, sin
  falsos positivos que bloqueen el trabajo.
- **CE-004**: La API arrancada en un entorno de desarrollo estándar responde la comprobación de
  estado de forma exitosa en cada invocación, sin servicios externos configurados.

## Fuera de alcance

Integración continua, base de datos, almacenamiento de archivos, documentación OpenAPI y
despliegue; cada uno vive en su propia historia (véase `specs/000-fundacion/spec.md`). La
comprobación de estado de esta historia es una sonda operativa mínima: su contrato formal se
define en la historia de OpenAPI, conforme al principio II de la constitución.

## Supuestos

- La elección de tecnologías concretas (lenguaje, herramientas de pruebas, mecanismo de la prueba
  de arquitectura, forma exacta del comando único y de la comprobación de estado) se decide en el
  plan, no en esta especificación.
- Las capas externas (Infraestructura, Api, Pruebas) pueden depender de las capas interiores según
  la regla general hacia adentro; la matriz fina de referencias permitidas se precisa en el plan
  conforme al principio I de la constitución.
- El entorno de desarrollo estándar es el que documenta el propio repositorio; instalarlo no es
  parte de esta historia.
- La comprobación de estado se limita a indicar que la API está en marcha; las comprobaciones de
  dependencias (base de datos, archivos, proveedores) llegan con sus historias correspondientes.
- Esta es la historia 2 de la épica E0 (fundación); el detalle y los números de historia del
  backlog viven en el documento «BlackHellHeim: backlog Scrum y estructura SDD», externo al
  repositorio.
