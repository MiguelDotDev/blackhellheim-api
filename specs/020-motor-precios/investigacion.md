# Investigación 020: API SIE de Banxico (T002)

- **Ruta**: `GET https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43718/datos/oportuno`. Verificada el 2026-11-11 con una petición sin token: la ruta existe y responde 400 con `{"error":{"mensaje":"Token inválido",...}}`, así que exige token.
- **Autenticación**: cabecera `Bmx-Token` (token gratuito que se solicita en el sitio de Banxico). `[MANUAL]` obtenerlo.
- **Respuesta esperada** (forma documentada por Banxico, **no verificada con token real**): `{"bmx":{"series":[{"idSerie":"SF43718","titulo":"…","datos":[{"fecha":"dd/MM/yyyy","dato":"18.4321"}]}]}}`; `dato` puede ser `"N/E"` (no disponible), que el adaptador trata como «sin dato». Las pruebas usan JSON de ejemplo con esa forma, sin red.
- **Fin de semana/festivo**: `oportuno` devuelve el último dato publicado (fecha anterior); el servicio lo guarda con esa fecha y lo marca como reserva.
- **Términos de uso**: la API es de acceso libre con token, con límite de consultas diarias de Banxico; el servicio consulta como máximo una vez por día hábil. `[MANUAL]` revisar los términos vigentes en el sitio de Banxico.
- **Licencia/dependencias**: ninguna dependencia nueva (solo `HttpClient`).
