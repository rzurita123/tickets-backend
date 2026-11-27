using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Soluciones;

public class RepositorioSoluciones(ContextoTickets contexto) : IRepositorioSoluciones
{
    public async Task<IReadOnlyList<Solucion>> ListarAsync(CancellationToken cancelacion = default) =>
        await contexto.Soluciones.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Solucion>> ListarCompletasAsync(EstadoSolucion? estado, CancellationToken cancelacion = default)
    {
        var consulta = contexto.Soluciones.AsNoTracking().Include(s => s.Adjuntos).AsSplitQuery();
        if (estado is EstadoSolucion e)
        {
            consulta = consulta.Where(s => s.Estado == e);
        }

        return await consulta.OrderBy(s => s.Id).ToListAsync(cancelacion);
    }

    public Task<Solucion?> BuscarAsync(string codigo, CancellationToken cancelacion = default) =>
        contexto.Soluciones.Include(s => s.Adjuntos).FirstOrDefaultAsync(s => s.Codigo == codigo, cancelacion);

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancelacion = default) =>
        contexto.Soluciones.AnyAsync(s => s.Codigo == codigo, cancelacion);

    public Task<bool> TienePropuestaVigenteAsync(int ticketId, CancellationToken cancelacion = default) =>
        contexto.Soluciones.AnyAsync(s => s.TicketOrigenId == ticketId && s.Estado != EstadoSolucion.Rechazada, cancelacion);

    public async Task<IReadOnlyDictionary<int, int>> ContarUsosAsync(CancellationToken cancelacion = default) =>
        await contexto.Tickets
            .Where(t => t.Estado == EstadoTicket.Cerrado && t.SolucionAplicadaId != null)
            .GroupBy(t => t.SolucionAplicadaId!.Value)
            .Select(g => new { Id = g.Key, Usos = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Usos, cancelacion);

    public void Agregar(Solucion solucion) => contexto.Soluciones.Add(solucion);

    public Task GuardarAsync(CancellationToken cancelacion = default) => contexto.SaveChangesAsync(cancelacion);
}
