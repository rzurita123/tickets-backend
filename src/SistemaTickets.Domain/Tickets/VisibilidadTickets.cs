using System.Linq.Expressions;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Domain.Tickets;

/// Única definición de "quién ve qué ticket" (RNF2). La usan la consulta SQL y las reglas en memoria.
public static class VisibilidadTickets
{
    public static Expression<Func<Ticket, bool>> Para(Usuario usuario)
    {
        var usuarioId = usuario.Id;
        var esOrmen = usuario.EsOrmen;
        var localidadId = usuario.LocalidadId;
        return t => t.Estado == EstadoTicket.Borrador
            ? t.CreadoPorId == usuarioId
            : esOrmen || (!t.Privado && localidadId != null && t.LocalidadId == localidadId);
    }

    public static bool PuedeVer(Ticket ticket, Usuario usuario) => Para(usuario).Compile().Invoke(ticket);
}