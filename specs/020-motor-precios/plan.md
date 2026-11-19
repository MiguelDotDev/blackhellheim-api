# Plan 020: Motor de precios

**Spec**: [spec.md](spec.md) · **Constitución**: III, V, VII. **Dependencias nuevas**: ninguna (el cliente de Banxico usa `HttpClient`).

## Resumen
`CalculadoraPrecios` pura en Dominio (`decimal`), `RedondeoComercial` aparte; puerto `IProveedorTipoCambio` con adaptador Banxico (SIE, serie FIX `SF43718`, cabecera `Bmx-Token`); servicio de aplicación `ObtenerTipoCambioDelDia` con persistencia por día y reserva; puerto de consumo `ICalculadoraPrecios`.

## Decisiones
1. **Redondeo** (decisión 2026-11-11): entero terminado en 9, siempre hacia arriba: precio = `⌈(b−9)/10⌉·10+9` (si b ≤ 9, 9), sobre `decimal`. Sin parámetro de formato.
2. **Concurrencia**: inserción con índice único + captura de violación (otra petición ganó) y relectura.
3. **Fecha**: `TimeZoneInfo` de Ciudad de México.
4. **Banxico**: `GET https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43718/datos/oportuno` con token `Banxico__Token`; verificar ruta y términos de uso en T002. Sin dependencias.
5. **Trabajo programado**: se obtiene bajo demanda (primer cálculo del día); sin servicios en segundo plano.
6. Parámetros leídos de la base en cada cálculo (consulta indexada por `VigenteDesdeUtc`); sin caché: se agrega si se mide un problema.
7. El tipo de cambio se guarda con la fecha que reporta el proveedor; si esa fecha no es la de hoy (CDMX) el resultado se marca `EsReserva`. Límite: en fin de semana se consulta al proveedor en cada petición hasta que publique el dato del día.

## Estructura
`Dominio/Precios/{CalculadoraPrecios,RedondeoComercial,ParametrosPrecio}`, `Aplicacion/Precios/{ICalculadoraPrecios,IProveedorTipoCambio,ObtenerTipoCambioDelDia,Simular,ActualizarParametros}`, `Infraestructura/Precios/{ProveedorTipoCambioBanxico,RepositorioPrecios}`, `Api/Endpoints/PreciosAdminEndpoints.cs`.

## Pruebas
Tabla de ≥20 casos (xUnit `Theory`) para la fórmula y el redondeo; manejadores con doble del proveedor (éxito, falla→reserva, rango ±20 %, sin dato→503); integración de unicidad por día y concurrencia; endpoints y contrato.

## Bloqueos y [MANUAL]
HU-16 para el costo base. `[MANUAL]` solicitar token de Banxico (gratuito) y guardarlo como `Banxico__Token`; confirmar los parámetros iniciales.
