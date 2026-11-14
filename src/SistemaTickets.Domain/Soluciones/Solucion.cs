namespace SistemaTickets.Domain.Soluciones;

public class Solucion
{
    public int Id { get; private set; }
    public required string Titulo { get; set; }
    public required string Texto { get; set; }
    public EstadoSolucion Estado { get; set; } = EstadoSolucion.Pendiente;
    public int? TicketOrigenId { get; set; }
    public int PropuestaPorId { get; set; }
    public int? RevisadaPorId { get; set; }
    public DateTime CreadaEn { get; set; }
}
