using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Infrastructure.Usuarios;

public class RepositorioUsuariosEnMemoria : IRepositorioUsuarios
{
    private readonly List<Usuario> _usuarios;

    public RepositorioUsuariosEnMemoria(IHashContrasenas hash, string contrasenaInicial)
    {
        var hashInicial = hash.Generar(contrasenaInicial);

        Usuario Crear(int id, string nombreUsuario, string nombre, string email, Rol rol, string cargo, int? localidadId = null) => new()
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            Nombre = nombre,
            Email = email,
            HashContrasena = hashInicial,
            Rol = rol,
            Cargo = cargo,
            LocalidadId = localidadId,
        };

        _usuarios =
        [
            Crear(1, "vpereira", "Valeria Pereira", "vpereira@ormen.example", Rol.Operador, "Mesa de ayuda"),
            Crear(2, "nacosta", "Nicolás Acosta", "nacosta@ormen.example", Rol.Operador, "Mesa de ayuda"),
            Crear(3, "scabrera", "Silvia Cabrera", "scabrera@ormen.example", Rol.Administrador, "Gerencia de operaciones"),
            Crear(4, "ralvarez", "Rodrigo Álvarez", "ralvarez@ormen.example", Rol.Administrador, "Responsable de sistemas"),
            Crear(5, "mtechera", "Marcelo Techera", "mtechera@pando.example", Rol.Cliente, "Encargado", localidadId: 7),
            Crear(6, "lgomez", "Laura Gómez", "lgomez@lagomar.example", Rol.Cliente, "Encargada", localidadId: 6),
        ];
    }

    public Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario));

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancelacion = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Usuario>>(_usuarios);
}
