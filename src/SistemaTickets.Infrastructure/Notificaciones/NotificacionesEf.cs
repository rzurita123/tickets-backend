using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Notificaciones;
using SistemaTickets.Domain.Notificaciones;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Notificaciones;

public class NotificacionesEf(ContextoTickets contexto) : INotificaciones
{
    public void Encolar(Notificacion notificacion) => contexto.Notificaciones.Add(notificacion);

    public async Task<IReadOnlyList<Notificacion>> ListarAsync(int? destinatarioId, int limite, CancellationToken cancelacion = default)
    {
        var consulta = contexto.Notificaciones.AsNoTracking();
        if (destinatarioId is int id)
        {
            consulta = consulta.Where(n => n.DestinatarioId == id);
        }

        return await consulta
            .OrderByDescending(n => n.CreadaEn).ThenByDescending(n => n.Id)
            .Take(limite)
            .ToListAsync(cancelacion);
    }
}
