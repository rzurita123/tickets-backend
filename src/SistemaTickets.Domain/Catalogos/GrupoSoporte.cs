namespace SistemaTickets.Domain.Catalogos;

public class GrupoSoporte
{
    public int Id { get; private set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
}
