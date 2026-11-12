using BlackHellHeim.Aplicacion.Mockups;
using BlackHellHeim.Dominio.Mockups;
using BlackHellHeim.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BlackHellHeim.Infraestructura.Mockups;

public sealed class RepositorioMockups(ContextoBlackHellHeim contexto) : IRepositorioMockups
{
    public Task<bool> ExisteDisenoAsync(Guid disenoId, CancellationToken c) => contexto.Disenos.AnyAsync(d => d.Id == disenoId, c);

    public Task<bool> ExistePendienteAsync(Guid disenoId, CancellationToken c) =>
        contexto.TareasMockup.AnyAsync(t => t.DisenoId == disenoId && t.Estado == EstadoTareaMockup.Pendiente, c);

    public async Task<bool> AgregarAsync(TareaMockup tarea, CancellationToken c)
    {
        contexto.TareasMockup.Add(tarea);
        try
        {
            await contexto.SaveChangesAsync(c);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            contexto.Entry(tarea).State = EntityState.Detached;
            return false;
        }
    }

    public Task<TareaMockup?> ObtenerAsync(Guid id, CancellationToken c) =>
        contexto.TareasMockup.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, c);

    public Task<TareaMockup?> ObtenerPorPrintfulIdAsync(string printfulTaskId, CancellationToken c) =>
        contexto.TareasMockup.AsNoTracking().FirstOrDefaultAsync(t => t.PrintfulTaskId == printfulTaskId, c);

    public async Task<IReadOnlyList<TareaMockup>> ListarPorDisenoAsync(Guid disenoId, CancellationToken c) =>
        await contexto.TareasMockup.AsNoTracking().Include(t => t.Imagenes).Where(t => t.DisenoId == disenoId)
            .OrderByDescending(t => t.CreadaUtc).ToListAsync(c);

    public async Task<IReadOnlyList<Guid>> ListarPendientesAntesDeAsync(DateTimeOffset limite, int maximo, CancellationToken c) =>
        await contexto.TareasMockup.AsNoTracking()
            .Where(t => t.Estado == EstadoTareaMockup.Pendiente && t.CreadaUtc <= limite)
            .OrderBy(t => t.CreadaUtc).Select(t => t.Id).Take(maximo).ToListAsync(c);

    public async Task<bool> CompletarAsync(Guid tareaId, IReadOnlyList<ImagenMockup> imagenes, DateTimeOffset ahora, CancellationToken c)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(c);
        var cambiadas = await contexto.TareasMockup.Where(t => t.Id == tareaId && t.Estado == EstadoTareaMockup.Pendiente)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Estado, EstadoTareaMockup.Completada).SetProperty(t => t.FinalizadaUtc, ahora), c);
        if (cambiadas == 0) return false; // el perdedor de la carrera no inserta nada

        contexto.ImagenesMockup.AddRange(imagenes);
        await contexto.SaveChangesAsync(c);
        await transaccion.CommitAsync(c);
        return true;
    }

    public async Task<bool> FallarAsync(Guid tareaId, string motivo, DateTimeOffset ahora, CancellationToken c) =>
        await contexto.TareasMockup.Where(t => t.Id == tareaId && t.Estado == EstadoTareaMockup.Pendiente)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Estado, EstadoTareaMockup.Fallida).SetProperty(t => t.Motivo, motivo)
                .SetProperty(t => t.FinalizadaUtc, ahora), c) == 1;
}
