using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Localidades;
using SistemaTickets.Domain.Usuarios;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Localidades;

public class RepositorioLocalidades(ContextoTickets contexto) : IRepositorioLocalidades
{
    public async Task<IReadOnlyList<Localidad>> ListarAsync(CancellationToken cancelacion = default) =>
        await contexto.Localidades.AsNoTracking().OrderBy(l => l.Nombre).ToListAsync(cancelacion);
}
