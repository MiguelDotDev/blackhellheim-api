using BlackHellHeim.Aplicacion.Disenos;
using BlackHellHeim.Dominio.Disenos;
using BlackHellHeim.Infraestructura.Disenos;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Persistencia;

[Collection(ColeccionSqlServer.Nombre)]
public class DisenosPersistenciaPruebas(ContenedorSqlServer sql)
{
    private static readonly DateTimeOffset Ahora = DateTimeOffset.UtcNow;

    private async Task<(string Cadena, Func<ContextoBlackHellHeim> Nuevo)> PrepararAsync()
    {
        var cadena = await sql.CrearBaseAsync();
        ContextoBlackHellHeim Nuevo() => new(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);
        await using var contexto = Nuevo();
        await contexto.Database.MigrateAsync();
        return (cadena, Nuevo);
    }

    private static VarianteDisenoDto V(int id, int prod = 71, string talla = "S") => new(id, prod, "Playera", "Black", "#000000", talla, 12.5m);

    [Fact]
    public async Task LaMigracionAplicaSobreIdentidadSinPerderDatosYCreaLasTablasEIndices()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = new ContextoBlackHellHeim(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);
        var migrador = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions
            .GetInfrastructure(contexto).GetRequiredService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        var anterior = contexto.Database.GetMigrations().Reverse().Skip(1).First();
        await migrador.MigrateAsync(anterior); // hasta AgregarIdentidad
        await sql.ConsultarEscalarAsync<int>(cadena, "INSERT INTO dbo.Tiendas (Codigo, Nombre, Moneda) VALUES ('XYZ','Prueba','MXN'); SELECT 1");

        await contexto.Database.MigrateAsync();

        Assert.Equal(3, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM dbo.Tiendas"));
        Assert.Equal(2, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM AspNetRoles"));
        Assert.Equal(5, await sql.ConsultarEscalarAsync<int>(cadena,
            "SELECT COUNT(*) FROM sys.tables WHERE name IN ('Disenos','ProductosBase','Variantes','DisenosVariantes','ArchivosArte')"));
        Assert.Equal(1, await sql.ConsultarEscalarAsync<int>(cadena,
            "SELECT COUNT(*) FROM sys.indexes WHERE name LIKE 'IX_Disenos_Slug' AND is_unique = 1"));
    }

    [Fact]
    public async Task UnDisenoCompletoSeGuardaYSeRecuperaIdentico()
    {
        var (_, nuevo) = await PrepararAsync();
        Guid id;
        await using (var contexto = nuevo())
        {
            var arte = ArchivoArte.Crear("http://x/a.png", "a.png", "image/png", 100, 4500, 5400, new string('a', 64), Ahora);
            contexto.ArchivosArte.Add(arte);
            var repo = new RepositorioDisenos(contexto);
            var diseno = Diseno.Crear("Calavera épica", "Descripción", Ahora);
            diseno.AsignarArte(arte, Ahora);
            diseno.ReemplazarVariantes(await repo.ObtenerOCrearVariantesAsync([V(1), V(2, talla: "M"), V(3, talla: "L")], default), Ahora);
            diseno.CambiarEstado(EstadoDiseno.Publicado, Ahora);
            repo.Agregar(diseno);
            Assert.Equal(ResultadoGuardado.Correcto, await repo.GuardarAsync(diseno, null, default));
            id = diseno.Id;
        }

        await using var lectura = nuevo();
        var leido = await new RepositorioDisenos(lectura).ObtenerAsync(id, default);

        Assert.Equal("Calavera épica", leido!.Nombre);
        Assert.Equal("calavera-epica", leido.Slug);
        Assert.Equal(EstadoDiseno.Publicado, leido.Estado);
        Assert.Equal("http://x/a.png", leido.ArchivoArte!.Url);
        Assert.Equal(["L", "M", "S"], leido.Variantes.Select(v => v.Talla).Order());
        Assert.All(leido.Variantes, v => Assert.Equal(12.5m, v.CostoBaseUsd));
        Assert.Equal(1, lectura.ProductosBase.Count());
        Assert.NotEmpty(leido.Version);
    }

    [Fact]
    public async Task ElProductoBaseYLaVarianteSeReutilizanPorIdDePrintful()
    {
        var (_, nuevo) = await PrepararAsync();
        await using (var c1 = nuevo())
        {
            var repo = new RepositorioDisenos(c1);
            var a = Diseno.Crear("Diseño uno", null, Ahora);
            a.ReemplazarVariantes(await repo.ObtenerOCrearVariantesAsync([V(1), V(2)], default), Ahora);
            repo.Agregar(a);
            await repo.GuardarAsync(a, null, default);
        }

        await using var c2 = nuevo();
        var repo2 = new RepositorioDisenos(c2);
        var b = Diseno.Crear("Diseño dos", null, Ahora);
        b.ReemplazarVariantes(await repo2.ObtenerOCrearVariantesAsync([V(2), V(3)], default), Ahora);
        repo2.Agregar(b);
        await repo2.GuardarAsync(b, null, default);

        Assert.Equal(1, await c2.ProductosBase.CountAsync());
        Assert.Equal(3, await c2.Variantes.CountAsync());
    }

    [Fact]
    public async Task NombreYSlugRepetidosDanDuplicado()
    {
        var (_, nuevo) = await PrepararAsync();
        await using var contexto = nuevo();
        var repo = new RepositorioDisenos(contexto);
        var uno = Diseno.Crear("Calavera", null, Ahora);
        repo.Agregar(uno);
        await repo.GuardarAsync(uno, null, default);

        await using var otro = nuevo();
        var repo2 = new RepositorioDisenos(otro);
        var repetido = Diseno.Crear("calavera", null, Ahora);
        repo2.Agregar(repetido);

        Assert.Equal(ResultadoGuardado.Duplicado, await repo2.GuardarAsync(repetido, null, default));
        Assert.True(await repo2.ExisteNombreOSlugAsync("Calavera", "calavera", null, default));
    }

    [Fact]
    public async Task EditarConUnaVersionVencidaDaConcurrencia()
    {
        var (_, nuevo) = await PrepararAsync();
        Guid id;
        byte[] versionInicial;
        await using (var c = nuevo())
        {
            var repo = new RepositorioDisenos(c);
            var d = Diseno.Crear("Calavera", null, Ahora);
            repo.Agregar(d);
            await repo.GuardarAsync(d, null, default);
            id = d.Id;
            versionInicial = d.Version.ToArray();
        }

        await using (var primera = nuevo())
        {
            var repo = new RepositorioDisenos(primera);
            var d = (await repo.ObtenerAsync(id, default))!;
            d.EditarDatos("Calavera dos", null, Ahora);
            Assert.Equal(ResultadoGuardado.Correcto, await repo.GuardarAsync(d, versionInicial, default));
        }

        await using var segunda = nuevo();
        var repo2 = new RepositorioDisenos(segunda);
        var vencido = (await repo2.ObtenerAsync(id, default))!;
        vencido.EditarDatos("Calavera tres", null, Ahora);

        Assert.Equal(ResultadoGuardado.Concurrencia, await repo2.GuardarAsync(vencido, versionInicial, default));
    }

    [Fact]
    public async Task ListarPaginaYFiltraPorEstado()
    {
        var (_, nuevo) = await PrepararAsync();
        await using var contexto = nuevo();
        var repo = new RepositorioDisenos(contexto);
        for (var i = 1; i <= 5; i++)
        {
            repo.Agregar(Diseno.Crear($"Diseño {i}", null, Ahora.AddMinutes(i)));
        }

        await contexto.SaveChangesAsync();

        var (pagina, total) = await repo.ListarAsync(EstadoDiseno.Borrador, 2, 2, default);
        var (publicados, totalPublicados) = await repo.ListarAsync(EstadoDiseno.Publicado, 1, 10, default);

        Assert.Equal(5, total);
        Assert.Equal(2, pagina.Count);
        Assert.Equal("Diseño 3", pagina[0].Nombre); // más recientes primero: 5,4 | 3,2 | 1
        Assert.Empty(publicados);
        Assert.Equal(0, totalPublicados);
    }
}
