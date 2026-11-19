namespace SistemaTickets.Domain.Auditoria;

public class RegistroAuditoria
{
    public long Id { get; private set; }
    public DateTime Fecha { get; init; }
    public int? UsuarioId { get; init; }
    public required string Operacion { get; init; }
    public string? Entidad { get; init; }
    public string? EntidadId { get; init; }
    public string? Detalle { get; init; }
}
