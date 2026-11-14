namespace SistemaTickets.Domain.Usuarios;

public class Localidad
{
    public int Id { get; init; }
    public required string Nombre { get; set; }
    public bool Activa { get; set; } = true;
}
