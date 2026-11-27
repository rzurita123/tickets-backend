using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController(IRepositorioUsuarios usuarios, IUsuarioActual actual) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<UsuarioDto>> Listar(
        [FromQuery] string? rol, [FromQuery] int? localidadId, [FromQuery] bool? activo, CancellationToken cancelacion)
    {
        var lector = await actual.ObtenerAsync(cancelacion);
        var lista = (await usuarios.ListarAsync(cancelacion)).AsEnumerable();

        if (!lector.EsOrmen)
        {
            // Un cliente necesita los nombres de quienes aparecen en sus tickets: su localidad y el personal de ORMEN.
            lista = lista.Where(u => u.EsOrmen || (lector.LocalidadId is not null && u.LocalidadId == lector.LocalidadId));
        }

        if (!string.IsNullOrEmpty(rol))
        {
            lista = lista.Where(u => string.Equals(u.Rol.ToString(), rol, StringComparison.OrdinalIgnoreCase));
        }

        if (localidadId is int l)
        {
            lista = lista.Where(u => u.LocalidadId == l);
        }

        if (activo is bool a)
        {
            lista = lista.Where(u => u.Activo == a);
        }

        return lista.Select(u => Dto(u, lector));
    }

    /// Los datos de contacto sólo los ve el personal de ORMEN y cada persona sobre sí misma (RNF1).
    private static UsuarioDto Dto(Usuario u, Usuario lector)
    {
        var dto = UsuarioDto.Desde(u);
        return lector.EsOrmen || u.Id == lector.Id ? dto : dto with { Email = string.Empty, Cargo = null };
    }
}