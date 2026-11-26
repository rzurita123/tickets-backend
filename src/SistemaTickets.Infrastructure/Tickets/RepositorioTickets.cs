using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Tickets;

public class RepositorioTickets(ContextoTickets contexto) : IRepositorioTickets
{
    private const int PrimerNumero = 1001;

    public async Task<IReadOnlyList<Ticket>> ListarParaAsync(Usuario usuario, bool incluirBorradores, CancellationToken cancelacion = default)
    {
        var esOrmen = usuario.EsOrmen;
        var localidadId = usuario.LocalidadId;
        return await Completos()
            .AsNoTracking()
            .Where(t => t.Estado == EstadoTicket.Borrador
                ? incluirBorradores && t.CreadoPorId == usuario.Id
                : esOrmen || (!t.Privado && localidadId != null && t.LocalidadId == localidadId))
            .OrderBy(t => t.Id)
            .ToListAsync(cancelacion);
    }

    public Task<Ticket?> BuscarAsync(int id, CancellationToken cancelacion = default) =>
        Completos().FirstOrDefaultAsync(t => t.Id == id, cancelacion);

    public async Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default) =>
        (await contexto.Tickets.MaxAsync(t => t.Numero, cancelacion) ?? PrimerNumero - 1) + 1;

    public void Agregar(Ticket ticket) => contexto.Tickets.Add(ticket);

    public void Eliminar(Ticket ticket) => contexto.Tickets.Remove(ticket);

    public Task GuardarAsync(CancellationToken cancelacion = default) => contexto.SaveChangesAsync(cancelacion);

    private IQueryable<Ticket> Completos() =>
        contexto.Tickets.Include(t => t.Actividades).Include(t => t.Adjuntos).AsSplitQuery();
}
