namespace SistemaTickets.Domain.Usuarios;

public class Usuario
{
    public int Id { get; private set; }
    public required string Nombre { get; set; }
    public required string Email { get; set; }
    public Rol Rol { get; set; }
    public int? LocalidadId { get; set; }
    public bool Activo { get; set; } = true;

    public bool EsOrmen => Rol is Rol.Operador or Rol.Administrador;
}
