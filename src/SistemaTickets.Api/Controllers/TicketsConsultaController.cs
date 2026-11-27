using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Domain.Tickets;

namespace SistemaTickets.Api.Controllers;

/// Lado de lectura de tickets (listado filtrado, conteos por estado y detalle).
[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsConsultaController(ServicioConsultaTickets consulta, MapeadorTickets mapeador, IUsuarioActual actual) : ControllerBase
{
    [HttpGet("listado")]
    public async Task<PaginaDto<TicketResumenDto>> Listado([FromQuery] FiltroTickets filtro, CancellationToken cancelacion)
    {
        var usuario = await actual.ObtenerAsync(cancelacion);
        var pagina = await consulta.ListarAsync(usuario, filtro, cancelacion);
        return new(await mapeador.ResumenesAsync(pagina.Items, cancelacion), pagina.Total, pagina.Pagina, pagina.PorPagina);
    }

    [HttpGet("resumen")]
    public async Task<Dictionary<string, int>> Resumen([FromQuery] FiltroTickets filtro, CancellationToken cancelacion)
    {
        var usuario = await actual.ObtenerAsync(cancelacion);
        return Chips(await consulta.ContarAsync(usuario, filtro, cancelacion));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDto>> Obtener(int id, CancellationToken cancelacion)
    {
        var usuario = await actual.ObtenerAsync(cancelacion);
        var ticket = await consulta.ObtenerAsync(usuario, id, cancelacion);
        return ticket is null ? NotFound() : await mapeador.DtoAsync(ticket, usuario, cancelacion);
    }

    private static Dictionary<string, int> Chips(IReadOnlyDictionary<EstadoTicket, int> n)
    {
        int De(params EstadoTicket[] estados) => estados.Sum(e => n.GetValueOrDefault(e));

        return new()
        {
            ["CERRADO"] = De(EstadoTicket.Cerrado),
            ["en-curso"] = De(EstadoTicket.Abierto, EstadoTicket.EnProceso, EstadoTicket.Bloqueado),
            ["ABIERTO"] = De(EstadoTicket.Abierto),
            ["EN_PROCESO"] = De(EstadoTicket.EnProceso),
            ["BLOQUEADO"] = De(EstadoTicket.Bloqueado),
            ["todos"] = n.Values.Sum(),
        };
    }
}