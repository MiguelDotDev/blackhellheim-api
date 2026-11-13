using System.Globalization;
using System.Text;

namespace BlackHellHeim.Dominio.Disenos;

public enum EstadoDiseno { Borrador, Publicado, Archivado }

/// <summary>Regla de negocio incumplida (se traduce a 409 en la API). El código es estable.</summary>
public sealed class ReglaDeNegocioException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

/// <summary>Referencia al arte subido (la tabla la llena la historia 017).</summary>
public sealed class ArchivoArte
{
    private ArchivoArte() { }

    public Guid Id { get; private set; }
    public string Url { get; private set; } = "";
    public string NombreBlob { get; private set; } = "";
    public string TipoContenido { get; private set; } = "";
    public long TamanoBytes { get; private set; }
    public int Ancho { get; private set; }
    public int Alto { get; private set; }
    public string HashSha256 { get; private set; } = "";
    public DateTimeOffset CreadoUtc { get; private set; }

    public static ArchivoArte Crear(string url, string nombreBlob, string tipoContenido, long tamanoBytes,
        int ancho, int alto, string hashSha256, DateTimeOffset ahora) =>
        new()
        {
            Id = Guid.NewGuid(), Url = url, NombreBlob = nombreBlob, TipoContenido = tipoContenido,
            TamanoBytes = tamanoBytes, Ancho = ancho, Alto = alto, HashSha256 = hashSha256, CreadoUtc = ahora
        };
}

public sealed class ProductoBase
{
    private ProductoBase() { }

    public Guid Id { get; private set; }
    public int PrintfulProductoId { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Tipo { get; private set; } = "";

    public static ProductoBase Crear(int printfulProductoId, string nombre, string tipo)
    {
        if (printfulProductoId < 1) throw new ArgumentOutOfRangeException(nameof(printfulProductoId));
        return new ProductoBase { Id = Guid.NewGuid(), PrintfulProductoId = printfulProductoId, Nombre = nombre.Trim(), Tipo = tipo.Trim() };
    }
}

public sealed class Variante
{
    private Variante() { }

    public Guid Id { get; private set; }
    public Guid ProductoBaseId { get; private set; }
    public ProductoBase ProductoBase { get; private set; } = null!;
    public int PrintfulVarianteId { get; private set; }
    public string Color { get; private set; } = "";
    public string CodigoColor { get; private set; } = "";
    public string Talla { get; private set; } = "";
    /// <summary>Costo base en USD; el precio de venta nunca se guarda aquí (principio V).</summary>
    public decimal CostoBaseUsd { get; private set; }
    public bool Activa { get; private set; } = true;

    public static Variante Crear(ProductoBase producto, int printfulVarianteId, string color, string codigoColor,
        string talla, decimal costoBaseUsd)
    {
        if (printfulVarianteId < 1) throw new ArgumentOutOfRangeException(nameof(printfulVarianteId));
        if (costoBaseUsd < 0) throw new ArgumentOutOfRangeException(nameof(costoBaseUsd));
        return new Variante
        {
            Id = Guid.NewGuid(), ProductoBase = producto, ProductoBaseId = producto.Id, PrintfulVarianteId = printfulVarianteId,
            Color = color.Trim(), CodigoColor = codigoColor.Trim(), Talla = talla.Trim(), CostoBaseUsd = costoBaseUsd
        };
    }

    public void ActualizarDatos(string color, string codigoColor, string talla, decimal costoBaseUsd)
    {
        if (costoBaseUsd < 0) throw new ArgumentOutOfRangeException(nameof(costoBaseUsd));
        Color = color.Trim();
        CodigoColor = codigoColor.Trim();
        Talla = talla.Trim();
        CostoBaseUsd = costoBaseUsd;
    }
}

public sealed class Diseno
{
    public const int NombreMinimo = 3, NombreMaximo = 80, DescripcionMaxima = 2000;
    private readonly List<Variante> _variantes = [];

    private Diseno() { }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public string? Descripcion { get; private set; }
    public EstadoDiseno Estado { get; private set; }
    public Guid? ArchivoArteId { get; private set; }
    public ArchivoArte? ArchivoArte { get; private set; }
    public byte[] Version { get; private set; } = [];
    public DateTimeOffset CreadoUtc { get; private set; }
    public DateTimeOffset ActualizadoUtc { get; private set; }
    public IReadOnlyList<Variante> Variantes => _variantes;

    public static Diseno Crear(string nombre, string? descripcion, DateTimeOffset ahora)
    {
        var diseno = new Diseno { Id = Guid.NewGuid(), Estado = EstadoDiseno.Borrador, CreadoUtc = ahora, ActualizadoUtc = ahora };
        diseno.EditarDatos(nombre, descripcion, ahora);
        return diseno;
    }

    public void EditarDatos(string nombre, string? descripcion, DateTimeOffset ahora)
    {
        var limpio = (nombre ?? "").Trim();
        if (limpio.Length is < NombreMinimo or > NombreMaximo)
            throw new ArgumentException($"El nombre debe tener entre {NombreMinimo} y {NombreMaximo} caracteres.", nameof(nombre));
        if (descripcion is { Length: > DescripcionMaxima })
            throw new ArgumentException($"La descripción no puede superar {DescripcionMaxima} caracteres.", nameof(descripcion));
        var slug = GenerarSlug(limpio);
        if (slug.Length == 0) throw new ArgumentException("El nombre debe contener letras o números.", nameof(nombre));
        Nombre = limpio;
        Slug = slug;
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        ActualizadoUtc = ahora;
    }

    public void AsignarArte(ArchivoArte arte, DateTimeOffset ahora)
    {
        ArchivoArte = arte;
        ArchivoArteId = arte.Id;
        ActualizadoUtc = ahora;
    }

    /// <summary>Reemplaza las variantes; todas deben ser del mismo producto base y un diseño publicado no puede quedar sin ninguna.</summary>
    public void ReemplazarVariantes(IEnumerable<Variante> variantes, DateTimeOffset ahora)
    {
        var lista = variantes.DistinctBy(v => v.PrintfulVarianteId).ToList();
        if (lista.Select(v => v.ProductoBase.PrintfulProductoId).Distinct().Count() > 1)
            throw new ReglaDeNegocioException("variantes_de_varios_productos", "Todas las variantes de un diseño deben ser del mismo producto base.");
        if (Estado == EstadoDiseno.Publicado && !lista.Any(v => v.Activa))
            throw new ReglaDeNegocioException("publicado_sin_variantes", "Un diseño publicado necesita al menos una variante activa.");
        _variantes.Clear();
        _variantes.AddRange(lista);
        ActualizadoUtc = ahora;
    }

    public void CambiarEstado(EstadoDiseno nuevo, DateTimeOffset ahora)
    {
        if (nuevo == Estado) return;
        var permitido = (Estado, nuevo) is (EstadoDiseno.Borrador, EstadoDiseno.Publicado) or (EstadoDiseno.Borrador, EstadoDiseno.Archivado)
            or (EstadoDiseno.Publicado, EstadoDiseno.Archivado) or (EstadoDiseno.Archivado, EstadoDiseno.Publicado);
        if (!permitido)
            throw new ReglaDeNegocioException("transicion_invalida", $"No se puede pasar de {Estado} a {nuevo}.");
        if (nuevo == EstadoDiseno.Publicado)
        {
            if (ArchivoArteId is null) throw new ReglaDeNegocioException("publicar_sin_arte", "Para publicar el diseño necesita un archivo de arte.");
            if (!_variantes.Any(v => v.Activa)) throw new ReglaDeNegocioException("publicado_sin_variantes", "Para publicar el diseño necesita al menos una variante activa.");
        }

        Estado = nuevo;
        ActualizadoUtc = ahora;
    }

    /// <summary>Minúsculas, sin acentos, solo letras, números y guiones.</summary>
    public static string GenerarSlug(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }

        return sb.ToString().Trim('-');
    }
}
