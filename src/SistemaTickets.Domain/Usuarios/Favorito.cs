namespace SistemaTickets.Domain.Usuarios;

public class Favorito
{
    public int Id { get; private set; }
    public int UsuarioId { get; init; }
    public int? TicketId { get; init; }
    public int? SolucionId { get; init; }
    public DateTime Fecha { get; init; }
}
