using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Auditoria;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Controllers;

public record SolicitudIngreso(string? Usuario, string? Contrasena);

public record UsuarioDto(int Id, string Usuario, string Nombre, string Email, string Rol, string? Cargo, int? LocalidadId, bool Activo)
{
    public static UsuarioDto Desde(Usuario u) =>
        new(u.Id, u.NombreUsuario, u.Nombre, u.Email, u.Rol.ToString().ToUpperInvariant(), u.Cargo, u.LocalidadId, u.Activo);
}

public record RespuestaIngreso(string Token, DateTime Expira, UsuarioDto Usuario);

[ApiController]
[Route("api/auth")]
public class AuthController(ServicioIngreso ingreso, IRepositorioUsuarios usuarios, IAuditoria auditoria) : ControllerBase
{
    [HttpPost("ingresar")]
    [AllowAnonymous]
    public async Task<ActionResult<RespuestaIngreso>> Ingresar(SolicitudIngreso solicitud, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Usuario) || string.IsNullOrWhiteSpace(solicitud.Contrasena))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Usuario y contraseña son obligatorios.");
        }

        var resultado = await ingreso.IngresarAsync(solicitud.Usuario, solicitud.Contrasena, cancelacion);
        if (resultado is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Usuario o contraseña incorrectos.");
        }

        auditoria.Registrar(resultado.Usuario.Id, OperacionesAuditoria.Login, detalle: "Usuario y contraseña");
        await auditoria.GuardarAsync(cancelacion);
        return new RespuestaIngreso(resultado.Token.Valor, resultado.Token.Expira, UsuarioDto.Desde(resultado.Usuario));
    }

    [HttpPost("salir")]
    [Authorize]
    public async Task<ActionResult> Salir(CancellationToken cancelacion)
    {
        if (User.IdUsuario() is int id)
        {
            auditoria.Registrar(id, OperacionesAuditoria.Logout);
            await auditoria.GuardarAsync(cancelacion);
        }

        return NoContent();
    }

    [HttpGet("yo")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> Yo(CancellationToken cancelacion)
    {
        var usuario = User.IdUsuario() is int id ? await usuarios.BuscarPorIdAsync(id, cancelacion) : null;
        if (usuario is null || !usuario.Activo)
        {
            return Unauthorized();
        }

        return UsuarioDto.Desde(usuario);
    }
}
