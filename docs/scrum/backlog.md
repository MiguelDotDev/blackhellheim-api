# Backlog por sprint (BlackHellHeim)

Fuente: documento «BlackHellHeim: backlog Scrum y estructura SDD». Este archivo es la referencia para agentes y personas: qué historia (HU) pertenece a qué sprint, en qué repositorio se trabaja y en qué estado está.

- Estado: `Pendiente`, `Tareas listas`, `En progreso`, `Hecha`, `Por auditar`.
- Prioridad: M = imprescindible, S = deseable, C = si sobra tiempo.
- Rama de trabajo del sprint N: `sprint/NN` (ej. `sprint/01`).
- Columna Spec: carpeta `specs/NNN-slug` de la HU. Llenar al crearla. Las carpetas no siguen siempre el número de HU; la tabla manda.

## Sprint 1 · 18–24 sep · 32 h

Meta: Backend en Docker, SDD y Jira listos. Rama: `sprint/01`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 1 | api + web | DOC | 3 | M | Como desarrolladora, quiero la constitución y las plantillas de Spec Kit en ambos repos para que cada cambio nazca de una spec. | constitucion.md aprobada y /speckit.specify corre en los dos repos. |  | Hecha |
| 2 | api | BE | 9 | M | Quiero la solución .NET 10 en capas con Mediator y comportamientos de pipeline. | Compila, y una consulta de ejemplo cruza API, Aplicación y Dominio sin violar dependencias. | specs/002-estructura-base-api, specs/002b-casos-de-uso-mediator | Hecha |
| 3 | api | BE DB | 8 | M | Quiero SQL Server en Compose, EF Core, migración inicial y datos semilla. | docker compose up crea la base y aplica la migración con semillas. | specs/003-sql-server-efcore | Hecha |
| 4 | api | BE | 4 | M | Quiero OpenAPI con Scalar y errores como ProblemDetails. | /scalar muestra el contrato y un error de validación devuelve ProblemDetails. | specs/004-openapi-scalar | Hecha |
| 5 | api | BE CI | 5 | M | Quiero un Dockerfile de la API y Compose con healthchecks y secretos por variables de entorno. | API y base arrancan sanas con un comando y no hay secretos en el repo. | specs/005-dockerfile-compose | Hecha |
| 12 | api + web | DOC | 3 | M | Quiero Jira configurado con componentes, flujo y las historias cargadas. | El tablero muestra el backlog completo y el Sprint 1 activo. |  | Hecha |

## Sprint 2 · 25 sep–1 oct · 31 h

Meta: CI del backend, esqueleto Vue con ambientes, cliente y mock. Rama: `sprint/02`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 6 | api | CI | 8 | M | Quiero CI del backend con build, pruebas, SonarQube Cloud y CodeQL. | Cada push corre todo y falla si hay problemas bloqueantes. | specs/006-integracion-continua (en curso) | Por auditar |
| 7 | web | FE | 9 | M | Quiero el esqueleto Vue 3 con TypeScript, Vite, Pinia, Router, Tailwind y capas FSD. | npm run lint falla si una capa importa de otra superior. |  | Pendiente |
| 8 | web | FE | 3 | M | Quiero ambientes de desarrollo, prueba y producción. | La URL del API cambia según .env.development, .env.test y .env.production. | specs/003-sql-server-efcore | En progreso |
| 9 | web | FE | 6 | M | Quiero un cliente API tipado generado desde OpenAPI y un servidor mock. | El cliente se regenera con un comando y el mock responde un endpoint de ejemplo. |  | Pendiente |
| 10 | web | CI | 5 | M | Quiero CI del frontend con lint, tipos, pruebas y build. | Cada push corre las cuatro verificaciones. | specs/005-dockerfile-compose | En progreso |

## Sprint 3 · 2–8 oct · 31 h

Meta: Acceso con JWT de punta a punta y primer despliegue a Azure. Rama: `sprint/03`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 13 | api | BE DB | 12 | M | Como cliente, quiero registrarme e iniciar sesión con JWT de acceso corto y refresh rotativo, con roles Cliente y Administrador. | Un refresh reusado revoca la familia de tokens; pruebas de caso exitoso, error y validación. | specs/013-autenticacion-jwt | Tareas listas |
| 14 | web | FE | 9 | M | Como cliente, quiero pantallas de acceso y que mi sesión se renueve sola. | Tras caducar el acceso, la siguiente petición se repite con un token nuevo sin pedir contraseña. |  | Pendiente |
| 25 | api + web | CI | 10 | M | Quiero un primer despliegue continuo a Azure con la dirección por defecto, incluyendo el proxy de /api. | Un push a la rama principal publica API y web como contenedores; la web carga por HTTPS y /api responde a través de ella. |  | Pendiente |

## Sprint 4 · 9–15 oct · 36 h

Meta: Marca, cliente Printful, dominio de diseños y subida de arte. Rama: `sprint/04`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 11 | web | FE | 8 | M | Como cliente, quiero una marca reconocible: tokens, fuentes locales, logo y monograma. | Tokens de Tailwind, logo SVG y monograma entregados y usados en el esqueleto. |  | Pendiente |
| 15 | api | BE | 10 | M | Quiero un cliente de Printful para catálogo, variantes y disponibilidad. | Lista variantes de una playera filtradas por técnica y país; un 429 se reintenta con retry-after. |  | Pendiente |
| 16 | api | BE DB | 8 | M | Quiero el dominio de diseño, producto base y variante. | Migración aplicada; un diseño apunta a variantes de Printful y a un archivo de arte. |  | Pendiente |
| 17 | api | BE | 10 | M | Como administradora, quiero subir el arte a Azure Blob con validaciones. | Rechaza lo que no sea PNG con fondo transparente, menor de 200 MB y de resolución mínima; devuelve URL pública. |  | Pendiente |

## Sprint 5 · 16–22 oct · 30 h

Meta: API de catálogo, panel de alta de diseños y motor de precios. Rama: `sprint/05`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 19 | api | BE | 6 | M | Como visitante, quiero que la API de catálogo liste, filtre y detalle las playeras. | Listado paginado con filtros; el detalle trae imágenes, colores y tallas. |  | Pendiente |
| 18 | web | FE | 12 | M | Como administradora, quiero crear y editar diseños con varias imágenes, colores y tallas. | El formulario guarda un diseño completo y avisa si el arte negro va sobre prenda negra. |  | Pendiente |
| 20 | api | BE | 12 | M | Quiero un motor de precios con tipo de cambio, colchón y margen estático. | Para entradas dadas, el precio coincide con la fórmula y termina en 9 o 90; el tipo de cambio se guarda por día. |  | Pendiente |

## Sprint 6 · 23–29 oct · 36 h

Meta: Webhooks, mockups y catálogo visible en la web. Rama: `sprint/06`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 22 | api | BE | 8 | M | Quiero un endpoint de webhooks de Printful con firma verificada. | Una firma inválida se rechaza; un evento repetido no se procesa dos veces. |  | Pendiente |
| 21 | api | BE | 12 | M | Quiero mockups asíncronos de Printful. | Se pide la tarea, llega mockup_task_finished, hay polling de respaldo y las imágenes se copian a Blob. |  | Pendiente |
| 23 | web | FE | 16 | M | Como visitante, quiero ver el catálogo y el detalle con galería, color, talla, precio, Agregar al carrito y Comprar ahora. | Todo se ve sin cuenta; los botones respetan la variante elegida; Comprar ahora lleva al checkout. |  | Pendiente |

## Sprint 7 · 30 oct–5 nov · 33 h

Meta: Carrito, cotización de envío y checkout con sesión obligatoria. Rama: `sprint/07`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 24 | api + web | BE FE | 12 | M | Como visitante, quiero un carrito que se conserve. | Sin sesión el carrito vive en el navegador; al iniciar sesión se fusiona con el del servidor. |  | Pendiente |
| 26 | api | BE | 8 | M | Quiero cotizar el envío con Printful. | Devuelve métodos con días estimados, costo en MXN y aviso de posibles cargos aduanales. |  | Pendiente |
| 27 | web | FE | 13 | M | Como cliente, quiero un checkout con dirección mexicana, envío cotizado y resumen, y que me pida iniciar sesión si no la tengo. | Un visitante que llega al pago es enviado a iniciar sesión o registrarse y regresa al checkout con el carrito intacto; muestra el total antes de pagar. El botón Pagar se conecta en la historia 29. |  | Pendiente |

## Sprint 8 · 6–12 nov · 36 h

Meta: Pedido, pago con Stripe y borrador en Printful. Rama: `sprint/08`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 28 | api | BE DB | 12 | M | Quiero pedidos con estados e idempotencia. | Reintentar un pago no duplica el pedido; el precio queda congelado al pagar; solo usuarios autenticados crean pedidos. |  | Pendiente |
| 29 | api + web | BE FE CI | 14 | M | Como cliente, quiero pagar con Stripe. | Crea la sesión de Stripe Checkout, redirige, valida el webhook y marca el pedido pagado; los pagos diferidos (OXXO, SPEI) quedan pendientes. |  | Pendiente |
| 30 | api | BE | 10 | M | Quiero que un pago aprobado cree el borrador en Printful. | El pedido se crea como borrador con external_id; confirmar solo si la bandera real está activa. |  | Pendiente |

## Sprint 9 · 13–19 nov · 41 h

Meta: Código de creadora, seguimiento, historial y panel de pedidos. Rama: `sprint/09`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 31 | api + web | BE FE DB | 7 | M | Como administradora, quiero crear códigos de creadora y que la cliente los escriba en el checkout, para saber cuántas ventas trae cada una. | El código se valida al aplicarlo y el pedido lo guarda; no cambia el precio ni genera comisión. |  | Pendiente |
| 32 | api | BE | 8 | M | Quiero actualizar pedidos con los webhooks de envío de Printful. | El pedido pasa a enviado con número y enlace de guía. |  | Pendiente |
| 33 | web | FE | 5 | M | Como cliente, quiero ver la confirmación y el estado de mi pedido. | Muestra estado, dirección y guía del pedido de la cliente con sesión. | specs/005-dockerfile-compose | En progreso |
| 34 | api + web | BE FE | 10 | M | Como cliente, quiero mi historial de pedidos con detalle. | Lista mis pedidos paginados y abre el detalle de cada uno. |  | Pendiente |
| 38 | api + web | BE FE | 11 | M | Como administradora, quiero ver los pedidos y, por cada código de creadora, cuántas ventas tiene y la lista paginada de sus compras. | El panel muestra el total de ventas (pedidos entregados) por código y una lista paginada de las compras con ese código; los pedidos se filtran por estado y la administradora puede marcar uno como entregado. |  | Pendiente |

## Sprint 10 · 20–26 nov · 36 h

Meta: Notificaciones, push, reseñas y lista de deseos. Rama: `sprint/10`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 35 | api + web | BE FE | 16 | S | Como cliente, quiero una bandeja de notificaciones y avisos push. | Un cambio de estado crea el aviso en la bandeja y llega como push a Android si hay permiso. |  | Pendiente |
| 36 | api + web | BE FE | 6 | C | Como cliente, quiero una lista de deseos. | Agregar y quitar playeras; solo con sesión. |  | Pendiente |
| 37 | api + web | BE FE | 14 | S | Como cliente, quiero reseñas con estrellas y comentarios. | Solo compradoras verificadas reseñan; la administradora puede ocultar contenido. |  | Pendiente |

## Sprint 11 · 27 nov–4 dic · 40 h

Meta: PWA sin conexión, producción, pruebas de punta a punta y entrega. Rama: `sprint/11`.

| HU | Repos | Comp. | Horas | Prio | Historia | Criterio de aceptación | Spec | Estado |
|---|---|---|---|---|---|---|---|---|
| 39 | web | FE | 14 | M | Como cliente, quiero instalar la app y ver mis compras, avisos y parte del catálogo sin conexión. | Instalable en Android; en modo avión abre y muestra pedidos, notificaciones y catálogo visto. |  | Pendiente |
| 40 | api | CI | 6 | M | Quiero producción estable en Azure con HTTPS para webhooks. | Stripe y Printful llegan a la URL pública de la API; las migraciones se aplican desde el pipeline. |  | Pendiente |
| 41 | web | FE | 4 | S | Quiero accesibilidad y rendimiento revisados. | Lighthouse sin fallas críticas de accesibilidad; contrastes validados. | specs/004-openapi-scalar | En progreso |
| 42 | api + web | BE FE | 7 | M | Quiero el flujo completo probado y un pedido real de demo. | Compra de prueba de punta a punta con sesión; la confirmación real en Printful es opcional. |  | Pendiente |
| 43 | api | DB | 2 | M | Quiero demostrar aplicar y revertir una migración. | Se aplica y revierte una migración en vivo sin perder datos de prueba. |  | Pendiente |
| 44 | api + web | DOC | 3 | M | Quiero specs diferidos de aviso de privacidad y CFDI con plantillas. | Existen las carpetas y plantillas marcadas como fuera de alcance. | specs/003-sql-server-efcore | En progreso |
| 45 | api + web | DOC | 4 | M | Quiero la documentación final y la evidencia de cada sprint. | README, evidencias SDD, revisiones y retrospectivas en ambos repos. | specs/004-openapi-scalar | En progreso |

Total: 382 h.
