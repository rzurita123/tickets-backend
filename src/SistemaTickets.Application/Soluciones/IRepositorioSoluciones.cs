using SistemaTickets.Domain.Soluciones;

namespace SistemaTickets.Application.Soluciones;

public interface IRepositorioSoluciones
{
    Task<IReadOnlyList<Solucion>> ListarAsync(CancellationToken cancelacion = default);

    /// Con adjuntos, para mostrarlas completas.
    Task<IReadOnlyList<Solucion>> ListarCompletasAsync(EstadoSolucion? estado, CancellationToken cancelacion = default);

    Task<Solucion?> BuscarAsync(string codigo, CancellationToken cancelacion = default);

    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancelacion = default);

    Task<bool> TienePropuestaVigenteAsync(int ticketId, CancellationToken cancelacion = default);

    /// Tickets cerrados que aplicaron cada solución, por id de solución.
    Task<IReadOnlyDictionary<int, int>> ContarUsosAsync(CancellationToken cancelacion = default);

    void Agregar(Solucion solucion);

    Task GuardarAsync(CancellationToken cancelacion = default);
}
