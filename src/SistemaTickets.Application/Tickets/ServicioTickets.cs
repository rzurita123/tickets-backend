using System.Text.Json;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Localidades;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Tickets;

public class ErrorTicket(string mensaje) : Exception(mensaje);

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
    TimeProvider reloj)
{
    public const string TipoProblemaNoDefinido = "tp-nodef";
    private const int LargoMaximoTitulo = 120;

    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    public Task<IReadOnlyList<Ticket>> ListarAsync(Usuario usuario, CancellationToken cancelacion = default) =>
        tickets.ListarParaAsync(usuario, cancelacion);

    public async Task<Ticket> CrearAsync(Usuario usuario, DatosTicket datos, OpcionesAlta opciones, CancellationToken cancelacion = default)
    {
        if (usuario.Rol == Rol.Cliente && (opciones.RegistroDirecto || opciones.Privado))
        {
            throw new SinPermisoException("Sólo ORMEN puede registrar tickets resueltos o privados.");
        }

        var titulo = Limpiar(datos.Titulo);
        var descripcion = Limpiar(datos.Descripcion);
        Exigir(titulo.Length > 0, "Escribí un título.");
        Exigir(titulo.Length <= LargoMaximoTitulo, $"El título puede tener hasta {LargoMaximoTitulo} caracteres.");
        Exigir(descripcion.Length > 0, "Describí el problema.");

        var localidadId = usuario.Rol == Rol.Cliente ? usuario.LocalidadId : datos.LocalidadId;
        var localidad = (await localidades.ListarAsync(cancelacion)).FirstOrDefault(l => l.Id == localidadId);
        Exigir(localidad is not null, "Elegí la localidad.");
        Exigir(localidad!.Activa, "Esa localidad está inactiva.");

        var sistema = await ItemAsync(TipoCatalogo.Sistemas, datos.Sistema, cancelacion);
        Exigir(sistema is { Activo: true }, "Elegí el sistema o servicio afectado.");
        var subsistema = await SubsistemaAsync(datos.Subsistema, sistema!, soloActivo: true, cancelacion);
        var criticidad = await ItemAsync(TipoCatalogo.Criticidades, datos.Criticidad, cancelacion);
        Exigir(criticidad is not null, "Elegí la criticidad.");
        var tipoProblema = await ItemAsync(
            TipoCatalogo.TiposProblema,
            usuario.EsOrmen && !string.IsNullOrEmpty(datos.TipoProblema) ? datos.TipoProblema : TipoProblemaNoDefinido,
            cancelacion);

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
        return ticket;
    }

    public async Task<Ticket> GuardarBorradorAsync(Usuario usuario, int? id, DatosTicket datos, CancellationToken cancelacion = default)
    {
        var titulo = Limpiar(datos.Titulo);
        var descripcion = Limpiar(datos.Descripcion);
        var localidadId = usuario.Rol == Rol.Cliente ? usuario.LocalidadId : datos.LocalidadId;
        var tieneAlgo = titulo.Length > 0 || descripcion.Length > 0 || localidadId is not null
            || !string.IsNullOrEmpty(datos.Sistema) || datos.Imagenes.Count > 0;
        Exigir(tieneAlgo, "Para guardar un borrador escribí algo.");

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

        var sistema = await ItemAsync(TipoCatalogo.Sistemas, datos.Sistema, cancelacion);
        var subsistema = (await ItemAsync(TipoCatalogo.Subsistemas, datos.Subsistema, cancelacion)) as Subsistema;
        ticket.Titulo = titulo;
        ticket.Descripcion = descripcion;
        ticket.LocalidadId = (await localidades.ListarAsync(cancelacion)).FirstOrDefault(l => l.Id == localidadId)?.Id;
        ticket.SistemaId = sistema?.Id;
        ticket.SubsistemaId = subsistema is not null && subsistema.SistemaId == sistema?.Id ? subsistema.Id : null;
        ticket.CriticidadId = (await ItemAsync(TipoCatalogo.Criticidades, datos.Criticidad, cancelacion))?.Id;
        ticket.TipoProblemaId = (await ItemAsync(TipoCatalogo.TiposProblema, datos.TipoProblema ?? TipoProblemaNoDefinido, cancelacion))?.Id;
        ticket.ReemplazarAdjuntosDescripcion(Adjuntos(datos.Imagenes, OrigenAdjunto.Descripcion, usuario.Id, ahora));
        ticket.Tocar(ahora);

        await tickets.GuardarAsync(cancelacion);
        return ticket;
    }

    public async Task<bool> DescartarBorradorAsync(Usuario usuario, int id, CancellationToken cancelacion = default)
    {
        var ticket = await BorradorAsync(usuario, id, cancelacion);
        if (ticket is null)
        {
            return false;
        }

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
        Exigir(limpio.Length > 0, "Escribí el comentario.");
        var ahora = Ahora;
        var comentario = ticket.Comentar(usuario.Id, ahora, limpio, usuario.EsOrmen && privado);
        ticket.AgregarAdjuntos(Adjuntos(imagenes, OrigenAdjunto.Comentario, usuario.Id, ahora, comentario));
        await tickets.GuardarAsync(cancelacion);
        return ticket;
    }

    public Task<Ticket?> AsignarAsync(Usuario usuario, int id, string? operador, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var persona = await PersonaOrmenAsync(operador, cancelacion);
            ticket.Asignar(persona.Id, usuario.Id, Ahora);
        }, cancelacion);

    public Task<Ticket?> LiberarAsync(Usuario usuario, int id, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, ticket =>
        {
            ticket.Liberar(usuario.Id, Ahora);
            return Task.CompletedTask;
        }, cancelacion);

    public Task<Ticket?> ElevarAsync(Usuario usuario, int id, string? grupo, string? persona, string? motivo, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var limpio = Limpiar(motivo);
            Exigir(limpio.Length > 0, "Escribí por qué se eleva.");
            int? grupoId = null;
            int? usuarioId = null;
            if (!string.IsNullOrEmpty(grupo))
            {
                var item = await ItemAsync(TipoCatalogo.GruposSoporte, grupo, cancelacion);
                Exigir(item is not null, "Elegí a quién se eleva.");
                Exigir(item!.Activo, "Ese grupo está inactivo.");
                grupoId = item.Id;
            }
            else
            {
                usuarioId = (await PersonaOrmenAsync(persona, cancelacion)).Id;
            }

            ticket.Elevar(grupoId, usuarioId, limpio, usuario.Id, Ahora);
        }, cancelacion);

    public Task<Ticket?> BloquearAsync(Usuario usuario, int id, string? texto, bool privado, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, ticket =>
        {
            var limpio = Limpiar(texto);
            Exigir(limpio.Length > 0, "Escribí qué factor externo lo detiene.");
            ticket.Bloquear(limpio, privado, usuario.Id, Ahora);
            return Task.CompletedTask;
        }, cancelacion);

    public Task<Ticket?> DesbloquearAsync(Usuario usuario, int id, string? texto, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, ticket =>
        {
            ticket.Desbloquear(Vacio(texto), usuario.Id, Ahora);
            return Task.CompletedTask;
        }, cancelacion);

    public Task<Ticket?> ReclasificarAsync(Usuario usuario, int id, string? sistema, string? subsistema, string? criticidad, string? tipoProblema, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var nuevoSistema = await ItemAsync(TipoCatalogo.Sistemas, sistema, cancelacion);
            Exigir(nuevoSistema is not null, "Elegí el sistema o servicio afectado.");
            var nuevoSubsistema = await SubsistemaAsync(subsistema, nuevoSistema!, soloActivo: false, cancelacion);
            var nuevaCriticidad = await ItemAsync(TipoCatalogo.Criticidades, criticidad, cancelacion);
            Exigir(nuevaCriticidad is not null, "Elegí la criticidad.");
            var nuevoTipo = await ItemAsync(TipoCatalogo.TiposProblema, tipoProblema, cancelacion);
            Exigir(nuevoTipo is not null, "Elegí el tipo de problema.");

            var cambios = new List<CambioClasificacion>();
            await CompararAsync(cambios, "sistemaId", "Sistema", TipoCatalogo.Sistemas, ticket.SistemaId, nuevoSistema, cancelacion);
            await CompararAsync(cambios, "subsistemaId", "Subsistema", TipoCatalogo.Subsistemas, ticket.SubsistemaId, nuevoSubsistema, cancelacion);
            await CompararAsync(cambios, "criticidadId", "Criticidad", TipoCatalogo.Criticidades, ticket.CriticidadId, nuevaCriticidad, cancelacion);
            await CompararAsync(cambios, "tipoProblemaId", "Tipo de problema", TipoCatalogo.TiposProblema, ticket.TipoProblemaId, nuevoTipo, cancelacion);
            Exigir(cambios.Count > 0, "No hay cambios para guardar.");

            ticket.Reclasificar(
                nuevoSistema!.Id, nuevoSubsistema?.Id, nuevaCriticidad!.Id, nuevoTipo!.Id,
                JsonSerializer.Serialize(cambios, JsonSerializerOptions.Web), usuario.Id, Ahora);
        }, cancelacion);

    public Task<Ticket?> CerrarAsync(Usuario usuario, int id, DatosCierre datos, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var texto = Limpiar(datos.Texto);
            Exigir(texto.Length > 0, "Escribí la resolución.");
            ItemCatalogo? tipo = null;
            if (!string.IsNullOrEmpty(datos.TipoProblema))
            {
                tipo = await ItemAsync(TipoCatalogo.TiposProblema, datos.TipoProblema, cancelacion);
                Exigir(tipo is not null, "Elegí el tipo de problema.");
            }

            var solucionAplicada = await SolucionAplicadaAsync(datos.SolucionCatalogoId, cancelacion);
            var ahora = Ahora;
            var mensaje = Limpiar(datos.MensajePublico);
            if (mensaje.Length > 0 && ticket.Estado != EstadoTicket.Cerrado)
            {
                ticket.Comentar(usuario.Id, ahora, mensaje, privada: false);
            }

            ticket.Cerrar(
                texto, solucionAplicada, tipo?.Id,
                Adjuntos(datos.Imagenes, OrigenAdjunto.Resolucion, usuario.Id, ahora), usuario.Id, ahora);
        }, cancelacion);

    public Task<Ticket?> ReabrirAsync(Usuario usuario, int id, string? motivo, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, ticket =>
        {
            ticket.Reabrir(Vacio(motivo), usuario.Id, Ahora);
            return Task.CompletedTask;
        }, cancelacion);

    public Task<Ticket?> CambiarPrivadoAsync(Usuario usuario, int id, bool privado, CancellationToken cancelacion = default) =>
        TratarAsync(usuario, id, async ticket =>
        {
            var creador = await usuarios.BuscarPorIdAsync(ticket.CreadoPorId, cancelacion);
            Exigir(creador?.Rol != Rol.Cliente, "Este ticket lo creó la localidad: no se puede hacer privado.");
            ticket.CambiarPrivado(privado, usuario.Id, Ahora);
        }, cancelacion);

    public static bool PuedeVer(Ticket ticket, Usuario usuario)
    {
        if (ticket.Estado == EstadoTicket.Borrador)
        {
            return ticket.CreadoPorId == usuario.Id;
        }

        return usuario.EsOrmen
            || (!ticket.Privado && usuario.LocalidadId is not null && ticket.LocalidadId == usuario.LocalidadId);
    }

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
        Exigir(solucion is null || solucion.Estado == EstadoSolucion.Aprobada, "La solución reutilizable elegida ya no está aprobada.");
        return solucion?.Id;
    }

    private async Task<ItemCatalogo?> ItemAsync(TipoCatalogo tipo, string? codigo, CancellationToken cancelacion) =>
        string.IsNullOrEmpty(codigo) ? null : (await catalogos.ListarAsync(tipo, cancelacion)).FirstOrDefault(x => x.Codigo == codigo);

    private async Task<Subsistema?> SubsistemaAsync(string? codigo, ItemCatalogo sistema, bool soloActivo, CancellationToken cancelacion)
    {
        if (string.IsNullOrEmpty(codigo))
        {
            return null;
        }

        var subsistema = await ItemAsync(TipoCatalogo.Subsistemas, codigo, cancelacion) as Subsistema;
        Exigir(
            subsistema is not null && subsistema.SistemaId == sistema.Id && (!soloActivo || subsistema.Activo),
            "Ese subsistema no corresponde al sistema elegido.");
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

    private static string Limpiar(string? texto) => (texto ?? string.Empty).Trim();

    private static string? Vacio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
        {
            throw new ErrorTicket(mensaje);
        }
    }
}
