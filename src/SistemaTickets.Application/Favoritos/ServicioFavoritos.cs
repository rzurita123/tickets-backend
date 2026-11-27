using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Favoritos;

public interface IRepositorioFavoritos
{
    Task<IReadOnlyList<Favorito>> ListarAsync(int usuarioId, CancellationToken cancelacion = default);

    Task<Favorito?> BuscarAsync(int usuarioId, int? ticketId, int? solucionId, CancellationToken cancelacion = default);

    void Agregar(Favorito favorito);

    void Quitar(Favorito favorito);

    Task GuardarAsync(CancellationToken cancelacion = default);
}

/// Favoritos personales de ORMEN: tickets y soluciones reutilizables.
public class ServicioFavoritos(
    IRepositorioFavoritos favoritos,
    IRepositorioTickets tickets,
    IRepositorioSoluciones soluciones,
    TimeProvider reloj)
{
    public async Task<IReadOnlyList<Favorito>> ListarAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        ExigirOrmen(usuario);
        return (await favoritos.ListarAsync(usuario.Id, cancelacion)).OrderByDescending(f => f.Fecha).ToList();
    }

    /// False si el ticket no existe o no se puede ver.
    public async Task<bool> MarcarTicketAsync(Usuario usuario, int ticketId, bool marcado, CancellationToken cancelacion = default)
    {
        ExigirOrmen(usuario);
        var ticket = await tickets.BuscarAsync(ticketId, cancelacion);
        if (ticket is null || ticket.Estado == EstadoTicket.Borrador || !VisibilidadTickets.PuedeVer(ticket, usuario))
        {
            return false;
        }

        await MarcarAsync(usuario, ticketId, null, marcado, cancelacion);
        return true;
    }

    /// False si la solución no existe.
    public async Task<bool> MarcarSolucionAsync(Usuario usuario, string codigo, bool marcado, CancellationToken cancelacion = default)
    {
        ExigirOrmen(usuario);
        var solucion = await soluciones.BuscarAsync(codigo, cancelacion);
        if (solucion is null)
        {
            return false;
        }

        await MarcarAsync(usuario, null, solucion.Id, marcado, cancelacion);
        return true;
    }

    private async Task MarcarAsync(Usuario usuario, int? ticketId, int? solucionId, bool marcado, CancellationToken cancelacion)
    {
        var existente = await favoritos.BuscarAsync(usuario.Id, ticketId, solucionId, cancelacion);
        if (marcado && existente is null)
        {
            favoritos.Agregar(new Favorito
            {
                UsuarioId = usuario.Id,
                TicketId = ticketId,
                SolucionId = solucionId,
                Fecha = reloj.GetUtcNow().UtcDateTime,
            });
        }
        else if (!marcado && existente is not null)
        {
            favoritos.Quitar(existente);
        }
        else
        {
            return;
        }

        await favoritos.GuardarAsync(cancelacion);
    }

    private static void ExigirOrmen(Usuario usuario)
    {
        if (!usuario.EsOrmen)
        {
            throw new SinPermisoException("Los favoritos son de ORMEN.");
        }
    }
}
