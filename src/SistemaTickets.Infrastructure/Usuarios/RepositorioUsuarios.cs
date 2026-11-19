using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Usuarios;

public class RepositorioUsuarios(ContextoTickets contexto) : IRepositorioUsuarios
{
    public Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion = default) =>
        contexto.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, cancelacion);

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancelacion = default) =>
        contexto.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancelacion);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancelacion = default) =>
        await contexto.Usuarios.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancelacion);
}
