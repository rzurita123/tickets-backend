using SistemaTickets.Application.Auditoria;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Tickets;

public class ServicioConsultaTickets(TraductorFiltroTickets traductor, IConsultaTickets consulta, IRepositorioTickets tickets, IAuditoria auditoria)
{
    public async Task<PaginaTickets> ListarAsync(Usuario usuario, FiltroTickets filtro, CancellationToken cancelacion = default) =>
        await consulta.ListarPaginaAsync(usuario, await traductor.TraducirAsync(filtro, cancelacion), cancelacion);

    /// Cuenta por estado respetando los demás filtros (para los chips del front).
    public async Task<IReadOnlyDictionary<EstadoTicket, int>> ContarAsync(Usuario usuario, FiltroTickets filtro, CancellationToken cancelacion = default) =>
        await consulta.ContarPorEstadoAsync(usuario, await traductor.TraducirAsync(filtro with { Estado = null }, cancelacion), cancelacion);

    /// Null si no existe o el usuario no lo puede ver: el controller responde 404 en ambos casos.
    public async Task<Ticket?> ObtenerAsync(Usuario usuario, int id, CancellationToken cancelacion = default)
    {
        var ticket = await tickets.BuscarAsync(id, cancelacion);
        if (ticket is null || ticket.Estado == EstadoTicket.Borrador)
        {
            return null;
        }

        if (!VisibilidadTickets.PuedeVer(ticket, usuario))
        {
            auditoria.Registrar(usuario.Id, OperacionesAuditoria.AccesoDenegado, EntidadesAuditoria.Ticket, ticket.Id.ToString(),
                $"Ticket #{ticket.Numero} de otra localidad");
            await auditoria.GuardarAsync(cancelacion);
            return null;
        }

        return ticket;
    }
}