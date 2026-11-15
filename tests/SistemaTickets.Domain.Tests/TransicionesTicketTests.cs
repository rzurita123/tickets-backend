using SistemaTickets.Domain.Tickets;

namespace SistemaTickets.Domain.Tests;

public class TransicionesTicketTests
{
    [Theory]
    [InlineData(EstadoTicket.Borrador, EstadoTicket.Abierto)]
    [InlineData(EstadoTicket.Borrador, EstadoTicket.Cerrado)]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.EnProceso)]
    [InlineData(EstadoTicket.EnProceso, EstadoTicket.Abierto)]
    [InlineData(EstadoTicket.EnProceso, EstadoTicket.Bloqueado)]
    [InlineData(EstadoTicket.Bloqueado, EstadoTicket.EnProceso)]
    [InlineData(EstadoTicket.EnProceso, EstadoTicket.Cerrado)]
    [InlineData(EstadoTicket.Cerrado, EstadoTicket.EnProceso)]
    public void Permite_las_transiciones_validas(EstadoTicket desde, EstadoTicket hacia)
    {
        Assert.True(TransicionesTicket.EsPermitida(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.Bloqueado)]
    [InlineData(EstadoTicket.Abierto, EstadoTicket.Borrador)]
    [InlineData(EstadoTicket.Cerrado, EstadoTicket.Abierto)]
    [InlineData(EstadoTicket.Cerrado, EstadoTicket.Borrador)]
    public void Rechaza_las_transiciones_invalidas(EstadoTicket desde, EstadoTicket hacia)
    {
        Assert.False(TransicionesTicket.EsPermitida(desde, hacia));
    }

    [Fact]
    public void El_recorrido_registra_actividad_y_fecha_de_cierre()
    {
        var fecha = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);
        var ticket = Ticket.NuevoBorrador(autorId: 1, fecha);

        ticket.Registrar(numero: 1001, privado: false, desdeBorrador: false, autorId: 1, fecha);
        ticket.Asignar(operadorId: 2, autorId: 2, fecha);
        ticket.Cerrar("Se reinició la terminal.", null, null, [], autorId: 2, fecha);

        Assert.Equal(EstadoTicket.Cerrado, ticket.Estado);
        Assert.Equal(fecha, ticket.CerradoEn);
        Assert.Equal(
            [TipoActividad.Creacion, TipoActividad.Asignacion, TipoActividad.Cierre],
            ticket.Actividades.Select(a => a.Tipo));
    }

    [Fact]
    public void No_se_bloquea_un_ticket_abierto()
    {
        var fecha = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);
        var ticket = Ticket.NuevoBorrador(autorId: 1, fecha);
        ticket.Registrar(numero: 1001, privado: false, desdeBorrador: false, autorId: 1, fecha);

        Assert.Throws<ReglaTicketException>(() => ticket.Bloquear("Falta un repuesto", privada: false, autorId: 2, fecha));
    }
}
