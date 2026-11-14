namespace SistemaTickets.Domain.Tickets;

public enum TipoActividad
{
    Creacion,
    CambioEstado,
    Comentario,
    Asignacion,
    Elevacion,
    ModificacionDatos,
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
}
