namespace SistemaTickets.Domain.Tickets;

public class Ticket
{
    private readonly List<Actividad> _actividades = [];

    public int Id { get; private set; }
    public int? Numero { get; private set; }
    public required string Titulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public EstadoTicket Estado { get; private set; } = EstadoTicket.Borrador;
    public bool Privado { get; set; }
    public int LocalidadId { get; set; }
    public int CreadoPorId { get; set; }
    public int? OperadorId { get; set; }
    public int? SistemaId { get; set; }
    public int? SubsistemaId { get; set; }
    public int? CriticidadId { get; set; }
    public int? TipoProblemaId { get; set; }
    public int? ElevadoAGrupoId { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? CerradoEn { get; private set; }
    public IReadOnlyList<Actividad> Actividades => _actividades;

    public void CambiarEstado(EstadoTicket nuevo, int autorId, DateTime fecha)
    {
        if (!TransicionesTicket.EsPermitida(Estado, nuevo))
        {
            throw new InvalidOperationException($"No se puede pasar de {Estado} a {nuevo}.");
        }

        _actividades.Add(new Actividad
        {
            Tipo = TipoActividad.CambioEstado,
            AutorId = autorId,
            Fecha = fecha,
            EstadoAnterior = Estado,
            EstadoNuevo = nuevo,
        });

        Estado = nuevo;
        CerradoEn = nuevo == EstadoTicket.Cerrado ? fecha : null;
    }
}
