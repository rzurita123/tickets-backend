using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Application.Usuarios;

namespace SistemaTickets.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController(IRepositorioUsuarios usuarios) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<UsuarioDto>> Listar(CancellationToken cancelacion) =>
        (await usuarios.ListarAsync(cancelacion)).Select(UsuarioDto.Desde);
}
