using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Localidades;

public interface IRepositorioLocalidades
{
    Task<IReadOnlyList<Localidad>> ListarAsync(CancellationToken cancelacion = default);
}
