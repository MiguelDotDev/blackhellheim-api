using System.Text.RegularExpressions;
using Xunit;

namespace BlackHellHeim.Pruebas.Entorno;

/// <summary>Pruebas estáticas del <c>Dockerfile</c> (historia 005).</summary>
public partial class DockerfilePruebas
{
    private static string Dockerfile() => RaizRepositorio.Leer("Dockerfile");

    /// <summary>Instrucciones de la etapa, desde su FROM hasta el siguiente FROM.</summary>
    private static string Etapa(string nombre)
    {
        var texto = Dockerfile();
        var inicio = Regex.Match(texto, $@"^FROM .* AS {nombre}\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        Assert.True(inicio.Success, $"El Dockerfile no define la etapa '{nombre}'.");
        var siguiente = Regex.Match(texto[(inicio.Index + inicio.Length)..], @"^FROM ", RegexOptions.Multiline);
        return siguiente.Success
            ? texto.Substring(inicio.Index, inicio.Length + siguiente.Index)
            : texto[inicio.Index..];
    }

    [Fact]
    public void DefineLasCuatroEtapas()
    {
        foreach (var etapa in new[] { "restauracion", "compilacion", "migraciones", "final" })
        {
            Etapa(etapa);
        }
    }

    [Fact]
    public void LasImagenesBaseSeFijanPorDigest()
    {
        var texto = Dockerfile();
        var imagenes = ImagenBase().Matches(texto).Select(m => m.Groups[1].Value).ToArray();

        Assert.Equal(3, imagenes.Length);
        Assert.All(imagenes, imagen => Assert.Matches(@"@sha256:[0-9a-f]{64}$", imagen));
        // Todo FROM usa un ARG de imagen o una etapa anterior; nunca una etiqueta suelta.
        foreach (Match from in Regex.Matches(texto, @"^FROM (\S+)", RegexOptions.Multiline))
        {
            var origen = from.Groups[1].Value;
            Assert.True(origen.StartsWith("${IMAGEN_", StringComparison.Ordinal) || Regex.IsMatch(origen, "^(restauracion|compilacion)$"),
                $"FROM sin digest ni etapa: {origen}");
        }
    }

    [Fact]
    public void LaRestauracionUsaModoBloqueado()
    {
        Assert.Contains("--locked-mode", Etapa("restauracion"));
    }

    [Fact]
    public void LaEtapaFinalNoUsaElSdkNiCopiaTodoElContexto()
    {
        var final = Etapa("final");

        Assert.Contains("${IMAGEN_ASPNET}", final);
        Assert.DoesNotContain("IMAGEN_SDK", final);
        Assert.DoesNotMatch(@"COPY\s+(--\S+\s+)*\.\s+\.", final);
        Assert.Contains("--from=compilacion", final);
    }

    [Fact]
    public void LaEtapaFinalCorreSinPrivilegiosEnProduccion()
    {
        foreach (var etapa in new[] { "migraciones", "final" })
        {
            var usuarios = Regex.Matches(Etapa(etapa), @"^USER (\S+)", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToArray();
            Assert.NotEmpty(usuarios);
            Assert.DoesNotContain(usuarios.Last(), new[] { "root", "0" });
        }

        Assert.Contains("ENV ASPNETCORE_ENVIRONMENT=Production", Etapa("final"));
    }

    [Fact]
    public void LaPublicacionEliminaLaConfiguracionDeDesarrollo()
    {
        Assert.Contains("appsettings.Development.json", Etapa("compilacion"));
    }

    [Fact]
    public void LaVariableFicticiaDelBundleNoSePropagaNiContieneCredenciales()
    {
        var texto = Dockerfile();

        Assert.DoesNotMatch(@"^\s*(ENV|ARG)\s+ConnectionStrings", texto);
        Assert.DoesNotMatch("(?i)Password=", texto);
        Assert.DoesNotContain("ConnectionStrings__BlackHellHeim", Etapa("final"));
    }

    [GeneratedRegex(@"^ARG IMAGEN_\w+=(\S+)", RegexOptions.Multiline)]
    private static partial Regex ImagenBase();
}
