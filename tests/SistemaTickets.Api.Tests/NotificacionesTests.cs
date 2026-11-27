using System.Net.Http.Json;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class NotificacionesTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Una_respuesta_publica_de_ORMEN_avisa_al_creador()
    {
        var cliente = await factory.ClienteConSesion("mtechera");
        var operador = await factory.ClienteConSesion("vpereira");
        var ticket = await cliente.CrearTicket("Con respuesta");

        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/comentarios", new SolicitudComentario("Ya lo estamos viendo.", false, null)).Ok<TicketDto>();
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/comentarios", new SolicitudComentario("Nota interna.", true, null)).Ok<TicketDto>();
        var correos = await cliente.GetAsync("/api/notificaciones").Ok<List<NotificacionDto>>();

        var correo = Assert.Single(correos, c => c.TicketId == ticket.Id);
        Assert.Equal($"Ticket #{ticket.Numero}: nueva respuesta de Mesa de ayuda", correo.Asunto);
        Assert.Contains("\"Ya lo estamos viendo.\"", correo.Cuerpo);
        Assert.StartsWith("Hola Marcelo:", correo.Cuerpo);
        Assert.Equal("mtechera", correo.Para);
        Assert.Equal("CORREO", correo.Canal);
    }

    [Fact]
    public async Task Cada_cliente_ve_solo_sus_correos()
    {
        var pando = await factory.ClienteConSesion("mtechera");
        var lagomar = await factory.ClienteConSesion("lgomez");
        var operador = await factory.ClienteConSesion("vpereira");
        var ticket = await pando.CrearTicket("Sólo para Pando");
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/asignar", new SolicitudAsignar("vpereira")).Ok<TicketDto>();

        var dePando = await pando.GetAsync("/api/notificaciones").Ok<List<NotificacionDto>>();
        var deLagomar = await lagomar.GetAsync("/api/notificaciones").Ok<List<NotificacionDto>>();
        var deOrmen = await operador.GetAsync("/api/notificaciones").Ok<List<NotificacionDto>>();

        Assert.Contains(dePando, c => c.TicketId == ticket.Id && c.Asunto.EndsWith("Mesa de ayuda lo está atendiendo"));
        Assert.DoesNotContain(deLagomar, c => c.TicketId == ticket.Id);
        Assert.Contains(deOrmen, c => c.TicketId == ticket.Id);
    }

    [Fact]
    public async Task El_cierre_avisa_con_el_mensaje_publico()
    {
        var cliente = await factory.ClienteConSesion("mtechera");
        var operador = await factory.ClienteConSesion("nacosta");
        var ticket = await cliente.CrearTicket("Para cerrar");
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/asignar", new SolicitudAsignar("nacosta")).Ok<TicketDto>();

        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/cerrar",
            new SolicitudCierre("Interno.", "Quedó resuelto.", null, null, null)).Ok<TicketDto>();
        var correos = await cliente.GetAsync("/api/notificaciones").Ok<List<NotificacionDto>>();

        var cierre = Assert.Single(correos, c => c.TicketId == ticket.Id && c.Asunto == $"Ticket #{ticket.Numero} cerrado");
        Assert.Contains("\"Quedó resuelto.\"", cierre.Cuerpo);
        Assert.DoesNotContain("Interno.", cierre.Cuerpo);
    }
}
