# Inicio rápido: Casos de uso con CQRS, mediador y pipeline

Historia 002b · Fecha: 2026-11-11

Guía para comprobar de punta a punta la consulta de ejemplo, la validación, el registro y
el formato uniforme de errores. No requiere base de datos ni proveedores externos.

## Requisitos

- SDK de .NET 10.
- Repositorio en la rama `sprint/01`.
- Ningún secreto ni variable de entorno adicional.

## 1. Compilar y probar

```bash
dotnet build BlackHellHeim.sln
dotnet test BlackHellHeim.sln
```

Resultado esperado:

- Compilación sin errores ni advertencias.
- Todas las pruebas en verde, incluidas las de arquitectura existentes.

## 2. Arrancar la API

```bash
dotnet run --project src/Api --launch-profile http
```

La API escucha en `http://localhost:5068`.

## 3. Consulta exitosa

```bash
curl -i http://localhost:5068/tiendas/BHH
```

Resultado esperado:

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-ID: <identificador generado>

{"codigo":"BHH","nombre":"BlackHellHeim","moneda":"MXN"}
```

## 4. Validación antes del manejador

```bash
curl -i http://localhost:5068/tiendas/B
```

Resultado esperado:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
X-Correlation-ID: <identificador generado>

{
  "type": "https://blackhellheim.mx/errores/solicitud_invalida",
  "title": "La solicitud no es válida",
  "status": 400,
  "detail": "Revisa los campos indicados.",
  "instance": "/tiendas/B",
  "codigo": "solicitud_invalida",
  "idCorrelacion": "<el mismo de la cabecera>",
  "errores": {
    "codigo": ["El código debe tener exactamente tres letras mayúsculas."]
  }
}
```

El manejador no se ejecuta: el rechazo ocurre en el comportamiento de validación.

## 5. Recurso no encontrado

```bash
curl -i http://localhost:5068/tiendas/ZZZ
```

Resultado esperado: `404` con ProblemDetails, `codigo` igual a
`recurso_no_encontrado` y `idCorrelacion` presente. No es un error no controlado.

## 6. Correlación aportada por el cliente

```bash
curl -i -H "X-Correlation-ID: prueba-123" http://localhost:5068/tiendas/BHH
```

Resultado esperado: la respuesta conserva `X-Correlation-ID: prueba-123` y, si hay error,
el cuerpo usa ese mismo identificador.

## 7. Comprobación de estado existente

```bash
curl -i http://localhost:5068/salud
```

Resultado esperado: `200` con `{"estado":"ok"}`, sin cambios respecto de la historia 002.

## 8. Registro de ejecución

Con el nivel de registro `Information` (el de `appsettings.json`), cada ejecución deja una
entrada con:

- tipo de solicitud (`ConsultarTiendaQuery`);
- resultado (éxito o fallo);
- duración en milisegundos;
- identificador de correlación;
- código de error cuando falla.

El registro no incluye el código consultado ni otros datos de la solicitud. Un fallo del
registro no cambia la respuesta del caso de uso.

## 9. Qué revisar en el código

- `src/Aplicacion/Comun/Comportamientos/`: orden del pipeline y responsabilidades.
- `src/Aplicacion/Tiendas/ConsultarTienda/`: consulta, validador y manejador.
- `src/Api/Comun/MiddlewareCorrelacionYErrores.cs`: correlación y ProblemDetails.
- `src/Pruebas/`: pruebas de éxito, error y validación de cada comportamiento.

## Solución de problemas

| Síntoma | Causa probable | Acción |
|---------|----------------|--------|
| `MissingMessageHandlerException` | El manejador no se generó o el generador no está en `Api` | Verificar `Mediator.SourceGenerator` en `src/Api` y compilar de nuevo |
| `Unable to resolve service for type 'IContextoEjecucion'` | Falta el registro con ámbito | Revisar `RegistroServicios` |
| La validación no se ejecuta | El comportamiento no está registrado o el orden es incorrecto | Revisar el orden en `RegistroServicios` |
| `X-Correlation-ID` no aparece | El middleware no está antes del endpoint | Revisar `Program.cs` |
