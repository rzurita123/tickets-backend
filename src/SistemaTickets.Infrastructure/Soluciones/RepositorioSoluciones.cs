using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Soluciones;

public class RepositorioSoluciones(ContextoTickets contexto) : IRepositorioSoluciones
{
    public async Task<IReadOnlyList<Solucion>> ListarAsync(CancellationToken cancelacion = default) =>
        await contexto.Soluciones.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancelacion);
}
