using System.Globalization;
using System.Text;
using SistemaTickets.Application.Auditoria;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Soluciones;

/// Imágenes nuevas y claves de las que ya tenía la solución y se conservan.
public record ImagenesSolucion(IReadOnlyList<ImagenNueva> Nuevas, IReadOnlyCollection<string> Conservadas)
{
    public static readonly ImagenesSolucion Ninguna = new([], []);
}

public record DatosSolucion(
    string? Titulo,
    string? Descripcion,
    string? Sistema,
    string? Subsistema,
    IReadOnlyList<string>? PalabrasClave,
    ImagenesSolucion? Imagenes);

public record DatosPropuesta(
    int TicketId,
    string? Titulo,
    string? Descripcion,
    IReadOnlyList<string>? PalabrasClave,
    ImagenesSolucion? Imagenes);

public record ListadoSoluciones(IReadOnlyList<Solucion> Soluciones, IReadOnlyDictionary<int, int> Usos);

public class ServicioSoluciones(
    IRepositorioSoluciones soluciones,
    IRepositorioTickets tickets,
    IRepositorioCatalogos catalogos,
    IAuditoria auditoria,
    TimeProvider reloj)
{
    private const int LargoMaximoTitulo = 200;
    private const int LargoMaximoCodigo = 60;

    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    public async Task<ListadoSoluciones> ListarAsync(Usuario usuario, EstadoSolucion? estado, CancellationToken cancelacion = default)
    {
        ExigirOrmen(usuario);
        return new ListadoSoluciones(
            await soluciones.ListarCompletasAsync(estado, cancelacion),
            await soluciones.ContarUsosAsync(cancelacion));
    }

    public async Task<Solucion?> ObtenerAsync(Usuario usuario, string codigo, CancellationToken cancelacion = default)
    {
        ExigirOrmen(usuario);
        return await soluciones.BuscarAsync(codigo, cancelacion);
    }

    public Task<IReadOnlyDictionary<int, int>> ContarUsosAsync(CancellationToken cancelacion = default) =>
        soluciones.ContarUsosAsync(cancelacion);

    /// Un administrador la carga aprobada; un operador la deja para revisar.
    public async Task<Solucion> CrearAsync(Usuario usuario, DatosSolucion datos, CancellationToken cancelacion = default)
    {
        ExigirOrmen(usuario);
        var campos = await ValidarAsync(datos, cancelacion);
        var ahora = Ahora;
        var admin = usuario.Rol == Rol.Administrador;
        var solucion = new Solucion
        {
            Codigo = await CodigoLibreAsync(campos.Titulo, cancelacion),
            Titulo = campos.Titulo,
            Texto = campos.Descripcion,
            SistemaId = campos.SistemaId,
            SubsistemaId = campos.SubsistemaId,
            PalabrasClave = campos.PalabrasClave,
            Estado = admin ? EstadoSolucion.Aprobada : EstadoSolucion.Pendiente,
            PropuestaPorId = usuario.Id,
            RevisadaPorId = admin ? usuario.Id : null,
            RevisadaEn = admin ? ahora : null,
            CreadaEn = ahora,
            ActualizadaEn = ahora,
        };
        solucion.Adjuntos.AddRange(Adjuntos((datos.Imagenes ?? ImagenesSolucion.Ninguna).Nuevas, usuario.Id, ahora));

        soluciones.Agregar(solucion);
        Auditar(usuario, admin ? OperacionesAuditoria.SolucionCreada : OperacionesAuditoria.SolucionPropuesta, solucion, solucion.Titulo);
        await soluciones.GuardarAsync(cancelacion);
        return solucion;
    }

    public async Task<Solucion?> EditarAsync(Usuario usuario, string codigo, DatosSolucion datos, CancellationToken cancelacion = default)
    {
        ExigirAdministrador(usuario, "Sólo un administrador puede modificar soluciones reutilizables.");
        var solucion = await soluciones.BuscarAsync(codigo, cancelacion);
        if (solucion is null)
        {
            return null;
        }

        var campos = await ValidarAsync(datos, cancelacion);
        var ahora = Ahora;
        Aplicar(solucion, campos);
        if (datos.Imagenes is { } imagenes)
        {
            ReemplazarAdjuntos(solucion, imagenes, usuario.Id, ahora);
        }

        solucion.ActualizadaEn = ahora;
        Auditar(usuario, OperacionesAuditoria.SolucionEditada, solucion, solucion.Titulo);
        await soluciones.GuardarAsync(cancelacion);
        return solucion;
    }

    /// La resolución de un ticket cerrado da origen a una solución por aprobar.
    public async Task<Solucion?> ProponerAsync(Usuario usuario, DatosPropuesta datos, CancellationToken cancelacion = default)
    {
        ExigirOrmen(usuario);
        var ticket = await tickets.BuscarAsync(datos.TicketId, cancelacion);
        if (ticket is null || ticket.Estado == EstadoTicket.Borrador)
        {
            return null;
        }

        Exigir(ticket.Estado == EstadoTicket.Cerrado && ticket.Resolucion is not null,
            "Sólo la resolución de un ticket cerrado puede dar origen a una solución reutilizable.");
        Exigir(!await soluciones.TienePropuestaVigenteAsync(ticket.Id, cancelacion),
            "La resolución de este ticket ya dio origen a una solución reutilizable.");

        var titulo = Limpiar(datos.Titulo);
        var descripcion = Limpiar(datos.Descripcion);
        var palabras = NormalizarPalabras(datos.PalabrasClave);
        Exigir(titulo.Length > 0, "Escribí un título para la solución reutilizable.", "propuestaTitulo");
        Exigir(titulo.Length <= LargoMaximoTitulo, $"El título puede tener hasta {LargoMaximoTitulo} caracteres.", "propuestaTitulo");
        Exigir(palabras.Count > 0, "Agregá al menos una palabra clave.", "propuestaPalabras");
        Exigir(descripcion.Length > 0, "Escribí la solución (los pasos a seguir).", "propuestaDescripcion");

        var ahora = Ahora;
        var solucion = new Solucion
        {
            Codigo = await CodigoLibreAsync(titulo, cancelacion),
            Titulo = titulo,
            Texto = descripcion,
            SistemaId = ticket.SistemaId,
            SubsistemaId = ticket.SubsistemaId,
            PalabrasClave = palabras,
            Estado = EstadoSolucion.Pendiente,
            TicketOrigenId = ticket.Id,
            PropuestaPorId = usuario.Id,
            CreadaEn = ahora,
            ActualizadaEn = ahora,
        };
        solucion.Adjuntos.AddRange(datos.Imagenes is { } imagenes
            ? Adjuntos(imagenes.Nuevas, usuario.Id, ahora)
            : CopiaDeResolucion(ticket));

        soluciones.Agregar(solucion);
        Auditar(usuario, OperacionesAuditoria.SolucionPropuesta, solucion, solucion.Titulo);
        await soluciones.GuardarAsync(cancelacion);
        return solucion;
    }

    public async Task<Solucion?> RevisarAsync(Usuario usuario, string codigo, bool aprobar, string? motivo, DatosSolucion? cambios, CancellationToken cancelacion = default)
    {
        ExigirAdministrador(usuario, "Sólo un administrador puede aprobar o rechazar soluciones reutilizables.");
        var solucion = await soluciones.BuscarAsync(codigo, cancelacion);
        if (solucion is null)
        {
            return null;
        }

        Exigir(solucion.Estado == EstadoSolucion.Pendiente, "La solución ya fue revisada.");
        var limpio = Limpiar(motivo);
        Exigir(aprobar || limpio.Length > 0, "Escribí el motivo del rechazo.", "motivo");
        if (aprobar && cambios is not null)
        {
            Aplicar(solucion, await ValidarAsync(cambios, cancelacion));
        }

        var ahora = Ahora;
        solucion.Estado = aprobar ? EstadoSolucion.Aprobada : EstadoSolucion.Rechazada;
        solucion.RevisadaPorId = usuario.Id;
        solucion.RevisadaEn = ahora;
        solucion.ActualizadaEn = ahora;
        solucion.MotivoRechazo = aprobar ? null : limpio;
        Auditar(usuario, aprobar ? OperacionesAuditoria.SolucionAprobada : OperacionesAuditoria.SolucionRechazada, solucion,
            aprobar ? solucion.Titulo : $"{solucion.Titulo} · {TextoAuditoria.Truncar(limpio, 70)}");
        await soluciones.GuardarAsync(cancelacion);
        return solucion;
    }

    public static List<string> NormalizarPalabras(IEnumerable<string>? lista)
    {
        var vistas = new HashSet<string>();
        var resultado = new List<string>();
        foreach (var palabra in lista ?? [])
        {
            var texto = string.Join(' ', (palabra ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            var clave = Normalizar(texto);
            if (clave.Length == 0 || !vistas.Add(clave))
            {
                continue;
            }

            resultado.Add(texto.ToLowerInvariant());
        }

        return resultado;
    }

    private sealed record CamposSolucion(string Titulo, string Descripcion, int? SistemaId, int? SubsistemaId, List<string> PalabrasClave);

    private async Task<CamposSolucion> ValidarAsync(DatosSolucion datos, CancellationToken cancelacion)
    {
        var titulo = Limpiar(datos.Titulo);
        var descripcion = Limpiar(datos.Descripcion);
        var palabras = NormalizarPalabras(datos.PalabrasClave);
        Exigir(titulo.Length > 0, "Escribí un título.", "titulo");
        Exigir(titulo.Length <= LargoMaximoTitulo, $"El título puede tener hasta {LargoMaximoTitulo} caracteres.", "titulo");
        Exigir(descripcion.Length > 0, "Escribí la solución (los pasos a seguir).", "descripcion");

        ItemCatalogo? sistema = null;
        if (Vacio(datos.Sistema) is string codigoSistema)
        {
            sistema = (await catalogos.ListarAsync(TipoCatalogo.Sistemas, cancelacion)).FirstOrDefault(x => x.Codigo == codigoSistema);
            Exigir(sistema is not null, "El sistema no existe.", "sistemaId");
        }

        Subsistema? subsistema = null;
        if (Vacio(datos.Subsistema) is string codigoSubsistema)
        {
            subsistema = (await catalogos.ListarAsync(TipoCatalogo.Subsistemas, cancelacion))
                .OfType<Subsistema>().FirstOrDefault(x => x.Codigo == codigoSubsistema);
            Exigir(subsistema is not null && subsistema.SistemaId == sistema?.Id,
                "El subsistema no corresponde al sistema elegido.", "subsistemaId");
        }

        Exigir(palabras.Count > 0, "Agregá al menos una palabra clave.", "palabrasClave");
        return new CamposSolucion(titulo, descripcion, sistema?.Id, subsistema?.Id, palabras);
    }

    private static void Aplicar(Solucion solucion, CamposSolucion campos)
    {
        solucion.Titulo = campos.Titulo;
        solucion.Texto = campos.Descripcion;
        solucion.SistemaId = campos.SistemaId;
        solucion.SubsistemaId = campos.SubsistemaId;
        solucion.PalabrasClave = campos.PalabrasClave;
    }

    private static void ReemplazarAdjuntos(Solucion solucion, ImagenesSolucion imagenes, int autorId, DateTime fecha)
    {
        var nuevas = imagenes.Nuevas.Where(i => solucion.Adjuntos.All(a => a.Clave != i.Clave)).ToList();
        var quedan = imagenes.Conservadas.Concat(imagenes.Nuevas.Select(i => i.Clave)).ToHashSet();
        solucion.Adjuntos.RemoveAll(a => !quedan.Contains(a.Clave));
        solucion.Adjuntos.AddRange(Adjuntos(nuevas, autorId, fecha));
    }

    private static IEnumerable<Adjunto> CopiaDeResolucion(Ticket ticket)
    {
        var origen = ticket.ResolucionEnDescripcion ? OrigenAdjunto.Descripcion : OrigenAdjunto.Resolucion;
        return ticket.Adjuntos.Where(a => a.Origen == origen).Select(a => new Adjunto
        {
            Origen = OrigenAdjunto.Solucion,
            Clave = a.Clave,
            Nombre = a.Nombre,
            TipoContenido = a.TipoContenido,
            Contenido = a.Contenido,
            SubidoPorId = a.SubidoPorId,
            Fecha = a.Fecha,
        }).ToList();
    }

    private static IEnumerable<Adjunto> Adjuntos(IEnumerable<ImagenNueva> imagenes, int autorId, DateTime fecha) =>
        imagenes.Select(i => new Adjunto
        {
            Origen = OrigenAdjunto.Solucion,
            Clave = i.Clave,
            Nombre = i.Nombre,
            TipoContenido = i.TipoContenido,
            Contenido = i.Contenido,
            SubidoPorId = autorId,
            Fecha = fecha,
        }).ToList();

    private async Task<string> CodigoLibreAsync(string titulo, CancellationToken cancelacion)
    {
        var slug = Normalizar(titulo).Replace(' ', '-');
        var codigoBase = slug.Length > 0 ? $"sol-{slug}" : "sol";
        if (codigoBase.Length > LargoMaximoCodigo - 4)
        {
            codigoBase = codigoBase[..(LargoMaximoCodigo - 4)].TrimEnd('-');
        }

        var codigo = codigoBase;
        for (var n = 2; await soluciones.ExisteCodigoAsync(codigo, cancelacion); n++)
        {
            codigo = $"{codigoBase}-{n}";
        }

        return codigo;
    }

    private void Auditar(Usuario usuario, string operacion, Solucion solucion, string detalle)
    {
        if (solucion.TicketOrigenId is int ticketId)
        {
            auditoria.Registrar(usuario.Id, operacion, EntidadesAuditoria.Ticket, ticketId.ToString(), detalle);
        }
        else
        {
            auditoria.Registrar(usuario.Id, operacion, EntidadesAuditoria.Solucion, solucion.Codigo, detalle);
        }
    }

    private static void ExigirOrmen(Usuario usuario)
    {
        if (!usuario.EsOrmen)
        {
            throw new SinPermisoException("Las soluciones reutilizables son para Mesa de ayuda y gestión de ORMEN.");
        }
    }

    private static void ExigirAdministrador(Usuario usuario, string mensaje)
    {
        if (usuario.Rol != Rol.Administrador)
        {
            throw new SinPermisoException(mensaje);
        }
    }

    private static string Normalizar(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(char.IsAsciiLetterOrDigit(c) ? c : ' ');
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Limpiar(string? texto) => (texto ?? string.Empty).Trim();

    private static string? Vacio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static void Exigir(bool condicion, string mensaje, string? campo = null)
    {
        if (!condicion)
        {
            throw new ErrorTicket(mensaje, campo);
        }
    }
}
