namespace SistemaTickets.Domain.Catalogos;

public abstract class ItemCatalogo
{
    public int Id { get; set; }
    public required string Codigo { get; init; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
}
