namespace SistemaTickets.Domain.Tickets;

public enum TipoActividad
{
    Creacion,
    Comentario,
    Asignacion,
    Liberacion,
    Elevacion,
    Bloqueo,
    Desbloqueo,
    ModificacionDatos,
    Cierre,
    Reapertura,
    Privacidad,
}

public class Actividad
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public TipoActividad Tipo { get; init; }
    public int AutorId { get; init; }
    public DateTime Fecha { get; init; }
    public string? Texto { get; init; }
    public bool Privada { get; init; }
    public EstadoTicket? EstadoAnterior { get; init; }
    public EstadoTicket? EstadoNuevo { get; init; }
    public int? OperadorId { get; init; }
    public int? DestinoGrupoId { get; init; }
    public int? DestinoUsuarioId { get; init; }
    public string? Detalle { get; init; }
}
