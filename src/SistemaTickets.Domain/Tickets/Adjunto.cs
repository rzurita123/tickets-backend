namespace SistemaTickets.Domain.Tickets;

public enum OrigenAdjunto
{
    Descripcion,
    Comentario,
    Resolucion,
    Solucion,
}

public class Adjunto
{
    public int Id { get; private set; }
    public int? TicketId { get; private set; }
    public int? SolucionId { get; private set; }
    public int? ActividadId { get; private set; }
    public Actividad? Actividad { get; init; }
    public OrigenAdjunto Origen { get; init; }
    public required string Clave { get; init; }
    public required string Nombre { get; init; }
    public required string TipoContenido { get; init; }
    public required byte[] Contenido { get; init; }
    public int SubidoPorId { get; init; }
    public DateTime Fecha { get; init; }
}
