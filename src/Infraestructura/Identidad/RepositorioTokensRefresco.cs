using BlackHellHeim.Aplicacion.Autenticacion;
using BlackHellHeim.Dominio.Autenticacion;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BlackHellHeim.Infraestructura.Identidad;

public sealed class RepositorioTokensRefresco(ContextoBlackHellHeim contexto) : IRepositorioTokensRefresco
{
    public async Task AgregarAsync(TokenRefresco token, CancellationToken cancelacion)
    {
        contexto.TokensRefresco.Add(token);
        await contexto.SaveChangesAsync(cancelacion);
    }

    public Task<TokenRefresco?> ObtenerPorHashAsync(string hash, CancellationToken cancelacion) =>
        contexto.TokensRefresco.AsNoTracking().FirstOrDefaultAsync(t => t.HashToken == hash, cancelacion);

    // UPDATE condicional: solo una de dos renovaciones simultáneas afecta una fila.
    public async Task<bool> MarcarUsadoAsync(Guid id, DateTimeOffset ahora, CancellationToken cancelacion) =>
        await contexto.TokensRefresco
            .Where(t => t.Id == id && t.UsadoUtc == null && t.RevocadoUtc == null && t.ExpiraUtc > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoUtc, ahora), cancelacion) == 1;

    public async Task RevocarFamiliaAsync(Guid familiaId, DateTimeOffset ahora, CancellationToken cancelacion) =>
        await contexto.TokensRefresco
            .Where(t => t.FamiliaId == familiaId && t.RevocadoUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoUtc, ahora), cancelacion);

    public Task<int> PurgarCaducadosAsync(DateTimeOffset limite, CancellationToken cancelacion) =>
        contexto.TokensRefresco.Where(t => t.ExpiraUtc < limite).ExecuteDeleteAsync(cancelacion);
}
