using SistemaTickets.Domain.Notificaciones;

namespace SistemaTickets.Application.Notificaciones;

/// Avisos a enviar. Se guardan junto con el resto de los cambios del pedido.
public interface INotificaciones
{
    void Encolar(Notificacion notificacion);

    /// Sin destinatario: todas (para ORMEN).
    Task<IReadOnlyList<Notificacion>> ListarAsync(int? destinatarioId, int limite, CancellationToken cancelacion = default);
}
