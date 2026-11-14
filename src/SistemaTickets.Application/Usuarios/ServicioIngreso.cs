using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Usuarios;

public record ResultadoIngreso(Usuario Usuario, TokenAcceso Token);

public class ServicioIngreso(IRepositorioUsuarios usuarios, IHashContrasenas hash, IGeneradorToken tokens)
{
    public async Task<ResultadoIngreso?> IngresarAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        var usuario = await usuarios.BuscarPorNombreUsuarioAsync(nombreUsuario.Trim().ToLowerInvariant(), cancelacion);

        if (usuario is null || !usuario.Activo || !hash.Verificar(usuario.HashContrasena, contrasena))
        {
            return null;
        }

        return new ResultadoIngreso(usuario, tokens.Generar(usuario));
    }
}
