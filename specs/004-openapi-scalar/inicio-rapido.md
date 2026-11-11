# Inicio rápido: Contrato OpenAPI publicado y documentación interactiva

Historia 004 · Fecha: 2026-11-11

Guía para comprobar de punta a punta el documento publicado, la página de Scalar, el formato
uniforme de errores y la verificación de contrato.

## Requisitos

- SDK de .NET 10.
- Repositorio en la rama `sprint/01`.
- Historias 002, 002b y 003 implementadas: aportan `/salud`, `/tiendas/{codigo}` y
  `/salud/base`. Sin ellas, la prueba de contrato falla porque el documento publicado no
  tendría esas operaciones.
- No se requiere Docker ni base de datos para esta historia. Si 003 valida la cadena de
  conexión al arrancar, define una cadena ficticia:

```bash
export ConnectionStrings__BlackHellHeim='Server=127.0.0.1,1;Database=BlackHellHeim;User Id=sa;Password=__SOLO_PRUEBA__;Encrypt=True;TrustServerCertificate=True'
```

## 1. Compilar y probar

```bash
dotnet build BlackHellHeim.sln
dotnet test BlackHellHeim.sln
```

Resultado esperado:

- Compilación sin errores ni advertencias nuevas.
- Todas las pruebas en verde, incluidas las de arquitectura, contrato, documento, Scalar y
  errores uniformes.

## 2. Arrancar la API

```bash
dotnet run --project src/Api --launch-profile http
```

La API escucha en `http://localhost:5068`.

## 3. Descargar el documento publicado

```bash
curl -s http://localhost:5068/openapi/v1.json | python3 -m json.tool | head -n 40
```

Resultado esperado:

```json
{
  "openapi": "3.1.1",
  "info": {
    "title": "BlackHellHeim API",
    "version": "..."
  },
  "paths": {
    "/salud": { "...": "..." },
    "/tiendas/{codigo}": { "...": "..." },
    "/salud/base": { "...": "..." }
  }
}
```

Comprueba que cada operación tiene una respuesta `500` con
`application/problem+json` y `$ref` a `ProblemDetails`, y que el documento no contiene
`/openapi` ni `/scalar`.

## 4. Abrir la página interactiva

Abre en el navegador:

```text
http://localhost:5068/scalar
```

Resultado esperado:

- La ruta redirige a `/scalar/` y muestra la página de Scalar.
- El título es **BlackHellHeim API**.
- El listado incluye `/salud`, `/tiendas/{codigo}` y `/salud/base`.
- Al ejecutar `/salud` desde la página, se ve la respuesta real `{"estado":"ok"}`.

La página usa activos locales del paquete; no depende de un CDN.

## 5. Error de validación

```bash
curl -i http://localhost:5068/tiendas/B
```

Resultado esperado:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
X-Correlation-ID: <identificador>

{"type":"https://blackhellheim.mx/errores/solicitud_invalida","title":"La solicitud no es válida","status":400,"detail":"Revisa los campos indicados.","instance":"/tiendas/B","codigo":"solicitud_invalida","idCorrelacion":"<identificador>","errores":{"codigo":["El código debe tener exactamente tres letras mayúsculas."]}}
```

## 6. Error inesperado

El endpoint que lanza una excepción solo existe en el entorno `Pruebas` y está excluido del
documento. Arranca la API en ese entorno:

```bash
ASPNETCORE_ENVIRONMENT=Pruebas ASPNETCORE_URLS=http://localhost:5068 \
  dotnet run --project src/Api --no-launch-profile
curl -i http://localhost:5068/pruebas/error
```

`--no-launch-profile` es necesario porque el perfil `http` fija el entorno en `Development`
y ocultaría el endpoint de prueba.

Resultado esperado:

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json
X-Correlation-ID: <identificador>

{"type":"https://blackhellheim.mx/errores/error_inesperado","title":"Error interno","status":500,"detail":"Ocurrió un error inesperado. Usa el identificador de correlación para dar seguimiento.","instance":"/pruebas/error","codigo":"error_inesperado","idCorrelacion":"<identificador>"}
```

Comprueba que:

- el tipo de contenido y el esquema son los mismos que en el error de validación;
- no aparece `traceId`, ni una traza, ni rutas internas, ni la cadena de conexión;
- `idCorrelacion` está en el cuerpo y en la cabecera.

## 7. Verificación de contrato

```bash
dotnet test --filter "FullyQualifiedName~ContratoOpenApi"
```

Resultado esperado: la prueba pasa y no reporta diferencias. La prueba lee todos los
`specs/*/contratos/openapi.yaml`, los compone y los compara con el documento publicado en
`/openapi/v1.json`.

### Prueba negativa

Cambia temporalmente una respuesta de un contrato (por ejemplo, el estado 200 de `/salud`
por 201), ejecuta de nuevo la prueba y comprueba que falla indicando la operación o el
esquema distinto. Restaura el contrato y confirma que vuelve a pasar. No dejes el cambio.

## 8. Documento y contrato en el mismo estado

```bash
curl -s http://localhost:5068/openapi/v1.json | python3 -c "import sys,json; d=json.load(sys.stdin); print(sorted(d['paths']))"
```

Resultado esperado:

```text
['/salud', '/salud/base', '/tiendas/{codigo}']
```

Si falta una operación o aparece una que no está en los contratos, la prueba de contrato
falla y señala la diferencia.

## Problemas frecuentes

| Síntoma | Causa probable | Acción |
|---------|----------------|--------|
| `/openapi/v1.json` responde 404 | El documento no se registró o la ruta cambió | Revisa `AddOpenApi` y `MapOpenApi` en `Program.cs` |
| `/scalar` responde 404 | La página no se registró | Revisa `MapScalarApiReference` y el paquete `Scalar.AspNetCore` |
| La página carga sin operaciones | El documento no responde o la ruta no coincide | Abre `/openapi/v1.json` y revisa la consola del navegador |
| La prueba de contrato falla por `/tiendas` o `/salud/base` | Falta implementar 002b o 003 | Implementa esas historias antes de 004 |
| La prueba de contrato falla por `ProblemDetails` | Una copia del esquema se desalineó | Iguala la copia al esquema canónico de 004 |
| El error inesperado responde `application/json` | Se escribió con `WriteAsJsonAsync` en lugar de `IProblemDetailsService` | Usa el servicio estándar y `UseExceptionHandler` |
| Aparece `traceId` en el error | No se configuró `CustomizeProblemDetails` | Quita `traceId` en las opciones de ProblemDetails |
| La prueba de contrato falla por la versión `openapi` | Un contrato quedó en `3.1.0` | Actualízalo a `3.1.1` |
