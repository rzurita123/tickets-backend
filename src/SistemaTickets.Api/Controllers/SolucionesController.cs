using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Soluciones;

namespace SistemaTickets.Api.Controllers;

public record SolucionDto(
    string Codigo, string Titulo, string Descripcion, string? Sistema, string? Subsistema, IEnumerable<string> PalabrasClave,
    string Estado, int? TicketOrigenId, string? CreadaPor, DateTime CreadaEn, string? RevisadaPor, DateTime? RevisadaEn,
    string? MotivoRechazo, DateTime ActualizadaEn, IEnumerable<ImagenDto> Adjuntos, int Usos);

public record SolicitudSolucion(
    string? Titulo, string? Descripcion, string? Sistema, string? Subsistema, List<string>? PalabrasClave, List<ImagenSolicitud>? Adjuntos);

public record SolicitudPropuesta(
    int TicketId, string? Titulo, string? Descripcion, List<string>? PalabrasClave, List<ImagenSolicitud>? Adjuntos);

public record SolicitudRechazo(string? Motivo);

[ApiController]
[Route("api/soluciones")]
[Authorize(Policy = Politicas.EsOrmen)]
public class SolucionesController(
    ServicioSoluciones servicio, IUsuarioActual actual, IRepositorioUsuarios usuarios, ICatalogoLectura catalogos) : ControllerBase
{
    private Dictionary<int, string>? _usuarios;

    [HttpGet]
    public async Task<IEnumerable<SolucionDto>> Listar([FromQuery] string? estado, CancellationToken cancelacion)
    {
        var listado = await servicio.ListarAsync(await actual.ObtenerAsync(cancelacion), Estado(estado), cancelacion);
        var dtos = new List<SolucionDto>();
        foreach (var solucion in listado.Soluciones)
        {
            dtos.Add(await DtoAsync(solucion, listado.Usos, cancelacion));
        }

        return dtos;
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<SolucionDto>> Obtener(string codigo, CancellationToken cancelacion)
    {
        var solucion = await servicio.ObtenerAsync(await actual.ObtenerAsync(cancelacion), codigo, cancelacion);
        return solucion is null ? NotFound() : await DtoAsync(solucion, cancelacion);
    }

    [HttpPost]
    public async Task<SolucionDto> Crear(SolicitudSolucion s, CancellationToken cancelacion) =>
        await DtoAsync(await servicio.CrearAsync(await actual.ObtenerAsync(cancelacion), Datos(s), cancelacion), cancelacion);

    [HttpPut("{codigo}")]
    public async Task<ActionResult<SolucionDto>> Editar(string codigo, SolicitudSolucion s, CancellationToken cancelacion)
    {
        var solucion = await servicio.EditarAsync(await actual.ObtenerAsync(cancelacion), codigo, Datos(s), cancelacion);
        return solucion is null ? NotFound() : await DtoAsync(solucion, cancelacion);
    }

    /// Propone la resolución de un ticket cerrado como solución reutilizable.
    [HttpPost("propuestas")]
    public async Task<ActionResult<SolucionDto>> Proponer(SolicitudPropuesta s, CancellationToken cancelacion)
    {
        var solucion = await servicio.ProponerAsync(
            await actual.ObtenerAsync(cancelacion),
            new DatosPropuesta(s.TicketId, s.Titulo, s.Descripcion, s.PalabrasClave, s.Adjuntos is null ? null : Imagenes(s.Adjuntos)),
            cancelacion);
        return solucion is null ? NotFound() : await DtoAsync(solucion, cancelacion);
    }

    /// Con cambios, se aprueba la versión corregida.
    [HttpPost("{codigo}/aprobar")]
    public async Task<ActionResult<SolucionDto>> Aprobar(
        string codigo, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SolicitudSolucion? cambios, CancellationToken cancelacion)
    {
        var solucion = await servicio.RevisarAsync(
            await actual.ObtenerAsync(cancelacion), codigo, aprobar: true, null, cambios is null ? null : Datos(cambios), cancelacion);
        return solucion is null ? NotFound() : await DtoAsync(solucion, cancelacion);
    }

    [HttpPost("{codigo}/rechazar")]
    public async Task<ActionResult<SolucionDto>> Rechazar(string codigo, SolicitudRechazo s, CancellationToken cancelacion)
    {
        var solucion = await servicio.RevisarAsync(
            await actual.ObtenerAsync(cancelacion), codigo, aprobar: false, s.Motivo, null, cancelacion);
        return solucion is null ? NotFound() : await DtoAsync(solucion, cancelacion);
    }

    public static string EstadoTexto(EstadoSolucion estado) => estado.ToString().ToUpperInvariant();

    private static EstadoSolucion? Estado(string? estado) =>
        Enum.TryParse<EstadoSolucion>(estado, ignoreCase: true, out var valor) ? valor : null;

    private static DatosSolucion Datos(SolicitudSolucion s) =>
        new(s.Titulo, s.Descripcion, s.Sistema, s.Subsistema, s.PalabrasClave, s.Adjuntos is null ? null : Imagenes(s.Adjuntos));

    /// Las imágenes que ya tenía la solución pueden venir sólo con su id.
    private static ImagenesSolucion Imagenes(List<ImagenSolicitud> lista) =>
        new(
            MapeadorTickets.Imagenes(lista.Where(i => !string.IsNullOrEmpty(i.DataUrl)).ToList()),
            lista.Where(i => string.IsNullOrEmpty(i.DataUrl) && !string.IsNullOrWhiteSpace(i.Id)).Select(i => i.Id!.Trim()).ToList());

    private async Task<SolucionDto> DtoAsync(Solucion s, CancellationToken cancelacion) =>
        await DtoAsync(s, await servicio.ContarUsosAsync(cancelacion), cancelacion);

    private async Task<SolucionDto> DtoAsync(Solucion s, IReadOnlyDictionary<int, int> usos, CancellationToken cancelacion)
    {
        _usuarios ??= (await usuarios.ListarAsync(cancelacion)).ToDictionary(u => u.Id, u => u.NombreUsuario);
        return new SolucionDto(
            s.Codigo, s.Titulo, s.Texto,
            await catalogos.CodigoAsync(TipoCatalogo.Sistemas, s.SistemaId, cancelacion),
            await catalogos.CodigoAsync(TipoCatalogo.Subsistemas, s.SubsistemaId, cancelacion),
            s.PalabrasClave, EstadoTexto(s.Estado), s.TicketOrigenId,
            _usuarios.GetValueOrDefault(s.PropuestaPorId), s.CreadaEn,
            s.RevisadaPorId is int r ? _usuarios.GetValueOrDefault(r) : null, s.RevisadaEn, s.MotivoRechazo, s.ActualizadaEn,
            s.Adjuntos.OrderBy(a => a.Id).Select(a => new ImagenDto(
                a.Clave, a.Nombre, $"data:{a.TipoContenido};base64,{Convert.ToBase64String(a.Contenido)}",
                _usuarios.GetValueOrDefault(a.SubidoPorId), a.Fecha)).ToList(),
            usos.GetValueOrDefault(s.Id));
    }
}
