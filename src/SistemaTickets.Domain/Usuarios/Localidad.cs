namespace SistemaTickets.Domain.Usuarios;

public class Localidad
{
    public int Id { get; private set; }
    public required string Nombre { get; set; }
    public bool Activa { get; set; } = true;
}
