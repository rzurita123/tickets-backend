using System.Net;
using System.Net.Http.Json;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class SolucionesTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    private static SolicitudSolucion Nueva(string titulo, params string[] palabras) =>
        new(titulo, "Reiniciar el servicio y volver a intentar.", "sis-caja", "sub-caja-cierre", [.. palabras],
            [new ImagenSolicitud("paso-1", "paso.png", Sesiones.Imagen)]);

    [Fact]
    public async Task Un_operador_lista_las_soluciones_aprobadas()
    {
        var operador = await factory.ClienteConSesion("vpereira");

        var lista = await operador.GetAsync("/api/soluciones?estado=aprobada").Ok<List<SolucionDto>>();

        Assert.Contains(lista, s => s.Codigo == "sol-term-impresora");
        Assert.All(lista, s => Assert.Equal("APROBADA", s.Estado));
    }

    [Fact]
    public async Task Un_cliente_no_accede_a_las_soluciones()
    {
        var cliente = await factory.ClienteConSesion("mtechera");

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/soluciones")).StatusCode);
    }

    [Fact]
    public async Task Un_operador_propone_y_un_administrador_aprueba()
    {
        var operador = await factory.ClienteConSesion("vpereira");
        var admin = await factory.ClienteConSesion("scabrera");

        var propuesta = await operador.PostAsJsonAsync("/api/soluciones", Nueva("Caja que no cierra", "Caja", " caja ", "Cierre"))
            .Ok<SolucionDto>();
        var aprobada = await admin.PostAsync($"/api/soluciones/{propuesta.Codigo}/aprobar", null).Ok<SolucionDto>();

        Assert.Equal("PENDIENTE", propuesta.Estado);
        Assert.Equal(["caja", "cierre"], propuesta.PalabrasClave);
        Assert.Single(propuesta.Adjuntos);
        Assert.Equal("APROBADA", aprobada.Estado);
        Assert.Equal("scabrera", aprobada.RevisadaPor);
    }

    [Fact]
    public async Task Un_administrador_la_carga_ya_aprobada()
    {
        var admin = await factory.ClienteConSesion("scabrera");

        var solucion = await admin.PostAsJsonAsync("/api/soluciones", Nueva("Impresora sin papel", "papel")).Ok<SolucionDto>();

        Assert.Equal("APROBADA", solucion.Estado);
        Assert.Equal("scabrera", solucion.CreadaPor);
    }

    [Fact]
    public async Task Rechazar_exige_el_motivo()
    {
        var operador = await factory.ClienteConSesion("nacosta");
        var admin = await factory.ClienteConSesion("ralvarez");
        var propuesta = await operador.PostAsJsonAsync("/api/soluciones", Nueva("Para rechazar", "rechazo")).Ok<SolucionDto>();

        var sinMotivo = await admin.PostAsJsonAsync($"/api/soluciones/{propuesta.Codigo}/rechazar", new SolicitudRechazo(" "));
        var rechazada = await admin.PostAsJsonAsync($"/api/soluciones/{propuesta.Codigo}/rechazar", new SolicitudRechazo("Ya existe otra igual."))
            .Ok<SolucionDto>();

        Assert.Contains("motivo", (await sinMotivo.ErroresAsync()).Keys);
        Assert.Equal("RECHAZADA", rechazada.Estado);
        Assert.Equal("Ya existe otra igual.", rechazada.MotivoRechazo);
    }

    [Fact]
    public async Task Solo_un_administrador_edita()
    {
        var operador = await factory.ClienteConSesion("vpereira");
        var admin = await factory.ClienteConSesion("scabrera");
        var solucion = await admin.PostAsJsonAsync("/api/soluciones", Nueva("Para editar", "editar")).Ok<SolucionDto>();
        var cambios = Nueva("Editada", "editar") with { Adjuntos = [new ImagenSolicitud("paso-1", null, null)] };

        var deOperador = await operador.PutAsJsonAsync($"/api/soluciones/{solucion.Codigo}", cambios);
        var editada = await admin.PutAsJsonAsync($"/api/soluciones/{solucion.Codigo}", cambios).Ok<SolucionDto>();

        Assert.Equal(HttpStatusCode.Forbidden, deOperador.StatusCode);
        Assert.Equal("Editada", editada.Titulo);
        Assert.Equal("paso-1", Assert.Single(editada.Adjuntos).Id);
    }

    [Fact]
    public async Task Sin_palabras_clave_devuelve_400_con_el_campo()
    {
        var operador = await factory.ClienteConSesion("vpereira");

        var respuesta = await operador.PostAsJsonAsync("/api/soluciones", Nueva("Sin palabras"));

        Assert.Contains("palabrasClave", (await respuesta.ErroresAsync()).Keys);
    }

    [Fact]
    public async Task La_resolucion_de_un_ticket_cerrado_se_propone_una_sola_vez()
    {
        var operador = await factory.ClienteConSesion("vpereira");
        var ticket = await operador.CrearTicket("Para proponer", 7);
        var abierto = await operador.PostAsJsonAsync("/api/soluciones/propuestas",
            new SolicitudPropuesta(ticket.Id, "Propuesta", "Pasos.", ["propuesta"], null));
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/asignar", new SolicitudAsignar("vpereira")).Ok<TicketDto>();
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/cerrar", new SolicitudCierre(
            "Se cambió el rollo.", null, null, null, [new ImagenSolicitud("res-1", "resolucion.png", Sesiones.Imagen)])).Ok<TicketDto>();

        var propuesta = await operador.PostAsJsonAsync("/api/soluciones/propuestas",
            new SolicitudPropuesta(ticket.Id, "Propuesta", "Pasos.", ["propuesta"], null)).Ok<SolucionDto>();
        var repetida = await operador.PostAsJsonAsync("/api/soluciones/propuestas",
            new SolicitudPropuesta(ticket.Id, "Otra", "Pasos.", ["otra"], null));

        Assert.Equal(HttpStatusCode.BadRequest, abierto.StatusCode);
        Assert.Equal(ticket.Id, propuesta.TicketOrigenId);
        Assert.Equal("res-1", Assert.Single(propuesta.Adjuntos).Id);
        Assert.Equal(HttpStatusCode.BadRequest, repetida.StatusCode);
    }

    [Fact]
    public async Task Cuenta_los_tickets_que_aplicaron_la_solucion()
    {
        var operador = await factory.ClienteConSesion("nacosta");
        var antes = (await operador.GetAsync("/api/soluciones/sol-term-impresora").Ok<SolucionDto>()).Usos;
        var ticket = await operador.CrearTicket("Con solución aplicada", 7);
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/asignar", new SolicitudAsignar("nacosta")).Ok<TicketDto>();
        await operador.PostAsJsonAsync($"/api/tickets/{ticket.Id}/cerrar",
            new SolicitudCierre("Se aplicó.", null, null, "sol-term-impresora", null)).Ok<TicketDto>();

        var despues = (await operador.GetAsync("/api/soluciones/sol-term-impresora").Ok<SolucionDto>()).Usos;

        Assert.Equal(antes + 1, despues);
    }
}
