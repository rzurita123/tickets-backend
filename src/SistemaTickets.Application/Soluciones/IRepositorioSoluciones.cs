using SistemaTickets.Domain.Soluciones;

namespace SistemaTickets.Application.Soluciones;

public interface IRepositorioSoluciones
{
    Task<IReadOnlyList<Solucion>> ListarAsync(CancellationToken cancelacion = default);
}
