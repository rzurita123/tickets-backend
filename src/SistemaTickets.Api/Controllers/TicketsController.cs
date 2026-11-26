using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController(ServicioTickets tickets, MapeadorTickets mapeador, IRepositorioUsuarios usuarios) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> Listar(CancellationToken cancelacion, [FromQuery] bool incluirBorradores = true) =>
        ConUsuario(async u => Ok(await mapeador.DtosAsync(await tickets.ListarAsync(u, incluirBorradores, cancelacion), u, cancelacion)), cancelacion);

    [HttpPost]
    public Task<ActionResult> Crear(SolicitudTicket s, CancellationToken cancelacion) =>
        Ticket(u => tickets.CrearAsync(
            u, Datos(s),
            new OpcionesAlta(s.BorradorId, s.RegistroDirecto, s.Privado, s.SolucionTexto, s.SolucionCatalogoId, MapeadorTickets.Imagenes(s.SolucionAdjuntos)),
            cancelacion)!, cancelacion);

    [HttpPost("borradores")]
    public Task<ActionResult> CrearBorrador(SolicitudTicket s, CancellationToken cancelacion) =>
        Ticket(u => tickets.GuardarBorradorAsync(u, null, Datos(s), cancelacion)!, cancelacion);

    [HttpPut("borradores/{id:int}")]
    public Task<ActionResult> GuardarBorrador(int id, SolicitudTicket s, CancellationToken cancelacion) =>
        Ticket(u => tickets.GuardarBorradorAsync(u, id, Datos(s), cancelacion)!, cancelacion);

    [HttpDelete("borradores/{id:int}")]
    public Task<ActionResult> DescartarBorrador(int id, CancellationToken cancelacion) =>
        ConUsuario(async u => await tickets.DescartarBorradorAsync(u, id, cancelacion) ? NoContent() : NotFound(), cancelacion);

    [HttpPost("{id:int}/comentarios")]
    public Task<ActionResult> Comentar(int id, SolicitudComentario s, CancellationToken cancelacion) =>
        Ticket(u => tickets.ComentarAsync(u, id, s.Texto, s.Privado, MapeadorTickets.Imagenes(s.Adjuntos), cancelacion), cancelacion);

    [HttpPost("{id:int}/asignar")]
    public Task<ActionResult> Asignar(int id, SolicitudAsignar s, CancellationToken cancelacion) =>
        Ticket(u => tickets.AsignarAsync(u, id, s.Operador, cancelacion), cancelacion);

    [HttpPost("{id:int}/liberar")]
    public Task<ActionResult> Liberar(int id, CancellationToken cancelacion) =>
        Ticket(u => tickets.LiberarAsync(u, id, cancelacion), cancelacion);

    [HttpPost("{id:int}/elevar")]
    public Task<ActionResult> Elevar(int id, SolicitudElevar s, CancellationToken cancelacion) =>
        Ticket(u => tickets.ElevarAsync(u, id, s.Grupo, s.Usuario, s.Motivo, cancelacion), cancelacion);

    [HttpPost("{id:int}/bloquear")]
    public Task<ActionResult> Bloquear(int id, SolicitudBloqueo s, CancellationToken cancelacion) =>
        Ticket(u => tickets.BloquearAsync(u, id, s.Texto, s.Privado, cancelacion), cancelacion);

    [HttpPost("{id:int}/desbloquear")]
    public Task<ActionResult> Desbloquear(int id, SolicitudTexto s, CancellationToken cancelacion) =>
        Ticket(u => tickets.DesbloquearAsync(u, id, s.Texto, cancelacion), cancelacion);

    [HttpPut("{id:int}/clasificacion")]
    public Task<ActionResult> Reclasificar(int id, SolicitudClasificacion s, CancellationToken cancelacion) =>
        Ticket(u => tickets.ReclasificarAsync(u, id, s.Sistema, s.Subsistema, s.Criticidad, s.TipoProblema, cancelacion), cancelacion);

    [HttpPost("{id:int}/cerrar")]
    public Task<ActionResult> Cerrar(int id, SolicitudCierre s, CancellationToken cancelacion) =>
        Ticket(u => tickets.CerrarAsync(
            u, id, new DatosCierre(s.Texto, s.MensajePublico, s.TipoProblema, s.SolucionCatalogoId, MapeadorTickets.Imagenes(s.Adjuntos)), cancelacion), cancelacion);

    [HttpPost("{id:int}/reabrir")]
    public Task<ActionResult> Reabrir(int id, SolicitudTexto s, CancellationToken cancelacion) =>
        Ticket(u => tickets.ReabrirAsync(u, id, s.Texto, cancelacion), cancelacion);

    [HttpPut("{id:int}/privado")]
    public Task<ActionResult> CambiarPrivado(int id, SolicitudPrivado s, CancellationToken cancelacion) =>
        Ticket(u => tickets.CambiarPrivadoAsync(u, id,
            s.Privado ?? throw new ErrorTicket("Indicá la visibilidad del ticket.", "privado"), cancelacion), cancelacion);

    private static DatosTicket Datos(SolicitudTicket s) =>
        new(s.Titulo, s.Descripcion, s.LocalidadId, s.Sistema, s.Subsistema, s.Criticidad, s.TipoProblema, MapeadorTickets.Imagenes(s.Adjuntos));

    private Task<ActionResult> Ticket(Func<Usuario, Task<Ticket?>> accion, CancellationToken cancelacion) =>
        ConUsuario(async u =>
        {
            var ticket = await accion(u);
            return ticket is null ? NotFound() : Ok(await mapeador.DtoAsync(ticket, u, cancelacion));
        }, cancelacion);

    private async Task<ActionResult> ConUsuario(Func<Usuario, Task<ActionResult>> accion, CancellationToken cancelacion)
    {
        var usuario = User.IdUsuario() is int id ? await usuarios.BuscarPorIdAsync(id, cancelacion) : null;
        if (usuario is null || !usuario.Activo)
        {
            return Unauthorized();
        }

        try
        {
            return await accion(usuario);
        }
        catch (ErrorTicket e)
        {
            if (e.Campo is string campo)
            {
                ModelState.AddModelError(campo, e.Message);
                return ValidationProblem(title: e.Message, modelStateDictionary: ModelState);
            }

            return Problem(statusCode: StatusCodes.Status400BadRequest, title: e.Message);
        }
        catch (SinPermisoException e)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: e.Message);
        }
    }
}
