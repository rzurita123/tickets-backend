using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Favoritos;
using SistemaTickets.Domain.Usuarios;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Favoritos;

public class RepositorioFavoritos(ContextoTickets contexto) : IRepositorioFavoritos
{
    public async Task<IReadOnlyList<Favorito>> ListarAsync(int usuarioId, CancellationToken cancelacion = default) =>
        await contexto.Favoritos.AsNoTracking().Where(f => f.UsuarioId == usuarioId).ToListAsync(cancelacion);

    public Task<Favorito?> BuscarAsync(int usuarioId, int? ticketId, int? solucionId, CancellationToken cancelacion = default) =>
        contexto.Favoritos.FirstOrDefaultAsync(
            f => f.UsuarioId == usuarioId && f.TicketId == ticketId && f.SolucionId == solucionId, cancelacion);

    public void Agregar(Favorito favorito) => contexto.Favoritos.Add(favorito);

    public void Quitar(Favorito favorito) => contexto.Favoritos.Remove(favorito);

    public Task GuardarAsync(CancellationToken cancelacion = default) => contexto.SaveChangesAsync(cancelacion);
}
