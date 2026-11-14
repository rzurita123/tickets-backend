using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Tests;

public class ServicioIngresoTests
{
    private static readonly Usuario Operador = new()
    {
        Id = 1,
        NombreUsuario = "vpereira",
        Nombre = "Valeria Pereira",
        Email = "vpereira@ormen.example",
        HashContrasena = "hash:correcta",
        Rol = Rol.Operador,
    };

    private static ServicioIngreso Crear(params Usuario[] usuarios) =>
        new(new UsuariosFalsos(usuarios), new HashFalso(), new TokensFalsos());

    [Fact]
    public async Task Ingresa_con_usuario_y_contrasena_correctos()
    {
        var resultado = await Crear(Operador).IngresarAsync(" VPereira ", "correcta");

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Usuario.Id);
        Assert.Equal("token-1", resultado.Token.Valor);
    }

    [Fact]
    public async Task Rechaza_una_contrasena_incorrecta()
    {
        Assert.Null(await Crear(Operador).IngresarAsync("vpereira", "otra"));
    }

    [Fact]
    public async Task Rechaza_un_usuario_inexistente()
    {
        Assert.Null(await Crear(Operador).IngresarAsync("nadie", "correcta"));
    }

    [Fact]
    public async Task Rechaza_un_usuario_inactivo()
    {
        var inactivo = new Usuario
        {
            Id = 2,
            NombreUsuario = "inactivo",
            Nombre = "Inactivo",
            Email = "inactivo@ormen.example",
            HashContrasena = "hash:correcta",
            Activo = false,
        };

        Assert.Null(await Crear(inactivo).IngresarAsync("inactivo", "correcta"));
    }

    private sealed class UsuariosFalsos(Usuario[] usuarios) : IRepositorioUsuarios
    {
        public Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion = default) =>
            Task.FromResult(usuarios.FirstOrDefault(u => u.NombreUsuario == nombreUsuario));

        public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancelacion = default) =>
            Task.FromResult(usuarios.FirstOrDefault(u => u.Id == id));
    }

    private sealed class HashFalso : IHashContrasenas
    {
        public string Generar(string contrasena) => "hash:" + contrasena;

        public bool Verificar(string hash, string contrasena) => hash == Generar(contrasena);
    }

    private sealed class TokensFalsos : IGeneradorToken
    {
        public TokenAcceso Generar(Usuario usuario) => new("token-" + usuario.Id, DateTime.UnixEpoch);
    }
}
