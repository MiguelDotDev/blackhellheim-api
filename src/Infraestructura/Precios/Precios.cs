using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BlackHellHeim.Aplicacion.Precios;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Dominio.Precios;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackHellHeim.Infraestructura.Precios;

public sealed class OpcionesBanxico
{
    public string Token { get; set; } = "";
    public string Url { get; set; } = "https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43718/datos/oportuno";
}

/// <summary>Banxico SIE, serie SF43718 (FIX). Devuelve null si el dato es «N/E» o no hay datos.</summary>
public sealed class ProveedorTipoCambioBanxico(HttpClient http, OpcionesBanxico opciones) : IProveedorTipoCambio
{
    public string Fuente => "Banxico FIX (SF43718)";

    public async Task<TasaProveedor?> ObtenerAsync(CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(opciones.Token)) throw new InvalidOperationException("Falta Banxico:Token.");
        using var peticion = new HttpRequestMessage(HttpMethod.Get, opciones.Url);
        peticion.Headers.Add("Bmx-Token", opciones.Token);
        peticion.Headers.Accept.ParseAdd("application/json");
        using var respuesta = await http.SendAsync(peticion, cancelacion);
        respuesta.EnsureSuccessStatusCode();
        return Mapear(await respuesta.Content.ReadFromJsonAsync<RespuestaBanxico>(cancelacion));
    }

    /// <summary>Convierte el JSON de la API SIE; público para probarlo sobre JSON de ejemplo sin red.</summary>
    public static TasaProveedor? MapearJson(string json) => Mapear(System.Text.Json.JsonSerializer.Deserialize<RespuestaBanxico>(json));

    private static TasaProveedor? Mapear(RespuestaBanxico? r)
    {
        var dato = r?.Bmx?.Series?.FirstOrDefault()?.Datos?.FirstOrDefault();
        if (dato?.Fecha is null || dato.Dato is null) return null;
        if (!decimal.TryParse(dato.Dato, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) || valor <= 0) return null;
        return DateOnly.TryParseExact(dato.Fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? new TasaProveedor(fecha, valor) : null;
    }

    private sealed record RespuestaBanxico([property: JsonPropertyName("bmx")] CuerpoBanxico? Bmx);

    private sealed record CuerpoBanxico([property: JsonPropertyName("series")] List<SerieBanxico>? Series);

    private sealed record SerieBanxico([property: JsonPropertyName("datos")] List<DatoBanxico>? Datos);

    private sealed record DatoBanxico(
        [property: JsonPropertyName("fecha")] string? Fecha, [property: JsonPropertyName("dato")] string? Dato);
}

public sealed class RepositorioPrecios(ContextoBlackHellHeim contexto) : IRepositorioPrecios
{
    public Task<TipoCambioDiario?> ObtenerTipoCambioAsync(DateOnly fecha, CancellationToken cancelacion) =>
        contexto.TiposCambioDiarios.AsNoTracking().FirstOrDefaultAsync(t => t.Fecha == fecha, cancelacion);

    public Task<TipoCambioDiario?> ObtenerUltimoTipoCambioAsync(CancellationToken cancelacion) =>
        contexto.TiposCambioDiarios.AsNoTracking().OrderByDescending(t => t.Fecha).FirstOrDefaultAsync(cancelacion);

    public async Task<bool> GuardarTipoCambioAsync(TipoCambioDiario tasa, CancellationToken cancelacion)
    {
        contexto.TiposCambioDiarios.Add(tasa);
        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            contexto.Entry(tasa).State = EntityState.Detached;
            return false;
        }
    }

    public Task<ParametrosPrecio> ObtenerParametrosVigentesAsync(CancellationToken cancelacion) =>
        contexto.ParametrosPrecio.AsNoTracking().OrderByDescending(p => p.VigenteDesdeUtc).FirstAsync(cancelacion);

    public async Task AgregarParametrosAsync(ParametrosPrecio parametros, CancellationToken cancelacion)
    {
        contexto.ParametrosPrecio.Add(parametros);
        await contexto.SaveChangesAsync(cancelacion);
    }

    public async Task<decimal?> ObtenerCostoBaseVarianteAsync(Guid varianteId, CancellationToken cancelacion) =>
        await contexto.Variantes.AsNoTracking().Where(v => v.Id == varianteId).Select(v => (decimal?)v.CostoBaseUsd).FirstOrDefaultAsync(cancelacion);
}

public sealed class TipoCambioDiarioConfiguracion : IEntityTypeConfiguration<TipoCambioDiario>
{
    public void Configure(EntityTypeBuilder<TipoCambioDiario> c)
    {
        c.ToTable("TiposCambioDiarios", "dbo");
        c.HasKey(t => t.Id);
        c.Property(t => t.Id).ValueGeneratedNever();
        c.Property(t => t.Valor).HasPrecision(10, 4);
        c.Property(t => t.Fuente).IsRequired().HasMaxLength(60);
        c.HasIndex(t => t.Fecha).IsUnique();
    }
}

public sealed class ParametrosPrecioConfiguracion : IEntityTypeConfiguration<ParametrosPrecio>
{
    public static readonly Guid IdSemilla = new("0b7f3c1e-5a0d-4a53-9c1e-7d0f6b2a4e11");

    public void Configure(EntityTypeBuilder<ParametrosPrecio> c)
    {
        c.ToTable("ParametrosPrecio", "dbo");
        c.HasKey(p => p.Id);
        c.Property(p => p.Id).ValueGeneratedNever();
        c.Property(p => p.ColchonCambiario).HasPrecision(5, 4);
        c.Property(p => p.Margen).HasPrecision(5, 4);
        c.Property(p => p.ComisionPasarela).HasPrecision(5, 4);
        c.Property(p => p.ImpresionExtraUsd).HasPrecision(10, 2);
        c.Property(p => p.ActualizadoPor).IsRequired().HasMaxLength(100);
        c.HasIndex(p => p.VigenteDesdeUtc);
        // Valores iniciales propuestos (spec 020); [MANUAL] confirmarlos y cambiarlos con PUT /admin/precios/parametros.
        c.HasData(new
        {
            Id = IdSemilla, ColchonCambiario = 0.03m, Margen = 0.40m, ComisionPasarela = 0.036m, ImpresionExtraUsd = 0m,
            VigenteDesdeUtc = new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero), ActualizadoPor = "semilla"
        });
    }
}
