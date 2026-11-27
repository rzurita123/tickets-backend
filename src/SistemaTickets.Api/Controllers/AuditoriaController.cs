using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Auditoria;
using SistemaTickets.Application.Usuarios;

namespace SistemaTickets.Api.Controllers;

public record RegistroAuditoriaDto(
    long Id, DateTime Fecha, string? Usuario, string Operacion, string? Entidad, string? EntidadId, int? TicketId, string? Detalle);

public record SolicitudAccesoDenegado(string? Pantalla);

[ApiController]
[Route("api/auditoria")]
[Authorize]
public class AuditoriaController(IAuditoria auditoria, IRepositorioUsuarios usuarios, IUsuarioActual actual) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Politicas.EsAdministrador)]
    public async Task<ActionResult<IEnumerable<RegistroAuditoriaDto>>> Listar(
        [FromQuery] string? operacion, [FromQuery] string? usuario, [FromQuery] int? ticketId,
        [FromQuery] DateTimeOffset? desde, [FromQuery] DateTimeOffset? hasta, [FromQuery] int limite = 500,
        CancellationToken cancelacion = default)
    {
        var lista = await usuarios.ListarAsync(cancelacion);
        int? usuarioId = null;
        if (!string.IsNullOrWhiteSpace(usuario))
        {
            usuarioId = lista.FirstOrDefault(u => u.NombreUsuario == usuario.Trim())?.Id;
            if (usuarioId is null)
            {
                return Array.Empty<RegistroAuditoriaDto>();
            }
        }

        var registros = await auditoria.ListarAsync(new FiltroAuditoria(
            Vacio(operacion), usuarioId,
            ticketId is null ? null : EntidadesAuditoria.Ticket, ticketId?.ToString(),
            desde?.UtcDateTime, hasta?.UtcDateTime, limite), cancelacion);
        var nombres = lista.ToDictionary(u => u.Id, u => u.NombreUsuario);
        return registros.Select(r => new RegistroAuditoriaDto(
            r.Id, r.Fecha, r.UsuarioId is int id ? nombres.GetValueOrDefault(id) : null, r.Operacion, r.Entidad, r.EntidadId,
            r.Entidad == EntidadesAuditoria.Ticket && int.TryParse(r.EntidadId, out var t) ? t : null,
            r.Detalle)).ToList();
    }

    /// El frontend avisa cuando alguien intenta abrir una pantalla que su rol no tiene.
    [HttpPost("acceso-denegado")]
    public async Task<ActionResult> AccesoDenegado(SolicitudAccesoDenegado s, CancellationToken cancelacion)
    {
        var u = await actual.ObtenerAsync(cancelacion);
        auditoria.Registrar(u.Id, OperacionesAuditoria.AccesoDenegado, detalle: $"Intentó abrir la pantalla {TextoAuditoria.Truncar(s.Pantalla, 200)}");
        await auditoria.GuardarAsync(cancelacion);
        return NoContent();
    }

    private static string? Vacio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
