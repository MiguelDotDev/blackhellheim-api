# Plan 020: Motor de precios

**Spec**: [spec.md](spec.md) · **Constitución**: III, V, VII. **Dependencias nuevas**: ninguna (el cliente de Banxico usa `HttpClient`).

## Resumen
`CalculadoraPrecios` pura en Dominio (`decimal`), `RedondeoComercial` aparte; puerto `IProveedorTipoCambio` con adaptador Banxico (SIE, serie FIX `SF43718`, cabecera `Bmx-Token`); servicio de aplicación `ObtenerTipoCambioDelDia` con persistencia por día y reserva; puerto de consumo `ICalculadoraPrecios`.

## Decisiones
1. **Redondeo**: dado el bruto `b`, sea `c1` = menor entero ≥ `b` con terminación 9 (`⌈(b−9)/10⌉·10+9`) y `c2` = menor múltiplo de 100 menos 10 ≥ `b` (terminación 90); precio = `min(c1, c2)`. Cubierto por tabla de casos; esto resuelve la ambigüedad del criterio «9 o 90» y se documenta.
2. **Concurrencia**: inserción con índice único + captura de violación (otra petición ganó) y relectura.
3. **Fecha**: `TimeZoneInfo` de Ciudad de México.
4. **Banxico**: `GET https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43718/datos/oportuno` con token `Banxico__Token`; verificar ruta y términos de uso en T002. Sin dependencias.
5. **Trabajo programado**: se obtiene bajo demanda (primer cálculo del día); sin servicios en segundo plano.
6. Parámetros leídos con caché de 1 min; escribir invalida la caché.

## Estructura
`Dominio/Precios/{CalculadoraPrecios,RedondeoComercial,ParametrosPrecio}`, `Aplicacion/Precios/{ICalculadoraPrecios,IProveedorTipoCambio,ObtenerTipoCambioDelDia,Simular,ActualizarParametros}`, `Infraestructura/Precios/{ProveedorTipoCambioBanxico,RepositorioPrecios}`, `Api/Endpoints/PreciosAdminEndpoints.cs`.

## Pruebas
Tabla de ≥20 casos (xUnit `Theory`) para la fórmula y el redondeo; manejadores con doble del proveedor (éxito, falla→reserva, rango ±20 %, sin dato→503); integración de unicidad por día y concurrencia; endpoints y contrato.

## Bloqueos y [MANUAL]
HU-16 para el costo base. `[MANUAL]` solicitar token de Banxico (gratuito) y guardarlo como `Banxico__Token`; confirmar los parámetros iniciales.
