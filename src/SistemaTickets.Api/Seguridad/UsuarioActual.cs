using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Seguridad;

public class NoAutenticadoException() : Exception("Sesión inválida.");

public interface IUsuarioActual
{
    Task<Usuario> ObtenerAsync(CancellationToken cancelacion = default);
}

/// Scoped a propósito: guarda el usuario de ESTA request. Como singleton mezclaría usuarios entre requests.
public sealed class UsuarioActualHttp(IHttpContextAccessor http, IRepositorioUsuarios usuarios) : IUsuarioActual
{
    private Usuario? _usuario;

    public async Task<Usuario> ObtenerAsync(CancellationToken cancelacion = default)
    {
        if (_usuario is not null)
        {
            return _usuario;
        }

        var id = http.HttpContext?.User.IdUsuario();
        var usuario = id is int valor ? await usuarios.BuscarPorIdAsync(valor, cancelacion) : null;
        if (usuario is not { Activo: true })
        {
            throw new NoAutenticadoException();
        }

        return _usuario = usuario;
    }
}