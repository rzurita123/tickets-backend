using SistemaTickets.Domain.Tickets;

namespace SistemaTickets.Domain.Soluciones;

public class Solucion
{
    public int Id { get; private set; }
    public required string Codigo { get; init; }
    public required string Titulo { get; set; }
    public required string Texto { get; set; }
    public EstadoSolucion Estado { get; set; } = EstadoSolucion.Pendiente;
    public int? SistemaId { get; set; }
    public int? SubsistemaId { get; set; }
    public List<string> PalabrasClave { get; set; } = [];
    public int? TicketOrigenId { get; set; }
    public int PropuestaPorId { get; set; }
    public int? RevisadaPorId { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime CreadaEn { get; set; }
    public DateTime ActualizadaEn { get; set; }
    public DateTime? RevisadaEn { get; set; }
    public List<Adjunto> Adjuntos { get; } = [];
}
