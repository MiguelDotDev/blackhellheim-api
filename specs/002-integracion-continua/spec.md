# Especificación 002: Integración continua del API

**Rama de funcionalidad**: `002-integracion-continua`

**Creada**: 2026-11-10

**Estado**: Borrador

**Entrada**: Descripción de la usuaria:

> Integración continua del API de BlackHellHeim (segunda historia de la fundación, tomada de
> `specs/000-fundacion/spec.md`).
>
> Como desarrolladora quiero que cada cambio que suba al repositorio se compile y se pruebe
> automáticamente, para que ningún cambio con errores llegue a la rama principal sin que yo lo
> note.
>
> Debe cumplirse que:
> - En cada pull request y en cada push a la rama principal se compile la solución y se ejecuten
>   todas las pruebas.
> - Si algo falla, el resultado quede visible en el pull request como error.
> - Se haga un análisis estático de seguridad del código con CodeQL.
> - Se haga un análisis de calidad con SonarQube Cloud (gratis para repositorios públicos).
> - El flujo no use ni exponga secretos en el repositorio, que es público.
>
> Fuera de alcance: despliegue a Azure, base de datos, almacenamiento de archivos y documentación
> OpenAPI.
>
> No decidas tecnologías aquí; eso va en el plan. Escribe todo en español y respeta
> `.specify/memory/constitution.md`.

## Escenarios de uso y pruebas *(obligatorio)*

### Historia de usuario 1 — Compilación y pruebas automáticas en cada cambio (Prioridad: P1)

Como desarrolladora quiero que cada pull request y cada push a la rama principal compilen la
solución y ejecuten todas las pruebas automáticamente, para detectar errores antes de integrar
cambios y no depender de verificaciones manuales.

**Por qué esta prioridad**: es el núcleo de la integración continua y la primera barrera contra
cambios rotos; sin esta verificación automática, el resto de los análisis no tiene una base que
proteger.

**Prueba independiente**: abrir un pull request con un cambio válido y observar que la compilación
y las pruebas se ejecutan solas y reportan éxito; después, introducir un error de compilación o una
prueba que falla y observar que el pull request muestra el fallo como error.

**Escenarios de aceptación**:

1. **Dado** un pull request con cambios válidos, **cuando** se abre o se actualiza, **entonces** la
   solución completa se compila y todas las pruebas se ejecutan automáticamente, sin pasos
   manuales, y el resultado se reporta como exitoso.
2. **Dado** un pull request con un error de compilación o una prueba que falla, **cuando** se
   ejecuta la verificación automática, **entonces** el pull request muestra el resultado como error
   y el cambio no puede integrarse a la rama principal mientras la protección de rama exija esa
   verificación.
3. **Dado** un push directo a la rama principal, **cuando** llega el cambio, **entonces** la
   solución se compila y todas las pruebas se ejecutan automáticamente, y el resultado queda
   registrado; si falla, la rama principal queda marcada como roja de forma visible.
4. **Dado** un cambio que agrega un proyecto nuevo a la solución, **cuando** se ejecuta la
   verificación automática, **entonces** el proyecto nuevo también se compila y sus pruebas se
   ejecutan, sin quedar fuera de la cobertura por falta de configuración.
5. **Dada** una ejecución cancelada o reemplazada por un commit más reciente, **cuando** se
   consulta el estado del pull request, **entonces** se muestra el resultado de la última
   verificación correspondiente al commit vigente, sin dar por bueno un resultado anterior.

---

### Historia de usuario 2 — Flujo sin secretos expuestos en un repositorio público (Prioridad: P2)

Como mantenedora de un repositorio público quiero que la integración continua funcione sin exponer
credenciales, para que ninguna persona pueda reutilizar secretos del proyecto y para que las
contribuciones externas no obliguen a compartirlos.

**Por qué esta prioridad**: el repositorio es público y la constitución prohíbe secretos en él
(principio VIII); una filtración de credenciales tendría impacto inmediato en la seguridad y en los
servicios conectados.

**Prueba independiente**: revisar los archivos versionados y el historial en busca de credenciales,
revisar los registros de una ejecución y abrir un pull request desde una bifurcación sin acceso a
secretos; la compilación y las pruebas deben seguir funcionando sin exponer nada.

**Escenarios de aceptación**:

1. **Dado** el repositorio público, **cuando** se revisan los archivos versionados y su historial,
   **entonces** no hay valores de credenciales, tokens ni claves privadas.
2. **Dado** un flujo que necesita una credencial para un análisis externo, **cuando** se ejecuta,
   **entonces** la obtiene de un almacén cifrado fuera del repositorio y nunca la imprime ni la
   incluye en artefactos o registros.
3. **Dado** un pull request enviado desde una bifurcación sin acceso a secretos, **cuando** se
   ejecuta la verificación automática, **entonces** la compilación, las pruebas y los análisis que
   no requieren credenciales se ejecutan igualmente; los análisis que sí las requieren se omiten de
   forma visible, sin fallar en silencio ni exponer credenciales.
4. **Dado** un registro de ejecución, **cuando** se revisa, **entonces** cualquier valor sensible
   aparece enmascarado o ausente, incluso si un comando lo imprime por error.
5. **Dada** una credencial ausente, vencida o revocada, **cuando** se ejecuta el análisis que la
   necesita, **entonces** el resultado se muestra como fallo o como omisión explícita, nunca como
   éxito falso.

---

### Historia de usuario 3 — Análisis estático de seguridad con CodeQL (Prioridad: P3)

Como desarrolladora quiero que cada cambio pase por un análisis estático de seguridad con CodeQL,
para detectar vulnerabilidades conocidas antes de que lleguen a la rama principal.

**Por qué esta prioridad**: la seguridad es un requisito de la Definición de Hecho y el repositorio
público queda expuesto a revisión; el análisis debe ser automático y visible en el pull request.

**Prueba independiente**: abrir un pull request y observar que el análisis de seguridad se ejecuta y
publica su resultado; introducir un patrón vulnerable conocido y comprobar que el hallazgo aparece
con severidad y ubicación.

**Escenarios de aceptación**:

1. **Dado** un pull request, **cuando** se ejecuta la integración continua, **entonces** CodeQL
   analiza el código de la solución y publica el resultado como una verificación de seguridad
   visible en el pull request.
2. **Dado** un push a la rama principal, **cuando** llega el cambio, **entonces** el análisis de
   seguridad también se ejecuta y actualiza los resultados de la rama principal.
3. **Dado** un hallazgo de seguridad, **cuando** se publica el resultado, **entonces** se muestra
   con su severidad y ubicación en el código, de modo que la desarrolladora pueda corregirlo antes
   de integrar.
4. **Dado** un cambio sin hallazgos, **cuando** termina el análisis, **entonces** la verificación de
   seguridad se reporta como exitosa.
5. **Dado** un pull request desde una bifurcación sin credenciales, **cuando** se ejecuta el
   análisis de seguridad, **entonces** este funciona con los permisos mínimos necesarios y sin
   secretos del repositorio.

---

### Historia de usuario 4 — Análisis de calidad con SonarQube Cloud (Prioridad: P4)

Como desarrolladora quiero que cada cambio pase por un análisis de calidad con SonarQube Cloud, para
detectar deuda técnica, duplicación y malas prácticas antes de integrar, sin pagar licencias.

**Por qué esta prioridad**: complementa las pruebas y la seguridad con una revisión sostenida de
calidad; es importante, pero depende de que la compilación y las pruebas ya funcionen.

**Prueba independiente**: abrir un pull request y observar que el análisis de calidad se ejecuta y
publica el estado de la compuerta de calidad; introducir un cambio que empeore una métrica y
comprobar que el resultado se refleja.

**Escenarios de aceptación**:

1. **Dado** un pull request, **cuando** se ejecuta la integración continua, **entonces** SonarQube
   Cloud analiza el código y publica el resultado de la compuerta de calidad como una verificación
   visible en el pull request.
2. **Dado** un push a la rama principal, **cuando** llega el cambio, **entonces** el análisis de
   calidad también se ejecuta y actualiza el estado de la rama principal.
3. **Dado** un resultado de calidad que no cumple la compuerta, **cuando** se publica, **entonces**
   el pull request muestra el fallo de forma visible y, si la protección de rama lo exige, el cambio
   no puede integrarse.
4. **Dado** un repositorio público, **cuando** se ejecuta el análisis, **entonces** se usa la oferta
   gratuita de SonarQube Cloud, sin licencias de pago ni dependencias comerciales.
5. **Dado** un pull request desde una bifurcación sin el token de SonarQube Cloud, **cuando** se
   ejecuta la integración continua, **entonces** el análisis de calidad se omite de forma visible,
   sin exponer el token y sin impedir la compilación ni las pruebas.

---

### Casos límite

- ¿Qué ocurre si el servicio de integración continua está caído o una verificación queda pendiente?
  El pull request no debe mostrar éxito; la verificación pendiente o fallida impide integrar
  mientras la protección de rama la exija.
- ¿Qué ocurre si una prueba es inestable y falla de forma intermitente? El fallo debe quedar visible;
  un reintento manual no debe ocultar el resultado original.
- ¿Qué ocurre si se agrega un lenguaje o un tipo de proyecto que CodeQL no cubre? La cobertura debe
  reportarse explícitamente; no se debe dar por analizado un código que quedó fuera.
- ¿Qué ocurre si se agrega un archivo de configuración o un flujo nuevo? También debe quedar sujeto
  a la verificación y a la revisión de secretos.
- ¿Qué ocurre si un pull request cambia solo documentación? La verificación debe reportar un estado
  comprensible; no debe simular un éxito de compilación que no ocurrió.
- ¿Qué ocurre si el token de SonarQube Cloud vence o se revoca? El análisis debe fallar o omitirse de
  forma visible, nunca reportar una compuerta aprobada sin haber analizado.
- ¿Qué ocurre si un colaborador intenta imprimir una credencial en los registros? El valor debe
  aparecer enmascarado; además, la credencial debe rotarse si hubo exposición.

## Requisitos *(obligatorio)*

### Requisitos funcionales

- **RF-001**: En cada pull request, la solución completa DEBE compilarse y todas sus pruebas DEBEN
  ejecutarse automáticamente, sin pasos manuales.
- **RF-002**: En cada push a la rama principal, la solución completa DEBE compilarse y todas sus
  pruebas DEBEN ejecutarse automáticamente.
- **RF-003**: El resultado de la compilación y de las pruebas DEBE quedar visible en el pull request
  como una verificación exitosa o como error.
- **RF-004**: Cuando la protección de rama exija las verificaciones, un pull request con compilación
  o pruebas fallidas NO DEBE poder integrarse a la rama principal.
- **RF-005**: Cada pull request y cada push a la rama principal DEBEN pasar por un análisis estático
  de seguridad con CodeQL.
- **RF-006**: Los hallazgos de CodeQL DEBEN quedar visibles con severidad y ubicación, como
  verificación de seguridad del pull request.
- **RF-007**: Cada pull request y cada push a la rama principal DEBEN pasar por un análisis de
  calidad con SonarQube Cloud, usando la oferta gratuita para repositorios públicos.
- **RF-008**: El resultado de la compuerta de calidad de SonarQube Cloud DEBE quedar visible en el
  pull request como una verificación.
- **RF-009**: El repositorio NO DEBE contener valores de credenciales, tokens ni claves privadas en
  archivos versionados ni en su historial.
- **RF-010**: Las credenciales necesarias DEBEN residir en un almacén cifrado fuera del repositorio y
  NUNCA DEBEN imprimirse, registrarse ni incluirse en artefactos.
- **RF-011**: Los pull requests desde bifurcaciones sin acceso a secretos DEBEN poder compilar,
  probar y ejecutar los análisis que no requieren credenciales; los análisis que sí las requieren
  DEBEN omitirse de forma visible.
- **RF-012**: Una credencial ausente, vencida o revocada NO DEBE producir un resultado de éxito falso;
  DEBE reportarse como fallo o como omisión explícita.
- **RF-013**: Las verificaciones DEBEN ejecutarse con permisos mínimos: lectura por defecto y
  escritura solo para publicar los resultados que lo requieran.
- **RF-014**: El resultado de cada verificación DEBE ser comprensible sin leer la configuración
  interna del flujo, identificando al menos compilación, pruebas, seguridad y calidad.
- **RF-015**: La integración continua NO DEBE desplegar la aplicación, aprovisionar base de datos o
  almacenamiento, ni publicar documentación OpenAPI en esta historia.

### Entidades clave

- **Ejecución de verificación**: intento asociado a un commit o pull request, con estado pendiente,
  exitoso, fallido u omitido, y con el conjunto de comprobaciones que lo componen.
- **Hallazgo de seguridad**: resultado del análisis estático con severidad, ubicación y estado.
- **Resultado de calidad**: estado de la compuerta de calidad y métricas asociadas al cambio.
- **Credencial**: valor sensible almacenado fuera del repositorio, con vigencia y alcance mínimos.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: El 100 % de los pull requests y de los push a la rama principal disparan la
  compilación y las pruebas sin intervención manual.
- **CE-002**: Un pull request con compilación o pruebas fallidas muestra el error en menos de 10
  minutos desde el último cambio y no puede integrarse mientras la protección de rama exija la
  verificación.
- **CE-003**: El 100 % de los pull requests recibe un resultado del análisis estático de seguridad
  visible antes de poder integrarse.
- **CE-004**: El 100 % de los pull requests recibe un resultado de la compuerta de calidad
  o una omisión visible de ese análisis antes de poder integrarse.
- **CE-005**: Cero credenciales presentes en archivos versionados e historial, y cero credenciales
  visibles en registros de ejecución.
- **CE-006**: Un pull request desde una bifurcación sin secretos completa la compilación, las
  pruebas y los análisis que no requieren credenciales, y muestra de forma explícita las omisiones.
- **CE-007**: En condiciones normales, la verificación de compilación y pruebas entrega su resultado
  en 10 minutos o menos para el tamaño actual del repositorio.
- **CE-008**: El flujo funciona con recursos gratuitos: análisis de calidad sin licencias de pago y
  sin dependencias comerciales.
- **CE-009**: Una persona que revisa el pull request identifica el estado de compilación, pruebas,
  seguridad y calidad sin abrir la configuración del flujo.

## Fuera de alcance

Despliegue a Azure, base de datos, almacenamiento de archivos y documentación OpenAPI; cada uno vive
en su propia historia (véase `specs/000-fundacion/spec.md`). Tampoco se implementan en esta historia
las reglas de negocio del API ni se eligen aquí las versiones, sintaxis o estructura interna de los
flujos: esas decisiones corresponden al plan.

## Supuestos

- El repositorio es público y la rama principal admite protección que exige verificaciones; activar
  esa protección forma parte de la configuración del repositorio en esta historia.
- La plataforma de integración continua es la definida en la historia 000 de la fundación; esta
  especificación fija las capacidades requeridas y no elige herramientas adicionales.
- CodeQL está disponible para los lenguajes del repositorio y su uso para repositorios públicos no
  genera costo.
- SonarQube Cloud ofrece una compuerta de calidad gratuita para repositorios públicos; su token se
  almacena como credencial cifrada.
- Los ejecutores de integración continua tienen acceso a la red para restaurar dependencias, pero no
  llaman a proveedores externos reales (principio VII de la constitución).
- La verificación automática cubre los cambios de código, proyectos y configuración del flujo; no se
  promete cobertura de servicios externos que aún no existen.
- La protección de rama y las verificaciones requeridas se activan como parte de esta historia; si
  por alguna razón no estuvieran activas, el flujo informaría el resultado sin bloquear la
  integración.
- Esta es la historia 2 de la épica E0 (fundación); el detalle y los números de historia del backlog
  viven en el documento «BlackHellHeim: backlog Scrum y estructura SDD», externo al repositorio.
