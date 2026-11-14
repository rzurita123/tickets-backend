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
    public void Cambiar_estado_registra_la_actividad_y_la_fecha_de_cierre()
    {
        var ticket = new Ticket { Titulo = "Terminal sin conexión" };
        var fecha = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

        ticket.CambiarEstado(EstadoTicket.Abierto, autorId: 1, fecha);
        ticket.CambiarEstado(EstadoTicket.Cerrado, autorId: 2, fecha);

        Assert.Equal(EstadoTicket.Cerrado, ticket.Estado);
        Assert.Equal(fecha, ticket.CerradoEn);
        Assert.Equal(2, ticket.Actividades.Count);
    }

    [Fact]
    public void Cambiar_estado_falla_si_la_transicion_no_esta_permitida()
    {
        var ticket = new Ticket { Titulo = "Terminal sin conexión" };

        Assert.Throws<InvalidOperationException>(
            () => ticket.CambiarEstado(EstadoTicket.Bloqueado, autorId: 1, DateTime.UtcNow));
    }
}
