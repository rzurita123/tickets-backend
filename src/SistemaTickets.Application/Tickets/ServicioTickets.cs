using System.Text.Json;
using SistemaTickets.Application.Auditoria;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Localidades;
using SistemaTickets.Application.Notificaciones;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Tickets;

public class ErrorTicket(string mensaje, string? campo = null) : Exception(mensaje)
{
    public string? Campo { get; } = campo;
}

public class SinPermisoException(string mensaje) : Exception(mensaje);

public record ImagenNueva(string Clave, string Nombre, string TipoContenido, byte[] Contenido);

public record DatosTicket(
    string? Titulo,
    string? Descripcion,
    int? LocalidadId,
    string? Sistema,
    string? Subsistema,
    string? Criticidad,
    string? TipoProblema,
    IReadOnlyList<ImagenNueva> Imagenes);

public record OpcionesAlta(
    int? BorradorId,
    bool RegistroDirecto,
    bool Privado,
    string? SolucionTexto,
    string? SolucionCatalogoId,
    IReadOnlyList<ImagenNueva> SolucionImagenes);

public record DatosCierre(
    string? Texto,
    string? MensajePublico,
    string? TipoProblema,
    string? SolucionCatalogoId,
    IReadOnlyList<ImagenNueva> Imagenes);

public record CambioClasificacion(string Campo, string Etiqueta, string De, string A);

public class ServicioTickets(
    IRepositorioTickets tickets,
    IRepositorioUsuarios usuarios,
    IRepositorioLocalidades localidades,
    IRepositorioCatalogos catalogos,
    IRepositorioSoluciones soluciones,
    IAuditoria auditoria,
    INotificaciones notificaciones,
    TimeProvider reloj)
{
    public const string TipoProblemaNoDefinido = "tp-nodef";
    private const int LargoMaximoTitulo = 120;

    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    public Task<IReadOnlyList<Ticket>> ListarAsync(Usuario usuario, bool incluirBorradores, CancellationToken cancelacion = default) =>
        tickets.ListarParaAsync(usuario, incluirBorradores, cancelacion);

    public async Task<Ticket> CrearAsync(Usuario usuario, DatosTicket datos, OpcionesAlta opciones, CancellationToken cancelacion = default)
    {
        if (usuario.Rol == Rol.Cliente && (opciones.RegistroDirecto || opciones.Privado))
        {
            throw new SinPermisoException("Sólo ORMEN puede registrar tickets resueltos o privados.");
        }

        var titulo = Limpiar(datos.Titulo);
        var descripcion = Limpiar(datos.Descripcion);
        Exigir(titulo.Length > 0, "Escribí un título.", "titulo");
        Exigir(titulo.Length <= LargoMaximoTitulo, $"El título puede tener hasta {LargoMaximoTitulo} caracteres.", "titulo");
        Exigir(descripcion.Length > 0, "Describí el problema.", "descripcion");

        var localidadId = usuario.Rol == Rol.Cliente ? usuario.LocalidadId : datos.LocalidadId;
        var localidad = (await localidades.ListarAsync(cancelacion)).FirstOrDefault(l => l.Id == localidadId);
        Exigir(localidad is not null, "Elegí la localidad.", "localidadId");
        Exigir(localidad!.Activa, "Esa localidad está inactiva.", "localidadId");

        var sistema = await ItemAsync(TipoCatalogo.Sistemas, datos.Sistema, cancelacion);
        Exigir(sistema is { Activo: true }, "Elegí el sistema o servicio afectado.", "sistemaId");
        var subsistema = await SubsistemaAsync(datos.Subsistema, sistema!, soloActivo: true, cancelacion);
        var criticidad = await ItemAsync(TipoCatalogo.Criticidades, datos.Criticidad, cancelacion);
        Exigir(criticidad is { Activo: true }, "Elegí la criticidad.", "criticidadId");
        var codigoTipo = usuario.EsOrmen ? Vacio(datos.TipoProblema) : null;
        var tipoProblema = await ItemAsync(
            TipoCatalogo.TiposProblema,
            codigoTipo ?? TipoProblemaNoDefinido,
            cancelacion);
        Exigir(codigoTipo is null || tipoProblema is { Activo: true }, "Elegí el tipo de problema.", "tipoProblemaId");

        var ahora = Ahora;
        Ticket ticket;
        if (opciones.BorradorId is int borradorId)
        {
            ticket = await BorradorAsync(usuario, borradorId, cancelacion) ?? throw new ErrorTicket("El borrador ya no existe.");
        }
        else
        {
            ticket = Ticket.NuevoBorrador(usuario.Id, ahora);
            tickets.Agregar(ticket);
        }

        ticket.Titulo = titulo;
        ticket.Descripcion = descripcion;
        ticket.LocalidadId = localidad.Id;
        ticket.SistemaId = sistema!.Id;
        ticket.SubsistemaId = subsistema?.Id;
        ticket.CriticidadId = criticidad!.Id;
        ticket.TipoProblemaId = tipoProblema?.Id;
        ticket.ReemplazarAdjuntosDescripcion(Adjuntos(datos.Imagenes, OrigenAdjunto.Descripcion, usuario.Id, ahora));

        var solucionAplicada = opciones.RegistroDirecto ? await SolucionAplicadaAsync(opciones.SolucionCatalogoId, cancelacion) : null;
        var numero = await tickets.SiguienteNumeroAsync(cancelacion);
        var desdeBorrador = opciones.BorradorId is not null;
        Reglas(() =>
        {
            if (opciones.RegistroDirecto)
            {
                ticket.RegistrarResuelto(
                    numero, opciones.Privado, desdeBorrador, usuario.Id, ahora,
                    Limpiar(opciones.SolucionTexto), solucionAplicada,
                    Adjuntos(opciones.SolucionImagenes, OrigenAdjunto.Resolucion, usuario.Id, ahora));
            }
            else
            {
                ticket.Registrar(numero, opciones.Privado, desdeBorrador, usuario.Id, ahora);
            }
        });

        await tickets.GuardarAsync(cancelacion);
        var resumen = $"#{ticket.Numero} · {ticket.Titulo}";
        Auditar(usuario, opciones.RegistroDirecto ? OperacionesAuditoria.TicketCerradoDirecto : OperacionesAuditoria.TicketCreado, ticket,
            opciones.RegistroDirecto || !desdeBorrador ? resumen : resumen + " (desde un borrador)");
        await auditoria.GuardarAsync(cancelacion);
        return ticket;
    }

    public async Task<Ticket> GuardarBorradorAsync(Usuario usuario, int? id, DatosTicket datos, CancellationToken cancelacion = default)
    {
        var titulo = Limpiar(datos.Titulo);
        var descripcion = Limpiar(datos.Descripcion);
        var localidadId = usuario.Rol == Rol.Cliente ? usuario.LocalidadId : datos.LocalidadId;
        var localidadElegida = usuario.EsOrmen && datos.LocalidadId is not null;
        var tipoElegido = usuario.EsOrmen && Vacio(datos.TipoProblema) is string tipo && tipo != TipoProblemaNoDefinido;
        var tieneAlgo = titulo.Length > 0 || descripcion.Length > 0 || localidadElegida || tipoElegido
            || Vacio(datos.Sistema) is not null || Vacio(datos.Criticidad) is not null || datos.Imagenes.Count > 0;
        Exigir(tieneAlgo, "Para guardar un borrador escribí algo.");
        Exigir(titulo.Length <= LargoMaximoTitulo, $"El título puede tener hasta {LargoMaximoTitulo} caracteres.", "titulo");

        var localidad = localidadId is null ? null
            : (await localidades.ListarAsync(cancelacion)).FirstOrDefault(l => l.Id == localidadId);
        Exigir(!localidadElegida || localidad is not null, "Elegí la localidad.", "localidadId");
        var sistema = await ItemAsync(TipoCatalogo.Sistemas, datos.Sistema, cancelacion);
        Exigir(Vacio(datos.Sistema) is null || sistema is not null, "Elegí el sistema o servicio afectado.", "sistemaId");
        var subsistema = (await ItemAsync(TipoCatalogo.Subsistemas, datos.Subsistema, cancelacion)) as Subsistema;
        Exigir(Vacio(datos.Subsistema) is null || (subsistema is not null && subsistema.SistemaId == sistema?.Id),
            "Ese subsistema no corresponde al sistema elegido.", "subsistemaId");
        var criticidad = await ItemAsync(TipoCatalogo.Criticidades, datos.Criticidad, cancelacion);
        Exigir(Vacio(datos.Criticidad) is null || criticidad is not null, "Elegí la criticidad.", "criticidadId");
        var tipoProblema = await ItemAsync(TipoCatalogo.TiposProblema,
            (usuario.EsOrmen ? Vacio(datos.TipoProblema) : null) ?? TipoProblemaNoDefinido, cancelacion);
        Exigir(!tipoElegido || tipoProblema is not null, "Elegí el tipo de problema.", "tipoProblemaId");

        var ahora = Ahora;
        Ticket ticket;
        if (id is int borradorId)
        {
            ticket = await BorradorAsync(usuario, borradorId, cancelacion) ?? throw new ErrorTicket("El borrador ya no existe.");
        }
        else
        {
            ticket = Ticket.NuevoBorrador(usuario.Id, ahora);
            tickets.Agregar(ticket);
        }

        ticket.Titulo = titulo;
        ticket.Descripcion = descripcion;
        ticket.LocalidadId = localidad?.Id;
        ticket.SistemaId = sistema?.Id;
        ticket.SubsistemaId = subsistema?.Id;
        ticket.CriticidadId = criticidad?.Id;
        ticket.TipoProblemaId = tipoProblema?.Id;
        ticket.ReemplazarAdjuntosDescripcion(Adjuntos(datos.Imagenes, OrigenAdjunto.Descripcion, usuario.Id, ahora));
        ticket.Tocar(ahora);

        await tickets.GuardarAsync(cancelacion);
        auditoria.Registrar(usuario.Id, OperacionesAuditoria.BorradorGuardado, EntidadesAuditoria.Borrador, ticket.Id.ToString(), NombreBorrador(ticket));
        await auditoria.GuardarAsync(cancelacion);
        return ticket;
    }

    public async Task<bool> DescartarBorradorAsync(Usuario usuario, int id, CancellationToken cancelacion = default)
    {
        var ticket = await BorradorAsync(usuario, id, cancelacion);
        if (ticket is null)
        {
            return false;
        }

        auditoria.Registrar(usuario.Id, OperacionesAuditoria.BorradorDescartado, EntidadesAuditoria.Borrador, ticket.Id.ToString(), NombreBorrador(ticket));
        tickets.Eliminar(ticket);
        await tickets.GuardarAsync(cancelacion);
        return true;
    }

    public async Task<Ticket?> ComentarAsync(Usuario usuario, int id, string? texto, bool privado, IReadOnlyList<ImagenNueva> imagenes, CancellationToken cancelacion = default)
    {
        var ticket = await VisibleAsync(usuario, id, cancelacion);
        if (ticket is null)
        {
            return null;
        }

        var limpio = Limpiar(texto);
        Exigir(limpio.Length > 0, "Escribí el comentario.", "texto");
        var ahora = Ahora;
        var esPrivado = usuario.EsOrmen && privado;
        var comentario = ticket.Comentar(usuario.Id, ahora, limpio, esPrivado);
        ticket.AgregarAdjuntos(Adjuntos(imagenes, OrigenAdjunto.Comentario, usuario.Id, ahora, comentario));
        Auditar(usuario, esPrivado ? OperacionesAuditoria.ComentarioPrivado : OperacionesAuditoria.ComentarioPublico, ticket, TextoAuditoria.Truncar(limpio, 90));
        if (usuario.EsOrmen && !esPrivado)
        {
            await AvisarAsync(ticket, usuario, MotivoAviso.Comentario, Mensaje(limpio), cancelacion);
        }

        await tickets.GuardarAsync(cancelacion);
        return ticket;
    }

    public Task<Ticket?> AsignarAsync(Usuario usuario, int id, string? operador, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var persona = await PersonaOrmenAsync(operador, cancelacion);
            var de = ticket.Estado;
            ticket.Asignar(persona.Id, usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Asignacion, ticket,
                $"Atiende: {persona.Nombre}" + (de != ticket.Estado ? $" · {Cambio(de, ticket.Estado)}" : string.Empty));
            if (de != ticket.Estado)
            {
                await AvisarAsync(ticket, usuario, MotivoAviso.EnProceso, null, cancelacion);
            }
        }, cancelacion);

    public Task<Ticket?> LiberarAsync(Usuario usuario, int id, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, ticket =>
        {
            var de = ticket.Estado;
            ticket.Liberar(usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Liberacion, ticket, $"{Cambio(de, ticket.Estado)} · sin asignar");
            return Task.CompletedTask;
        }, cancelacion);

    public Task<Ticket?> ElevarAsync(Usuario usuario, int id, string? grupo, string? persona, string? motivo, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var limpio = Limpiar(motivo);
            Exigir(limpio.Length > 0, "Escribí por qué se eleva.", "motivo");
            Exigir((Vacio(grupo) is null) != (Vacio(persona) is null), "Elegí un único destinatario.", "destino");
            int? grupoId = null;
            int? usuarioId = null;
            string destino;
            if (Vacio(grupo) is not null)
            {
                var item = await ItemAsync(TipoCatalogo.GruposSoporte, grupo, cancelacion);
                Exigir(item is not null, "Elegí a quién se eleva.");
                Exigir(item!.Activo, "Ese grupo está inactivo.");
                grupoId = item.Id;
                destino = item.Nombre;
            }
            else
            {
                var elegido = await PersonaOrmenAsync(persona, cancelacion);
                usuarioId = elegido.Id;
                destino = elegido.Nombre;
            }

            ticket.Elevar(grupoId, usuarioId, limpio, usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Elevacion, ticket, $"Elevado a {destino} · {TextoAuditoria.Truncar(limpio, 70)}");
            await AvisarAsync(ticket, usuario, MotivoAviso.Elevado,
                $"Lo derivamos a {destino} para revisarlo. Te avisamos por acá apenas tengamos novedades.", cancelacion);
        }, cancelacion);

    public Task<Ticket?> BloquearAsync(Usuario usuario, int id, string? texto, bool privado, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var limpio = Limpiar(texto);
            Exigir(limpio.Length > 0, "Escribí qué factor externo lo detiene.", "texto");
            var de = ticket.Estado;
            ticket.Bloquear(limpio, privado, usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Bloqueo, ticket, $"{Cambio(de, ticket.Estado)} · {TextoAuditoria.Truncar(limpio, 70)}");
            await AvisarAsync(ticket, usuario, MotivoAviso.Bloqueado, privado ? null : Mensaje(limpio), cancelacion);
        }, cancelacion);

    public Task<Ticket?> DesbloquearAsync(Usuario usuario, int id, string? texto, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var de = ticket.Estado;
            var limpio = Vacio(texto);
            ticket.Desbloquear(limpio, usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Desbloqueo, ticket,
                Cambio(de, ticket.Estado) + (limpio is null ? string.Empty : $" · {TextoAuditoria.Truncar(limpio, 70)}"));
            await AvisarAsync(ticket, usuario, MotivoAviso.Estado, null, cancelacion);
        }, cancelacion);

    public Task<Ticket?> ReclasificarAsync(Usuario usuario, int id, string? sistema, string? subsistema, string? criticidad, string? tipoProblema, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var nuevoSistema = await ItemAsync(TipoCatalogo.Sistemas, sistema, cancelacion);
            Exigir(nuevoSistema is not null, "Elegí el sistema o servicio afectado.", "sistemaId");
            var nuevoSubsistema = await SubsistemaAsync(subsistema, nuevoSistema!, soloActivo: false, cancelacion);
            var nuevaCriticidad = await ItemAsync(TipoCatalogo.Criticidades, criticidad, cancelacion);
            Exigir(nuevaCriticidad is not null, "Elegí la criticidad.", "criticidadId");
            var nuevoTipo = await ItemAsync(TipoCatalogo.TiposProblema, tipoProblema, cancelacion);
            Exigir(nuevoTipo is not null, "Elegí el tipo de problema.", "tipoProblemaId");

            var cambios = new List<CambioClasificacion>();
            await CompararAsync(cambios, "sistemaId", "Sistema", TipoCatalogo.Sistemas, ticket.SistemaId, nuevoSistema, cancelacion);
            await CompararAsync(cambios, "subsistemaId", "Subsistema", TipoCatalogo.Subsistemas, ticket.SubsistemaId, nuevoSubsistema, cancelacion);
            await CompararAsync(cambios, "criticidadId", "Criticidad", TipoCatalogo.Criticidades, ticket.CriticidadId, nuevaCriticidad, cancelacion);
            await CompararAsync(cambios, "tipoProblemaId", "Tipo de problema", TipoCatalogo.TiposProblema, ticket.TipoProblemaId, nuevoTipo, cancelacion);
            Exigir(cambios.Count > 0, "No hay cambios para guardar.");

            ticket.Reclasificar(
                nuevoSistema!.Id, nuevoSubsistema?.Id, nuevaCriticidad!.Id, nuevoTipo!.Id,
                JsonSerializer.Serialize(cambios, JsonSerializerOptions.Web), usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Datos, ticket, string.Join(" · ", cambios.Select(c => $"{c.Etiqueta}: {c.De} → {c.A}")));
        }, cancelacion);

    public Task<Ticket?> CerrarAsync(Usuario usuario, int id, DatosCierre datos, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var texto = Limpiar(datos.Texto);
            Exigir(texto.Length > 0, "Escribí la resolución.", "solucionTexto");
            ItemCatalogo? tipo = null;
            if (!string.IsNullOrEmpty(datos.TipoProblema))
            {
                tipo = await ItemAsync(TipoCatalogo.TiposProblema, datos.TipoProblema, cancelacion);
                Exigir(tipo is not null, "Elegí el tipo de problema.", "tipoProblemaId");
            }

            var solucionAplicada = await SolucionAplicadaAsync(datos.SolucionCatalogoId, cancelacion);
            var ahora = Ahora;
            var mensaje = Limpiar(datos.MensajePublico);
            var conMensaje = mensaje.Length > 0 && ticket.Estado != EstadoTicket.Cerrado;
            if (conMensaje)
            {
                ticket.Comentar(usuario.Id, ahora, mensaje, privada: false);
            }

            var de = ticket.Estado;
            ticket.Cerrar(
                texto, solucionAplicada, tipo?.Id,
                Adjuntos(datos.Imagenes, OrigenAdjunto.Resolucion, usuario.Id, ahora), usuario.Id, ahora);
            if (conMensaje)
            {
                Auditar(usuario, OperacionesAuditoria.ComentarioPublico, ticket, TextoAuditoria.Truncar(mensaje, 90));
            }

            Auditar(usuario, OperacionesAuditoria.Cierre, ticket, Cambio(de, ticket.Estado));
            await AvisarAsync(ticket, usuario, MotivoAviso.Cierre, conMensaje ? Mensaje(mensaje) : null, cancelacion);
        }, cancelacion);

    public Task<Ticket?> ReabrirAsync(Usuario usuario, int id, string? motivo, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var de = ticket.Estado;
            var limpio = Vacio(motivo);
            ticket.Reabrir(limpio, usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Reapertura, ticket,
                Cambio(de, ticket.Estado) + (limpio is null ? string.Empty : $" · {TextoAuditoria.Truncar(limpio, 70)}"));
            await AvisarAsync(ticket, usuario, MotivoAviso.Reapertura, null, cancelacion);
        }, cancelacion);

    public Task<Ticket?> CambiarPrivadoAsync(Usuario usuario, int id, bool privado, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var creador = await usuarios.BuscarPorIdAsync(ticket.CreadoPorId, cancelacion);
            Exigir(creador?.Rol != Rol.Cliente, "Este ticket lo creó la localidad: no se puede hacer privado.");
            ticket.CambiarPrivado(privado, usuario.Id, Ahora);
            Auditar(usuario, OperacionesAuditoria.Privacidad, ticket, privado ? "Marcado como privado" : "Visible para la localidad");
        }, cancelacion);

    public static bool PuedeVer(Ticket ticket, Usuario usuario) => VisibilidadTickets.PuedeVer(ticket, usuario);

    private async Task<Ticket?> TratarAsync(Usuario usuario, int id, Func<Ticket, Task> accion, CancellationToken cancelacion)
    {
        if (!usuario.EsOrmen)
        {
            throw new SinPermisoException("Sólo ORMEN puede tratar tickets.");
        }

        var ticket = await tickets.BuscarAsync(id, cancelacion);
        if (ticket is null || ticket.Estado == EstadoTicket.Borrador)
        {
            return null;
        }

        try
        {
            await accion(ticket);
        }
        catch (ReglaTicketException e)
        {
            throw new ErrorTicket(e.Message);
        }

        await tickets.GuardarAsync(cancelacion);
        return ticket;
    }

    private async Task<Ticket?> VisibleAsync(Usuario usuario, int id, CancellationToken cancelacion)
    {
        var ticket = await tickets.BuscarAsync(id, cancelacion);
        return ticket is null || ticket.Estado == EstadoTicket.Borrador || !PuedeVer(ticket, usuario) ? null : ticket;
    }

    private async Task<Ticket?> BorradorAsync(Usuario usuario, int id, CancellationToken cancelacion)
    {
        var ticket = await tickets.BuscarAsync(id, cancelacion);
        return ticket is { Estado: EstadoTicket.Borrador } && ticket.CreadoPorId == usuario.Id ? ticket : null;
    }

    private async Task<Usuario> PersonaOrmenAsync(string? nombreUsuario, CancellationToken cancelacion)
    {
        var persona = string.IsNullOrEmpty(nombreUsuario) ? null : await usuarios.BuscarPorNombreUsuarioAsync(nombreUsuario, cancelacion);
        Exigir(persona is { EsOrmen: true, Activo: true }, "Elegí una persona activa de ORMEN.");
        return persona!;
    }

    private async Task<int?> SolucionAplicadaAsync(string? codigo, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return null;
        }

        var solucion = (await soluciones.ListarAsync(cancelacion)).FirstOrDefault(s => s.Codigo == codigo.Trim());
        // Las soluciones creadas en el frontend todavía no están en la Api: si no existe, se cierra sin vincularla.
        Exigir(solucion is null || solucion.Estado == EstadoSolucion.Aprobada, "La solución reutilizable elegida ya no está aprobada.");
        return solucion?.Id;
    }

    private async Task<ItemCatalogo?> ItemAsync(TipoCatalogo tipo, string? codigo, CancellationToken cancelacion) =>
        Vacio(codigo) is string limpio
            ? (await catalogos.ListarAsync(tipo, cancelacion)).FirstOrDefault(x => x.Codigo == limpio)
            : null;

    private async Task<Subsistema?> SubsistemaAsync(string? codigo, ItemCatalogo sistema, bool soloActivo, CancellationToken cancelacion)
    {
        if (Vacio(codigo) is null)
        {
            return null;
        }

        var subsistema = await ItemAsync(TipoCatalogo.Subsistemas, codigo, cancelacion) as Subsistema;
        Exigir(
            subsistema is not null && subsistema.SistemaId == sistema.Id && (!soloActivo || subsistema.Activo),
            "Ese subsistema no corresponde al sistema elegido.", "subsistemaId");
        return subsistema;
    }

    private async Task CompararAsync(List<CambioClasificacion> cambios, string campo, string etiqueta, TipoCatalogo tipo, int? actualId, ItemCatalogo? nuevo, CancellationToken cancelacion)
    {
        if (actualId == nuevo?.Id)
        {
            return;
        }

        var actual = (await catalogos.ListarAsync(tipo, cancelacion)).FirstOrDefault(x => x.Id == actualId);
        cambios.Add(new CambioClasificacion(campo, etiqueta, actual?.Nombre ?? "—", nuevo?.Nombre ?? "—"));
    }

    private static IEnumerable<Adjunto> Adjuntos(IReadOnlyList<ImagenNueva> imagenes, OrigenAdjunto origen, int autorId, DateTime fecha, Actividad? actividad = null) =>
        imagenes.Select(i => new Adjunto
        {
            Origen = origen,
            Actividad = actividad,
            Clave = i.Clave,
            Nombre = i.Nombre,
            TipoContenido = i.TipoContenido,
            Contenido = i.Contenido,
            SubidoPorId = autorId,
            Fecha = fecha,
        }).ToList();

    private static void Reglas(Action accion)
    {
        try
        {
            accion();
        }
        catch (ReglaTicketException e)
        {
            throw new ErrorTicket(e.Message);
        }
    }

    private void Auditar(Usuario usuario, string operacion, Ticket ticket, string detalle) =>
        auditoria.Registrar(usuario.Id, operacion, EntidadesAuditoria.Ticket, ticket.Id.ToString(), detalle);

    private async Task AvisarAsync(Ticket ticket, Usuario actor, MotivoAviso motivo, string? detalle, CancellationToken cancelacion)
    {
        var creador = await usuarios.BuscarPorIdAsync(ticket.CreadoPorId, cancelacion);
        if (AvisosTicket.Componer(ticket, creador, actor, motivo, detalle, Ahora) is { } aviso)
        {
            notificaciones.Encolar(aviso);
        }
    }

    private static string Cambio(EstadoTicket de, EstadoTicket a) => $"{AvisosTicket.NombreEstado(de)} → {AvisosTicket.NombreEstado(a)}";

    private static string Mensaje(string texto) => $"Mensaje de Mesa de ayuda:\n\"{texto}\"";

    private static string NombreBorrador(Ticket ticket) =>
        ticket.Titulo.Length > 0 ? ticket.Titulo
        : ticket.Descripcion.Length > 0 ? TextoAuditoria.Truncar(ticket.Descripcion, 60)
        : "Sin título";

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
