using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Soluciones;

public class RepositorioParametros(ContextoTickets contexto) : IRepositorioParametros
{
    public async Task<IReadOnlyList<ParametroSugerencia>> ListarAsync(CancellationToken cancelacion = default) =>
        await contexto.ParametrosSugerencia.OrderBy(p => p.Clave).ToListAsync(cancelacion);

    public void Agregar(ParametroSugerencia parametro) => contexto.ParametrosSugerencia.Add(parametro);

    public Task GuardarAsync(CancellationToken cancelacion = default) => contexto.SaveChangesAsync(cancelacion);
}
