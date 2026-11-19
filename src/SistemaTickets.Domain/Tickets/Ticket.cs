namespace SistemaTickets.Domain.Tickets;

public class Ticket
{
    public const string DetalleDesdeBorrador = "borrador";
    public const string DetalleRegistroDirecto = "directo";

    private readonly List<Actividad> _actividades = [];
    private readonly List<Adjunto> _adjuntos = [];

    public int Id { get; private set; }
    public int? Numero { get; private set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public EstadoTicket Estado { get; private set; } = EstadoTicket.Borrador;
    public bool Privado { get; private set; }
    public bool RegistroDirecto { get; private set; }
    public int? LocalidadId { get; set; }
    public int? SistemaId { get; set; }
    public int? SubsistemaId { get; set; }
    public int? CriticidadId { get; set; }
    public int? TipoProblemaId { get; set; }
    public int CreadoPorId { get; init; }
    public int? OperadorId { get; private set; }
    public int? ElevadoAGrupoId { get; private set; }
    public int? ElevadoAUsuarioId { get; private set; }
    public string? Resolucion { get; private set; }
    public bool ResolucionEnDescripcion { get; private set; }
    public int? SolucionAplicadaId { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }
    public DateTime? CerradoEn { get; private set; }

    public IReadOnlyList<Actividad> Actividades => _actividades;
    public IReadOnlyList<Adjunto> Adjuntos => _adjuntos;

    public static Ticket NuevoBorrador(int autorId, DateTime fecha) =>
        new() { CreadoPorId = autorId, CreadoEn = fecha, ActualizadoEn = fecha };

    public void Tocar(DateTime fecha) => ActualizadoEn = fecha;

    public void ReemplazarAdjuntosDescripcion(IEnumerable<Adjunto> adjuntos)
    {
        var nuevos = adjuntos.ToList();
        _adjuntos.RemoveAll(a => a.Origen == OrigenAdjunto.Descripcion && nuevos.All(n => n.Clave != a.Clave));
        _adjuntos.AddRange(nuevos.Where(n => _adjuntos.All(a => a.Clave != n.Clave)));
    }

    public void Registrar(int numero, bool privado, bool desdeBorrador, int autorId, DateTime fecha)
    {
        Exigir(Estado == EstadoTicket.Borrador, "El ticket ya está registrado.");
        Numero = numero;
        Privado = privado;
        CreadoEn = fecha;
        Cambiar(EstadoTicket.Abierto, fecha);
        Anotar(new Actividad
        {
            Tipo = TipoActividad.Creacion,
            AutorId = autorId,
            Fecha = fecha,
            EstadoNuevo = EstadoTicket.Abierto,
            Detalle = desdeBorrador ? DetalleDesdeBorrador : null,
        });
    }

    public void RegistrarResuelto(int numero, bool privado, bool desdeBorrador, int autorId, DateTime fecha, string? resolucion, int? solucionAplicadaId, IEnumerable<Adjunto> adjuntos)
    {
        Exigir(Estado == EstadoTicket.Borrador, "El ticket ya está registrado.");
        Numero = numero;
        Privado = privado;
        RegistroDirecto = true;
        CreadoEn = fecha;
        OperadorId = autorId;
        Anotar(new Actividad
        {
            Tipo = TipoActividad.Creacion,
            AutorId = autorId,
            Fecha = fecha,
            EstadoNuevo = EstadoTicket.EnProceso,
            OperadorId = autorId,
            Detalle = desdeBorrador ? DetalleDesdeBorrador : null,
        });
        Cambiar(EstadoTicket.Cerrado, fecha);
        ResolucionEnDescripcion = string.IsNullOrWhiteSpace(resolucion);
        Resolucion = ResolucionEnDescripcion ? Descripcion : resolucion;
        SolucionAplicadaId = solucionAplicadaId;
        if (!ResolucionEnDescripcion)
        {
            _adjuntos.AddRange(adjuntos);
        }

        Anotar(new Actividad { Tipo = TipoActividad.Cierre, AutorId = autorId, Fecha = fecha, Detalle = DetalleRegistroDirecto });
    }

    public Actividad Comentar(int autorId, DateTime fecha, string texto, bool privada) =>
        Anotar(new Actividad { Tipo = TipoActividad.Comentario, AutorId = autorId, Fecha = fecha, Texto = texto, Privada = privada });

    public void AgregarAdjuntos(IEnumerable<Adjunto> adjuntos) => _adjuntos.AddRange(adjuntos);

    public void Asignar(int operadorId, int autorId, DateTime fecha)
    {
        Exigir(Estado is not (EstadoTicket.Borrador or EstadoTicket.Cerrado), "El ticket está cerrado.");
        Exigir(OperadorId != operadorId, "El ticket ya lo atiende esa persona.");
        var anterior = Estado;
        OperadorId = operadorId;
        if (Estado == EstadoTicket.Abierto)
        {
            Cambiar(EstadoTicket.EnProceso, fecha);
        }

        Anotar(new Actividad { Tipo = TipoActividad.Asignacion, AutorId = autorId, Fecha = fecha, OperadorId = operadorId, EstadoAnterior = anterior, EstadoNuevo = Estado });
    }

    public void Liberar(int autorId, DateTime fecha)
    {
        Exigir(Estado == EstadoTicket.EnProceso, "Sólo se puede devolver a la cola un ticket En proceso.");
        OperadorId = null;
        Cambiar(EstadoTicket.Abierto, fecha);
        Anotar(new Actividad { Tipo = TipoActividad.Liberacion, AutorId = autorId, Fecha = fecha, EstadoAnterior = EstadoTicket.EnProceso, EstadoNuevo = EstadoTicket.Abierto });
    }

    public void Elevar(int? grupoId, int? usuarioId, string motivo, int autorId, DateTime fecha)
    {
        Exigir(Estado == EstadoTicket.EnProceso, "Sólo se eleva un ticket En proceso.");
        Exigir(grupoId is null != usuarioId is null, "Elegí a quién se eleva.");
        Exigir(grupoId != ElevadoAGrupoId || usuarioId != ElevadoAUsuarioId, "El ticket ya está elevado a ese destino.");
        Exigir(usuarioId is null || usuarioId != OperadorId, "Esa persona ya atiende el ticket.");
        ElevadoAGrupoId = grupoId;
        ElevadoAUsuarioId = usuarioId;
        if (usuarioId is not null)
        {
            OperadorId = usuarioId;
        }

        Anotar(new Actividad { Tipo = TipoActividad.Elevacion, AutorId = autorId, Fecha = fecha, Texto = motivo, DestinoGrupoId = grupoId, DestinoUsuarioId = usuarioId });
    }

    public void Bloquear(string motivo, bool privada, int autorId, DateTime fecha)
    {
        Exigir(Estado == EstadoTicket.EnProceso, "Sólo se bloquea un ticket En proceso.");
        Cambiar(EstadoTicket.Bloqueado, fecha);
        Anotar(new Actividad { Tipo = TipoActividad.Bloqueo, AutorId = autorId, Fecha = fecha, Texto = motivo, Privada = privada, EstadoAnterior = EstadoTicket.EnProceso, EstadoNuevo = EstadoTicket.Bloqueado });
    }

    public void Desbloquear(string? texto, int autorId, DateTime fecha)
    {
        Exigir(Estado == EstadoTicket.Bloqueado, "El ticket no está bloqueado.");
        Cambiar(EstadoTicket.EnProceso, fecha);
        Anotar(new Actividad { Tipo = TipoActividad.Desbloqueo, AutorId = autorId, Fecha = fecha, Texto = texto, EstadoAnterior = EstadoTicket.Bloqueado, EstadoNuevo = EstadoTicket.EnProceso });
    }

    public void Reclasificar(int sistemaId, int? subsistemaId, int criticidadId, int tipoProblemaId, string detalle, int autorId, DateTime fecha)
    {
        Exigir(Estado is not (EstadoTicket.Borrador or EstadoTicket.Cerrado), "El ticket está cerrado: reabrilo para modificarlo.");
        SistemaId = sistemaId;
        SubsistemaId = subsistemaId;
        CriticidadId = criticidadId;
        TipoProblemaId = tipoProblemaId;
        Anotar(new Actividad { Tipo = TipoActividad.ModificacionDatos, AutorId = autorId, Fecha = fecha, Detalle = detalle });
    }

    public void Cerrar(string resolucion, int? solucionAplicadaId, int? tipoProblemaId, IEnumerable<Adjunto> adjuntos, int autorId, DateTime fecha)
    {
        Exigir(Estado is not (EstadoTicket.Borrador or EstadoTicket.Cerrado), "El ticket ya está cerrado.");
        var anterior = Estado;
        Cambiar(EstadoTicket.Cerrado, fecha);
        TipoProblemaId = tipoProblemaId ?? TipoProblemaId;
        OperadorId ??= autorId;
        Resolucion = resolucion;
        ResolucionEnDescripcion = false;
        SolucionAplicadaId = solucionAplicadaId;
        _adjuntos.RemoveAll(a => a.Origen == OrigenAdjunto.Resolucion);
        _adjuntos.AddRange(adjuntos);
        Anotar(new Actividad { Tipo = TipoActividad.Cierre, AutorId = autorId, Fecha = fecha, Texto = resolucion, EstadoAnterior = anterior });
    }

    public void Reabrir(string? motivo, int autorId, DateTime fecha)
    {
        Exigir(Estado == EstadoTicket.Cerrado, "Sólo se puede reabrir un ticket cerrado.");
        Cambiar(EstadoTicket.EnProceso, fecha);
        OperadorId = autorId;
        Anotar(new Actividad { Tipo = TipoActividad.Reapertura, AutorId = autorId, Fecha = fecha, Texto = motivo, OperadorId = autorId, EstadoAnterior = EstadoTicket.Cerrado, EstadoNuevo = EstadoTicket.EnProceso });
    }

    public void CambiarPrivado(bool privado, int autorId, DateTime fecha)
    {
        Exigir(Estado != EstadoTicket.Borrador, "El ticket no existe.");
        Exigir(Privado != privado, privado ? "El ticket ya es privado." : "El ticket ya es visible para la localidad.");
        Privado = privado;
        Anotar(new Actividad { Tipo = TipoActividad.Privacidad, AutorId = autorId, Fecha = fecha, Privada = privado });
    }

    private void Cambiar(EstadoTicket nuevo, DateTime fecha)
    {
        Exigir(TransicionesTicket.EsPermitida(Estado, nuevo), $"No se puede pasar de {Estado} a {nuevo}.");
        Estado = nuevo;
        CerradoEn = nuevo == EstadoTicket.Cerrado ? fecha : null;
    }

    private Actividad Anotar(Actividad actividad)
    {
        _actividades.Add(actividad);
        ActualizadoEn = actividad.Fecha;
        return actividad;
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
        {
            throw new ReglaTicketException(mensaje);
        }
    }
}

public class ReglaTicketException(string mensaje) : Exception(mensaje);
