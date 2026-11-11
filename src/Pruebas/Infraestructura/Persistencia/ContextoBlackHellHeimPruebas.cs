using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Persistencia;

[Collection(ColeccionSqlServer.Nombre)]
public class ContextoBlackHellHeimPruebas(ContenedorSqlServer sql)
{
    private static ContextoBlackHellHeim CrearContexto(string cadena) =>
        new(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);

    [Fact]
    public async Task MigrarCreaElEsquemaYLasDosSemillas()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = CrearContexto(cadena);

        await contexto.Database.MigrateAsync();

        var columnas = await sql.ConsultarEscalarAsync<string>(cadena,
            """
            SELECT STRING_AGG(c.name + ':' + t.name + '(' + CAST(c.max_length AS varchar) + ')' + IIF(c.is_nullable = 1, ':null', ':notnull'), '|')
                   WITHIN GROUP (ORDER BY c.name)
            FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID('dbo.Tiendas')
            """);
        // char(3) no Unicode = 3 bytes; nvarchar(120) = 240 bytes.
        Assert.Equal("Codigo:char(3):notnull|Moneda:char(3):notnull|Nombre:nvarchar(240):notnull", columnas);

        var clavePrimaria = await sql.ConsultarEscalarAsync<string>(cadena,
            """
            SELECT STRING_AGG(col.name, ',') FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID('dbo.Tiendas') AND i.is_primary_key = 1
            """);
        Assert.Equal("Codigo", clavePrimaria);

        var filas = await sql.ConsultarEscalarAsync<string>(cadena,
            "SELECT STRING_AGG(Codigo + '/' + Nombre + '/' + Moneda, '|') WITHIN GROUP (ORDER BY Codigo) FROM dbo.Tiendas");
        Assert.Equal("BHH/BlackHellHeim/MXN|DEM/Tienda de demostración/MXN", filas);
    }

    [Fact]
    public async Task MigrarDosVecesNoDuplicaLasSemillas()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = CrearContexto(cadena);

        await contexto.Database.MigrateAsync();
        await contexto.Database.MigrateAsync();

        Assert.Equal(2, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM dbo.Tiendas"));
    }

    [Fact]
    public async Task RevertirACeroEliminaLaTablaYLasSemillasYVolverAAplicarLasRestaura()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = CrearContexto(cadena);
        await contexto.Database.MigrateAsync();
        var migrador = contexto.GetInfrastructure().GetRequiredService<IMigrator>();

        await migrador.MigrateAsync("0");

        Assert.Null(await sql.ConsultarEscalarAsync<string>(cadena, "SELECT OBJECT_ID('dbo.Tiendas')"));

        await contexto.Database.MigrateAsync();

        Assert.Equal(2, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM dbo.Tiendas"));
    }

    [Fact]
    public async Task ElHistorialContieneSoloLaMigracionInicialYQuedaVacioAlRevertir()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = CrearContexto(cadena);

        await contexto.Database.MigrateAsync();
        var aplicadas = await sql.ConsultarEscalarAsync<string>(cadena,
            "SELECT STRING_AGG(MigrationId, '|') FROM dbo.__EFMigrationsHistory");
        Assert.EndsWith("_MigracionInicial", aplicadas);
        Assert.DoesNotContain("|", aplicadas);

        await contexto.GetInfrastructure().GetRequiredService<IMigrator>().MigrateAsync("0");
        Assert.Equal(0, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory"));
    }

    [Fact]
    public async Task UnaMigracionQueFallaAMitadNoSeRegistraNiDejaRastros()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = new ContextoDeFallo(
            new DbContextOptionsBuilder<ContextoDeFallo>().UseSqlServer(cadena).Options);

        await Assert.ThrowsAsync<SqlException>(async () => await contexto.Database.MigrateAsync());

        Assert.Null(await sql.ConsultarEscalarAsync<string>(cadena, "SELECT OBJECT_ID('dbo.Parcial')"));
        Assert.Equal(0, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory"));
    }
}

/// <summary>Contexto y migración de prueba que crean una tabla y luego fallan.</summary>
public sealed class ContextoDeFallo(DbContextOptions<ContextoDeFallo> opciones) : DbContext(opciones);

[DbContext(typeof(ContextoDeFallo))]
[Migration("20260101000000_FallaAMitad")]
public sealed class MigracionQueFalla : Migration
{
    protected override void Up(MigrationBuilder migracion)
    {
        migracion.Sql("CREATE TABLE dbo.Parcial (Id int NOT NULL)");
        migracion.Sql("SELECT 1/0");
    }
}
