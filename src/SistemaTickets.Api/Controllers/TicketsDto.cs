using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Controllers;

public record ImagenDto(string Id, string Nombre, string DataUrl, string? Autor, DateTime Fecha);

public record DestinoDto(string Tipo, string Id);

public record ActividadDto(
    int Id, string Tipo, DateTime Fecha, string? Autor, string? Texto, bool Privada, string? De, string? A,
    string? Operador, DestinoDto? Destino, string? Detalle, IEnumerable<ImagenDto> Adjuntos);

public record SolucionTicketDto(
    string Texto, bool EnDescripcion, string? SolucionCatalogoId, string? Autor, DateTime? Fecha, IEnumerable<ImagenDto> Adjuntos);

public record TicketDto(
    int Id, int? Numero, string Estado, string Titulo, string Descripcion, bool Privado, bool RegistroDirecto,
    int? LocalidadId, string? Sistema, string? Subsistema, string? Criticidad, string? TipoProblema,
    string? CreadoPor, string? Operador, DestinoDto? ElevadoA,
    DateTime CreadoEn, DateTime ActualizadoEn, DateTime? CerradoEn,
    IEnumerable<ImagenDto> Adjuntos, SolucionTicketDto? Solucion, IEnumerable<ActividadDto> Actividad);

public record ImagenSolicitud(string? Id, string? Nombre, string? DataUrl);

public record SolicitudTicket(
    string? Titulo, string? Descripcion, int? LocalidadId, string? Sistema, string? Subsistema, string? Criticidad,
    string? TipoProblema, List<ImagenSolicitud>? Adjuntos, int? BorradorId, bool RegistroDirecto, bool Privado,
    string? SolucionTexto, string? SolucionCatalogoId, List<ImagenSolicitud>? SolucionAdjuntos);

public record SolicitudComentario(string? Texto, bool Privado, List<ImagenSolicitud>? Adjuntos);

public record SolicitudAsignar(string? Operador);

public record SolicitudElevar(string? Grupo, string? Usuario, string? Motivo);

public record SolicitudBloqueo(string? Texto, bool Privado);

public record SolicitudTexto(string? Texto);

public record SolicitudClasificacion(string? Sistema, string? Subsistema, string? Criticidad, string? TipoProblema);

public record SolicitudCierre(string? Texto, string? MensajePublico, string? TipoProblema, string? SolucionCatalogoId, List<ImagenSolicitud>? Adjuntos);

public record SolicitudPrivado(bool? Privado);

public class MapeadorTickets(IRepositorioUsuarios usuarios, IRepositorioCatalogos catalogos, IRepositorioSoluciones soluciones)
{
    private const int TamanoMaximoImagen = 5 * 1024 * 1024;

    private static readonly TipoActividad[] SinTextoParaCliente =
        [TipoActividad.Reapertura, TipoActividad.Cierre, TipoActividad.Elevacion, TipoActividad.Desbloqueo];

    private Dictionary<int, string>? _usuarios;
    private Dictionary<int, string>? _soluciones;
    private readonly Dictionary<TipoCatalogo, Dictionary<int, string>> _codigos = [];

    public static IReadOnlyList<ImagenNueva> Imagenes(List<ImagenSolicitud>? lista) =>
        (lista ?? []).Select(Imagen).ToList();

    public async Task<List<TicketDto>> DtosAsync(IEnumerable<Ticket> tickets, Usuario lector, CancellationToken cancelacion)
    {
        var dtos = new List<TicketDto>();
        foreach (var ticket in tickets)
        {
            dtos.Add(await DtoAsync(ticket, lector, cancelacion));
        }

        return dtos;
    }

    public async Task<TicketDto> DtoAsync(Ticket t, Usuario lector, CancellationToken cancelacion)
    {
        _usuarios ??= (await usuarios.ListarAsync(cancelacion)).ToDictionary(u => u.Id, u => u.NombreUsuario);
        _soluciones ??= (await soluciones.ListarAsync(cancelacion)).ToDictionary(s => s.Id, s => s.Codigo);
        var esCliente = !lector.EsOrmen;

        var actividades = t.Actividades
            .Where(a => !esCliente || !(a.Tipo == TipoActividad.Privacidad || (a.Tipo == TipoActividad.Comentario && a.Privada)))
            .OrderBy(a => a.Fecha).ThenBy(a => a.Id)
            .ToList();
        var visibles = actividades.Select(a => a.Id).ToHashSet();

        IEnumerable<ImagenDto> Adjuntos(OrigenAdjunto origen, int? actividadId = null) => t.Adjuntos
            .Where(x => x.Origen == origen && (origen != OrigenAdjunto.Comentario || x.ActividadId == actividadId))
            .OrderBy(x => x.Id)
            .Select(x => new ImagenDto(
                x.Clave, x.Nombre, $"data:{x.TipoContenido};base64,{Convert.ToBase64String(x.Contenido)}",
                Usuario(x.SubidoPorId), x.Fecha));

        var actividad = new List<ActividadDto>();
        foreach (var a in actividades)
        {
            var ocultarTexto = esCliente && (SinTextoParaCliente.Contains(a.Tipo) || (a.Tipo == TipoActividad.Bloqueo && a.Privada));
            actividad.Add(new ActividadDto(
                a.Id, a.Tipo.ToString(), a.Fecha, Usuario(a.AutorId), ocultarTexto ? null : a.Texto, a.Privada,
                Estado(a.EstadoAnterior), Estado(a.EstadoNuevo), Usuario(a.OperadorId),
                await DestinoAsync(a.DestinoGrupoId, a.DestinoUsuarioId, cancelacion), a.Detalle,
                a.Tipo == TipoActividad.Comentario ? Adjuntos(OrigenAdjunto.Comentario, a.Id).ToList() : []));
        }

        SolucionTicketDto? solucion = null;
        if (!esCliente && t.Resolucion is not null)
        {
            var cierre = t.Actividades.Where(a => a.Tipo == TipoActividad.Cierre).OrderBy(a => a.Fecha).ThenBy(a => a.Id).LastOrDefault();
            solucion = new SolucionTicketDto(
                t.Resolucion, t.ResolucionEnDescripcion, t.SolucionAplicadaId is int sid ? _soluciones.GetValueOrDefault(sid) : null, Usuario(cierre?.AutorId), cierre?.Fecha,
                Adjuntos(OrigenAdjunto.Resolucion).ToList());
        }

        return new TicketDto(
            t.Id, t.Numero, Estado(t.Estado)!, t.Titulo, t.Descripcion, t.Privado, t.RegistroDirecto, t.LocalidadId,
            await CodigoAsync(TipoCatalogo.Sistemas, t.SistemaId, cancelacion),
            await CodigoAsync(TipoCatalogo.Subsistemas, t.SubsistemaId, cancelacion),
            await CodigoAsync(TipoCatalogo.Criticidades, t.CriticidadId, cancelacion),
            await CodigoAsync(TipoCatalogo.TiposProblema, t.TipoProblemaId, cancelacion),
            Usuario(t.CreadoPorId), Usuario(t.OperadorId),
            await DestinoAsync(t.ElevadoAGrupoId, t.ElevadoAUsuarioId, cancelacion),
            t.CreadoEn, t.ActualizadoEn, t.CerradoEn,
            Adjuntos(OrigenAdjunto.Descripcion).ToList(), solucion, actividad);
    }

    private static ImagenNueva Imagen(ImagenSolicitud i)
    {
        const string base64 = ";base64";
        var dataUrl = i.DataUrl ?? string.Empty;
        var coma = dataUrl.IndexOf(',', StringComparison.Ordinal);
        if (!dataUrl.StartsWith("data:image/", StringComparison.Ordinal) || coma < 0 || string.IsNullOrWhiteSpace(i.Id))
        {
            throw new ErrorTicket("Una de las imágenes no es válida.");
        }

        var cabecera = dataUrl[5..coma];
        var datos = dataUrl[(coma + 1)..];
        var esBase64 = cabecera.EndsWith(base64, StringComparison.Ordinal);
        byte[] contenido;
        try
        {
            contenido = esBase64 ? Convert.FromBase64String(datos) : System.Text.Encoding.UTF8.GetBytes(Uri.UnescapeDataString(datos));
        }
        catch (FormatException)
        {
            throw new ErrorTicket("Una de las imágenes no es válida.");
        }

        if (contenido.Length > TamanoMaximoImagen)
        {
            throw new ErrorTicket("Una de las imágenes es demasiado grande.");
        }

        return new ImagenNueva(
            i.Id.Trim(), string.IsNullOrWhiteSpace(i.Nombre) ? "imagen" : i.Nombre.Trim(),
            esBase64 ? cabecera[..^base64.Length] : cabecera, contenido);
    }

    private static string? Estado(EstadoTicket? estado) => estado switch
    {
        null => null,
        EstadoTicket.EnProceso => "EN_PROCESO",
        _ => estado.Value.ToString().ToUpperInvariant(),
    };

    private string? Usuario(int? id) => id is int valor && _usuarios!.TryGetValue(valor, out var nombre) ? nombre : null;

    private async Task<DestinoDto?> DestinoAsync(int? grupoId, int? usuarioId, CancellationToken cancelacion)
    {
        if (grupoId is not null)
        {
            var codigo = await CodigoAsync(TipoCatalogo.GruposSoporte, grupoId, cancelacion);
            return codigo is null ? null : new DestinoDto("grupo", codigo);
        }

        var usuario = Usuario(usuarioId);
        return usuario is null ? null : new DestinoDto("usuario", usuario);
    }

    private async Task<string?> CodigoAsync(TipoCatalogo tipo, int? id, CancellationToken cancelacion)
    {
        if (id is not int valor)
        {
            return null;
        }

        if (!_codigos.TryGetValue(tipo, out var codigos))
        {
            codigos = (await catalogos.ListarAsync(tipo, cancelacion)).ToDictionary(x => x.Id, x => x.Codigo);
            _codigos[tipo] = codigos;
        }

        return codigos.GetValueOrDefault(valor);
    }
}
