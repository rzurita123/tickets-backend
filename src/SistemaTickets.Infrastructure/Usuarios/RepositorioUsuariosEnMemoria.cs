using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Infrastructure.Usuarios;

public class RepositorioUsuariosEnMemoria : IRepositorioUsuarios
{
    private readonly List<Usuario> _usuarios;

    public RepositorioUsuariosEnMemoria(IHashContrasenas hash, string contrasenaInicial)
    {
        var hashInicial = hash.Generar(contrasenaInicial);

        Usuario Crear(int id, string nombreUsuario, string nombre, string email, Rol rol, int? localidadId = null) => new()
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            Nombre = nombre,
            Email = email,
            HashContrasena = hashInicial,
            Rol = rol,
            LocalidadId = localidadId,
        };

        _usuarios =
        [
            Crear(1, "vpereira", "Valeria Pereira", "vpereira@ormen.example", Rol.Operador),
            Crear(2, "nacosta", "Nicolás Acosta", "nacosta@ormen.example", Rol.Operador),
            Crear(3, "scabrera", "Silvia Cabrera", "scabrera@ormen.example", Rol.Administrador),
            Crear(4, "ralvarez", "Rodrigo Álvarez", "ralvarez@ormen.example", Rol.Administrador),
            Crear(5, "mtechera", "Marcelo Techera", "mtechera@pando.example", Rol.Cliente, localidadId: 1),
            Crear(6, "lgomez", "Laura Gómez", "lgomez@lagomar.example", Rol.Cliente, localidadId: 2),
        ];
    }

    public Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario));

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));
}
