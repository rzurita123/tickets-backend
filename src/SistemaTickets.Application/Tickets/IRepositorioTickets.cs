using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Tickets;

public interface IRepositorioTickets
{
    Task<IReadOnlyList<Ticket>> ListarParaAsync(Usuario usuario, CancellationToken cancelacion = default);

    Task<Ticket?> BuscarAsync(int id, CancellationToken cancelacion = default);

    Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default);

    void Agregar(Ticket ticket);

    void Eliminar(Ticket ticket);

    Task GuardarAsync(CancellationToken cancelacion = default);
}
