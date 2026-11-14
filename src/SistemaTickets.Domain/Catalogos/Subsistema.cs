namespace SistemaTickets.Domain.Catalogos;

public class Subsistema
{
    public int Id { get; private set; }
    public int SistemaId { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
}
