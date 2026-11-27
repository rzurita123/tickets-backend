using SistemaTickets.Domain.Notificaciones;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Notificaciones;

public enum MotivoAviso
{
    Comentario,
    Estado,
    EnProceso,
    Elevado,
    Bloqueado,
    Cierre,
    Reapertura,
}

/// Correo al creador del ticket cuando ORMEN lo actualiza; sólo información pública.
public static class AvisosTicket
{
    public static Notificacion? Componer(Ticket ticket, Usuario? creador, Usuario actor, MotivoAviso motivo, string? detalle, DateTime ahora)
    {
        if (ticket.Privado || creador is not { Rol: Rol.Cliente, Activo: true })
        {
            return null;
        }

        var numero = ticket.Numero is int n ? $"#{n}" : string.Empty;
        var estado = NombreEstado(ticket.Estado);
        var asunto = motivo switch
        {
            MotivoAviso.Comentario => $"Ticket {numero}: nueva respuesta de Mesa de ayuda",
            MotivoAviso.Estado => $"Ticket {numero}: el estado cambió a {estado}",
            MotivoAviso.EnProceso => $"Ticket {numero}: Mesa de ayuda lo está atendiendo",
            MotivoAviso.Elevado => $"Ticket {numero}: se derivó a otro nivel de soporte",
            MotivoAviso.Bloqueado => $"Ticket {numero}: en espera de un factor externo",
            MotivoAviso.Cierre => $"Ticket {numero} cerrado",
            MotivoAviso.Reapertura => $"Ticket {numero} reabierto",
            _ => $"Ticket {numero} actualizado",
        };

        var lineas = new List<string>
        {
            $"Hola {creador.Nombre.Split(' ')[0]}:",
            string.Empty,
            $"Tu ticket {numero} \"{ticket.Titulo}\" tuvo una actualización.",
            $"Estado actual: {estado}.",
        };
        if (!string.IsNullOrEmpty(detalle))
        {
            lineas.Add(string.Empty);
            lineas.Add(detalle);
        }

        lineas.Add(string.Empty);
        lineas.Add("Podés ver el detalle ingresando al Sistema de tickets.");
        lineas.Add(string.Empty);
        lineas.Add($"Mesa de ayuda ORMEN ({actor.Nombre})");

        return new Notificacion
        {
            TicketId = ticket.Id,
            DestinatarioId = creador.Id,
            Destino = creador.Email,
            Canal = CanalNotificacion.Correo,
            Asunto = asunto,
            Cuerpo = string.Join('\n', lineas),
            CreadaEn = ahora,
        };
    }

    public static string NombreEstado(EstadoTicket estado) => estado switch
    {
        EstadoTicket.Borrador => "Borrador",
        EstadoTicket.Abierto => "Abierto",
        EstadoTicket.EnProceso => "En proceso",
        EstadoTicket.Bloqueado => "Bloqueado",
        EstadoTicket.Cerrado => "Cerrado",
        _ => estado.ToString(),
    };
}
