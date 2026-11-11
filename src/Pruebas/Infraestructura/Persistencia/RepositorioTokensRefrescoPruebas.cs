using BlackHellHeim.Dominio.Autenticacion;
using BlackHellHeim.Infraestructura.Identidad;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Persistencia;

[Collection(ColeccionSqlServer.Nombre)]
public class RepositorioTokensRefrescoPruebas(ContenedorSqlServer sql)
{
    private static ContextoBlackHellHeim Contexto(string cadena) =>
        new(new DbContextOptionsBuilder<ContextoBlackHellHeim>().UseSqlServer(cadena).Options);

    [Fact]
    public async Task MigrarSiembraLosRolesYMarcarUsadoEsAtomicoYPurgarEliminaCaducados()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = Contexto(cadena);
        await contexto.Database.MigrateAsync();
        Assert.Equal("Administrador|Cliente", await sql.ConsultarEscalarAsync<string>(cadena,
            "SELECT STRING_AGG(Name,'|') WITHIN GROUP (ORDER BY Name) FROM AspNetRoles"));

        var usuario = new Usuario { Id = Guid.NewGuid(), UserName = "a@b.com", Email = "a@b.com", NormalizedEmail = "A@B.COM", NormalizedUserName = "A@B.COM" };
        contexto.Users.Add(usuario);
        await contexto.SaveChangesAsync();
        var ahora = DateTimeOffset.UtcNow;
        var vigente = TokenRefresco.Crear(usuario.Id, Guid.NewGuid(), new string('a', 64), ahora, TimeSpan.FromDays(1));
        var viejo = TokenRefresco.Crear(usuario.Id, Guid.NewGuid(), new string('b', 64), ahora.AddDays(-10), TimeSpan.FromDays(1));
        var repositorio = new RepositorioTokensRefresco(contexto);
        await repositorio.AgregarAsync(vigente, default);
        await repositorio.AgregarAsync(viejo, default);

        Assert.True(await repositorio.MarcarUsadoAsync(vigente.Id, ahora, default));
        Assert.False(await repositorio.MarcarUsadoAsync(vigente.Id, ahora, default));
        Assert.Equal(1, await repositorio.PurgarCaducadosAsync(ahora, default));
        Assert.Equal(1, await sql.ConsultarEscalarAsync<int>(cadena, "SELECT COUNT(*) FROM TokensRefresco"));
    }

    [Fact]
    public async Task ElHashDeTokenEsUnico()
    {
        var cadena = await sql.CrearBaseAsync();
        await using var contexto = Contexto(cadena);
        await contexto.Database.MigrateAsync();
        var usuario = new Usuario { Id = Guid.NewGuid(), UserName = "a@b.com", Email = "a@b.com", NormalizedEmail = "A@B.COM", NormalizedUserName = "A@B.COM" };
        contexto.Users.Add(usuario);
        await contexto.SaveChangesAsync();
        var repositorio = new RepositorioTokensRefresco(contexto);
        await repositorio.AgregarAsync(TokenRefresco.Crear(usuario.Id, Guid.NewGuid(), new string('c', 64), DateTimeOffset.UtcNow, TimeSpan.FromDays(1)), default);

        await Assert.ThrowsAsync<DbUpdateException>(() => repositorio.AgregarAsync(
            TokenRefresco.Crear(usuario.Id, Guid.NewGuid(), new string('c', 64), DateTimeOffset.UtcNow, TimeSpan.FromDays(1)), default));
    }
}
