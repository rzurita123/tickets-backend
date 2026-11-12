namespace SistemaTickets.Domain.Catalogos;

public class Sistema
{
    public int Id { get; private set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
}
