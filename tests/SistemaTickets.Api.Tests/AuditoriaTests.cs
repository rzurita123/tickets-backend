using System.Net;
using System.Net.Http.Json;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class AuditoriaTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Crear_un_ticket_queda_auditado()
    {
        var cliente = await factory.ClienteConSesion("mtechera");
        var admin = await factory.ClienteConSesion("scabrera");
        var ticket = await cliente.CrearTicket("Auditado");

        var registros = await admin.GetAsync($"/api/auditoria?ticketId={ticket.Id}").Ok<List<RegistroAuditoriaDto>>();

        var creado = Assert.Single(registros, r => r.Operacion == "TICKET_CREADO");
        Assert.Equal("mtechera", creado.Usuario);
        Assert.Equal($"#{ticket.Numero} · Auditado", creado.Detalle);
        Assert.Equal(ticket.Id, creado.TicketId);
    }

    [Fact]
    public async Task Las_acciones_de_ORMEN_quedan_auditadas()
    {
        var operador = await factory.ClienteConSesion("vpereira");
        var admin = await factory.ClienteConSesion("scabrera");
        var ticket = await (await factory.ClienteConSesion("mtechera")).CrearTicket("Con acciones");
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/asignar", new SolicitudAsignar("vpereira")).Ok<TicketDto>();
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/cerrar", new SolicitudCierre("Listo.", null, null, null, null)).Ok<TicketDto>();

        var registros = await admin.GetAsync($"/api/auditoria?ticketId={ticket.Id}").Ok<List<RegistroAuditoriaDto>>();

        Assert.Contains(registros, r => r.Operacion == "ASIGNACION" && r.Detalle == "Atiende: Valeria Pereira · Abierto → En proceso");
        Assert.Contains(registros, r => r.Operacion == "CIERRE" && r.Detalle == "En proceso → Cerrado");
    }

    [Fact]
    public async Task Solo_un_administrador_consulta_la_auditoria()
    {
        var operador = await factory.ClienteConSesion("vpereira");

        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/auditoria")).StatusCode);
    }

    [Fact]
    public async Task El_ingreso_queda_auditado()
    {
        await factory.ClienteConSesion("lgomez");
        var admin = await factory.ClienteConSesion("scabrera");

        var registros = await admin.GetAsync("/api/auditoria?operacion=LOGIN&usuario=lgomez").Ok<List<RegistroAuditoriaDto>>();

        Assert.NotEmpty(registros);
        Assert.All(registros, r => Assert.Equal("lgomez", r.Usuario));
    }

    [Fact]
    public async Task Abrir_un_ticket_de_otra_localidad_queda_auditado()
    {
        var ticket = await (await factory.ClienteConSesion("mtechera")).CrearTicket("De Pando");
        var lagomar = await factory.ClienteConSesion("lgomez");
        var admin = await factory.ClienteConSesion("scabrera");

        var respuesta = await lagomar.GetAsync($"/api/tickets/{ticket.Id}");
        var registros = await admin.GetAsync($"/api/auditoria?operacion=ACCESO_DENEGADO&ticketId={ticket.Id}").Ok<List<RegistroAuditoriaDto>>();

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("lgomez", Assert.Single(registros).Usuario);
    }

    [Fact]
    public async Task Los_cambios_de_catalogo_quedan_auditados()
    {
        var admin = await factory.ClienteConSesion("ralvarez");
        await admin.PostAsJsonAsync("/api/catalogos/GruposSoporte", new SolicitudItemCatalogo("Redes auditadas", null, null)).Ok<ItemCatalogoDto>();

        var registros = await admin.GetAsync("/api/auditoria?operacion=CATALOGO").Ok<List<RegistroAuditoriaDto>>();

        Assert.Contains(registros, r => r.Detalle == "Grupos de soporte: alta de Redes auditadas" && r.Usuario == "ralvarez");
    }
}
