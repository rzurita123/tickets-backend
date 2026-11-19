using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Usuarios;

public interface IRepositorioUsuarios
{
    Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion = default);

    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancelacion = default);
}
