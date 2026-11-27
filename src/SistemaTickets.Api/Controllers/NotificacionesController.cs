using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Notificaciones;
using SistemaTickets.Application.Usuarios;

namespace SistemaTickets.Api.Controllers;

public record NotificacionDto(
    int Id, DateTime Fecha, int? TicketId, string? Para, string Destino, string Canal, string Asunto, string Cuerpo, string Estado);

/// Correos a las localidades. Un cliente ve los suyos; ORMEN, todos.
[ApiController]
[Route("api/notificaciones")]
[Authorize]
public class NotificacionesController(INotificaciones notificaciones, IRepositorioUsuarios usuarios, IUsuarioActual actual) : ControllerBase
{
    private const int Limite = 1500;

    [HttpGet]
    public async Task<IEnumerable<NotificacionDto>> Listar(CancellationToken cancelacion)
    {
        var u = await actual.ObtenerAsync(cancelacion);
        var lista = await notificaciones.ListarAsync(u.EsOrmen ? null : u.Id, Limite, cancelacion);
        var nombres = (await usuarios.ListarAsync(cancelacion)).ToDictionary(x => x.Id, x => x.NombreUsuario);
        return lista.Select(n => new NotificacionDto(
            n.Id, n.CreadaEn, n.TicketId, n.DestinatarioId is int id ? nombres.GetValueOrDefault(id) : null,
            n.Destino, n.Canal.ToString().ToUpperInvariant(), n.Asunto, n.Cuerpo, n.Estado.ToString().ToUpperInvariant())).ToList();
    }
}
