namespace SistemaTickets.Domain.Tickets;

public static class TransicionesTicket
{
    private static readonly Dictionary<EstadoTicket, EstadoTicket[]> Permitidas = new()
    {
        [EstadoTicket.Borrador] = [EstadoTicket.Abierto, EstadoTicket.Cerrado],
        [EstadoTicket.Abierto] = [EstadoTicket.EnProceso, EstadoTicket.Cerrado],
        [EstadoTicket.EnProceso] = [EstadoTicket.Abierto, EstadoTicket.Bloqueado, EstadoTicket.Cerrado],
        [EstadoTicket.Bloqueado] = [EstadoTicket.EnProceso, EstadoTicket.Cerrado],
        [EstadoTicket.Cerrado] = [EstadoTicket.EnProceso],
    };

    public static bool EsPermitida(EstadoTicket desde, EstadoTicket hacia) =>
        Permitidas[desde].Contains(hacia);

    public static IReadOnlyList<EstadoTicket> Desde(EstadoTicket estado) => Permitidas[estado];
}
