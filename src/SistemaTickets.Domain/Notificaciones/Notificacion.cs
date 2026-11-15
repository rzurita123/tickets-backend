namespace SistemaTickets.Domain.Notificaciones;

public enum CanalNotificacion
{
    Correo,
    WhatsApp,
    Slack,
}

public enum EstadoNotificacion
{
    Pendiente,
    Enviada,
    Fallida,
}

public class Notificacion
{
    public int Id { get; private set; }
    public int? TicketId { get; init; }
    public int? DestinatarioId { get; init; }
    public required string Destino { get; init; }
    public CanalNotificacion Canal { get; init; }
    public required string Asunto { get; init; }
    public required string Cuerpo { get; init; }
    public EstadoNotificacion Estado { get; set; } = EstadoNotificacion.Pendiente;
    public int Intentos { get; set; }
    public DateTime CreadaEn { get; init; }
    public DateTime? EnviadaEn { get; set; }
}
