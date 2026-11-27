using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Favoritos;
using SistemaTickets.Application.Soluciones;

namespace SistemaTickets.Api.Controllers;

/// Tipo "ticket" (con TicketId) o "solucion" (con el código de la solución).
public record FavoritoDto(string Tipo, int? TicketId, string? Solucion, DateTime Fecha);

[ApiController]
[Route("api/favoritos")]
[Authorize(Policy = Politicas.EsOrmen)]
public class FavoritosController(ServicioFavoritos favoritos, IRepositorioSoluciones soluciones, IUsuarioActual actual) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FavoritoDto>> Listar(CancellationToken cancelacion)
    {
        var lista = await favoritos.ListarAsync(await actual.ObtenerAsync(cancelacion), cancelacion);
        var codigos = (await soluciones.ListarAsync(cancelacion)).ToDictionary(s => s.Id, s => s.Codigo);
        return lista.Select(f => f.TicketId is int ticketId
            ? new FavoritoDto("ticket", ticketId, null, f.Fecha)
            : new FavoritoDto("solucion", null, codigos.GetValueOrDefault(f.SolucionId ?? 0), f.Fecha));
    }

    [HttpPut("tickets/{id:int}")]
    public Task<ActionResult> MarcarTicket(int id, CancellationToken cancelacion) => Ticket(id, true, cancelacion);

    [HttpDelete("tickets/{id:int}")]
    public Task<ActionResult> DesmarcarTicket(int id, CancellationToken cancelacion) => Ticket(id, false, cancelacion);

    [HttpPut("soluciones/{codigo}")]
    public Task<ActionResult> MarcarSolucion(string codigo, CancellationToken cancelacion) => Solucion(codigo, true, cancelacion);

    [HttpDelete("soluciones/{codigo}")]
    public Task<ActionResult> DesmarcarSolucion(string codigo, CancellationToken cancelacion) => Solucion(codigo, false, cancelacion);

    private async Task<ActionResult> Ticket(int id, bool marcado, CancellationToken cancelacion) =>
        await favoritos.MarcarTicketAsync(await actual.ObtenerAsync(cancelacion), id, marcado, cancelacion) ? NoContent() : NotFound();

    private async Task<ActionResult> Solucion(string codigo, bool marcado, CancellationToken cancelacion) =>
        await favoritos.MarcarSolucionAsync(await actual.ObtenerAsync(cancelacion), codigo, marcado, cancelacion) ? NoContent() : NotFound();
}
