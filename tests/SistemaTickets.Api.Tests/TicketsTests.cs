using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class TicketsTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    private const string Imagen = "data:image/png;base64,iVBORw0KGgo=";

    private static SolicitudTicket Solicitud(string titulo, int? localidadId = null, bool registroDirecto = false, bool privado = false, int? borradorId = null) =>
        new(titulo, "No imprime los comprobantes.", localidadId, "sis-caja", "sub-caja-cierre", "cri-alta", null,
            [new ImagenSolicitud("img-1", "foto.png", Imagen)], borradorId, registroDirecto, privado, null, null, null);

    [Fact]
    public async Task Un_cliente_crea_un_ticket_en_su_localidad()
    {
        var cliente = await ClienteConSesion("mtechera");

        var respuesta = await cliente.PostAsJsonAsync("/api/tickets", Solicitud("Impresora"));
        var ticket = await respuesta.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(ticket);
        Assert.Equal("ABIERTO", ticket.Estado);
        Assert.True(ticket.Numero >= 1001);
        Assert.Equal("mtechera", ticket.CreadoPor);
        Assert.Equal("tp-nodef", ticket.TipoProblema);
        Assert.Equal(Imagen, ticket.Adjuntos.Single().DataUrl);
        Assert.Equal("Creacion", ticket.Actividad.Single().Tipo);

        var listado = await cliente.GetFromJsonAsync<List<TicketDto>>("/api/tickets");
        var guardado = Assert.Single(listado!, t => t.Id == ticket.Id);
        Assert.Equal(ticket.Numero, guardado.Numero);
        Assert.Equal(ticket.Titulo, guardado.Titulo);
        Assert.Equal(ticket.Estado, guardado.Estado);
        Assert.Equal(ticket.LocalidadId, guardado.LocalidadId);
    }

    [Fact]
    public async Task Un_operador_registra_un_incidente_para_la_localidad_del_cliente()
    {
        var operador = await ClienteConSesion("vpereira");
        var pando = await ClienteConSesion("mtechera");
        var lagomar = await ClienteConSesion("lgomez");
        var cliente = await pando.GetFromJsonAsync<UsuarioDto>("/api/auth/yo");
        Assert.NotNull(cliente);
        Assert.NotNull(cliente.LocalidadId);

        var solicitud = Solicitud("Incidente informado por la agencia", cliente.LocalidadId) with
        {
            TipoProblema = "tp-software",
        };
        var ticket = await Crear(operador, solicitud);

        Assert.Equal("ABIERTO", ticket.Estado);
        Assert.False(ticket.Privado);
        Assert.Equal("vpereira", ticket.CreadoPor);
        Assert.Equal(cliente.LocalidadId, ticket.LocalidadId);
        Assert.Equal(solicitud.Titulo, ticket.Titulo);
        Assert.Equal(solicitud.Descripcion, ticket.Descripcion);
        Assert.Equal(solicitud.Sistema, ticket.Sistema);
        Assert.Equal(solicitud.Subsistema, ticket.Subsistema);
        Assert.Equal(solicitud.Criticidad, ticket.Criticidad);
        Assert.Equal(solicitud.TipoProblema, ticket.TipoProblema);
        Assert.Equal(Imagen, Assert.Single(ticket.Adjuntos).DataUrl);
        Assert.Equal("vpereira", Assert.Single(ticket.Actividad).Autor);

        var dePando = await pando.GetFromJsonAsync<List<TicketDto>>("/api/tickets");
        var guardado = Assert.Single(dePando!, t => t.Id == ticket.Id);
        Assert.Equal(ticket.Numero, guardado.Numero);
        Assert.Equal(ticket.Descripcion, guardado.Descripcion);
        var deLagomar = await lagomar.GetFromJsonAsync<List<TicketDto>>("/api/tickets");
        Assert.DoesNotContain(deLagomar!, t => t.Id == ticket.Id);
    }

    [Fact]
    public async Task Sin_titulo_devuelve_400()
    {
        var cliente = await ClienteConSesion("mtechera");

        var respuesta = await cliente.PostAsJsonAsync("/api/tickets", Solicitud(" "));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_cliente_no_ve_otra_localidad_ni_lo_privado()
    {
        var operador = await ClienteConSesion("vpereira");
        var pando = await ClienteConSesion("mtechera");
        var lagomar = await ClienteConSesion("lgomez");

        var propio = await Crear(pando, Solicitud("Visible para Pando"));
        var privado = await Crear(operador, Solicitud("Privado de ORMEN", localidadId: propio.LocalidadId, privado: true));
        await operador.PostAsJsonAsync($"/api/tickets/{propio.Id}/comentarios", new SolicitudComentario("Nota interna", true, null));
        await operador.PostAsJsonAsync($"/api/tickets/{propio.Id}/comentarios", new SolicitudComentario("Lo estamos viendo", false, null));

        var dePando = await pando.GetFromJsonAsync<List<TicketDto>>("/api/tickets");
        var deLagomar = await lagomar.GetFromJsonAsync<List<TicketDto>>("/api/tickets");
        var deOrmen = await operador.GetFromJsonAsync<List<TicketDto>>("/api/tickets");

        var visto = dePando!.Single(t => t.Id == propio.Id);
        Assert.DoesNotContain(visto.Actividad, a => a.Texto == "Nota interna");
        Assert.Contains(visto.Actividad, a => a.Texto == "Lo estamos viendo");
        Assert.DoesNotContain(dePando!, t => t.Id == privado.Id);
        Assert.DoesNotContain(deLagomar!, t => t.Id == propio.Id);
        Assert.Contains(deOrmen!, t => t.Id == privado.Id);
        Assert.All(deOrmen!, t => Assert.Equal(DateTimeKind.Utc, t.CreadoEn.Kind));

        var ajeno = await lagomar.PostAsJsonAsync($"/api/tickets/{propio.Id}/comentarios", new SolicitudComentario("Hola", false, null));
        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
    }

    [Fact]
    public async Task Un_operador_trata_el_ticket_hasta_cerrarlo_y_reabrirlo()
    {
        var pando = await ClienteConSesion("mtechera");
        var operador = await ClienteConSesion("vpereira");
        var id = (await Crear(pando, Solicitud("Recorrido completo"))).Id;

        var sinPermiso = await pando.PostAsJsonAsync($"/api/tickets/{id}/asignar", new SolicitudAsignar("vpereira"));
        Assert.Equal(HttpStatusCode.Forbidden, sinPermiso.StatusCode);

        var bloqueoInvalido = await operador.PostAsJsonAsync($"/api/tickets/{id}/bloquear", new SolicitudBloqueo("Falta repuesto", false));
        Assert.Equal(HttpStatusCode.BadRequest, bloqueoInvalido.StatusCode);

        var tomado = await Accion(operador, $"/api/tickets/{id}/asignar", new SolicitudAsignar("vpereira"));
        Assert.Equal("EN_PROCESO", tomado.Estado);
        Assert.Equal("vpereira", tomado.Operador);

        var elevado = await Accion(operador, $"/api/tickets/{id}/elevar", new SolicitudElevar("gs-desarrollo", null, "Requiere un cambio"));
        Assert.Equal(new DestinoDto("grupo", "gs-desarrollo"), elevado.ElevadoA);

        var bloqueado = await Accion(operador, $"/api/tickets/{id}/bloquear", new SolicitudBloqueo("Falta repuesto", true));
        Assert.Equal("BLOQUEADO", bloqueado.Estado);

        var desbloqueado = await Accion(operador, $"/api/tickets/{id}/desbloquear", new SolicitudTexto(null));
        Assert.Equal("EN_PROCESO", desbloqueado.Estado);

        var cambio = await operador.PutAsJsonAsync($"/api/tickets/{id}/clasificacion", new SolicitudClasificacion("sis-online", null, "cri-media", "tp-software"));
        var reclasificado = await cambio.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal("sis-online", reclasificado?.Sistema);
        Assert.Contains("On-Line", reclasificado!.Actividad.Last().Detalle);

        var cerrado = await Accion(operador, $"/api/tickets/{id}/cerrar", new SolicitudCierre("Se cambió el cabezal.", "Ya quedó resuelto.", null, null, null));
        Assert.Equal("CERRADO", cerrado.Estado);
        Assert.Equal("Se cambió el cabezal.", cerrado.Solucion?.Texto);
        Assert.NotNull(cerrado.CerradoEn);

        var paraCliente = (await pando.GetFromJsonAsync<List<TicketDto>>("/api/tickets"))!.Single(t => t.Id == id);
        Assert.Null(paraCliente.Solucion);
        Assert.Contains(paraCliente.Actividad, a => a.Texto == "Ya quedó resuelto.");
        Assert.DoesNotContain(paraCliente.Actividad, a => a.Texto == "Se cambió el cabezal." || a.Texto == "Falta repuesto");

        var reabierto = await Accion(operador, $"/api/tickets/{id}/reabrir", new SolicitudTexto("Volvió a fallar"));
        Assert.Equal("EN_PROCESO", reabierto.Estado);
        Assert.Null(reabierto.CerradoEn);
    }

    [Fact]
    public async Task Un_operador_registra_un_ticket_ya_resuelto()
    {
        var operador = await ClienteConSesion("vpereira");

        var ticket = await Crear(operador, Solicitud("Resuelto en la llamada", localidadId: 7, registroDirecto: true));

        Assert.Equal("CERRADO", ticket.Estado);
        Assert.True(ticket.RegistroDirecto);
        Assert.True(ticket.Solucion?.EnDescripcion);
        Assert.Equal("vpereira", ticket.Operador);
    }

    [Fact]
    public async Task El_cierre_registra_la_solucion_aplicada()
    {
        var operador = await ClienteConSesion("vpereira");
        var id = (await Crear(operador, Solicitud("Con solución del catálogo", localidadId: 7))).Id;
        await Accion(operador, $"/api/tickets/{id}/asignar", new SolicitudAsignar("vpereira"));

        var cerrado = await Accion(operador, $"/api/tickets/{id}/cerrar", new SolicitudCierre("Se aplicó la solución.", null, null, "sol-term-impresora", null));

        Assert.Equal("sol-term-impresora", cerrado.Solucion?.SolucionCatalogoId);
    }

    [Fact]
    public async Task Los_borradores_son_de_quien_los_crea()
    {
        var pando = await ClienteConSesion("mtechera");
        var operador = await ClienteConSesion("vpereira");

        var alta = await pando.PostAsJsonAsync("/api/tickets/borradores", new SolicitudTicket("A medias", null, null, null, null, null, null, null, null, false, false, null, null, null));
        var borrador = await alta.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal("BORRADOR", borrador?.Estado);
        Assert.Null(borrador?.Numero);

        Assert.DoesNotContain((await operador.GetFromJsonAsync<List<TicketDto>>("/api/tickets"))!, t => t.Id == borrador!.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await operador.DeleteAsync($"/api/tickets/borradores/{borrador!.Id}")).StatusCode);

        var ticket = await Crear(pando, Solicitud("Desde el borrador", borradorId: borrador.Id));
        Assert.Equal(borrador.Id, ticket.Id);
        Assert.Equal("ABIERTO", ticket.Estado);
        Assert.Equal("borrador", ticket.Actividad.Single().Detalle);

        var otro = await pando.PostAsJsonAsync("/api/tickets/borradores", new SolicitudTicket("Para descartar", null, null, null, null, null, null, null, null, false, false, null, null, null));
        var descartable = await otro.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal(HttpStatusCode.NoContent, (await pando.DeleteAsync($"/api/tickets/borradores/{descartable!.Id}")).StatusCode);
    }

    [Fact]
    public async Task Listar_sin_token_devuelve_401()
    {
        var respuesta = await factory.CreateClient().GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static async Task<TicketDto> Crear(HttpClient cliente, SolicitudTicket solicitud) =>
        await Accion(cliente, "/api/tickets", solicitud);

    private static async Task<TicketDto> Accion<T>(HttpClient cliente, string ruta, T cuerpo)
    {
        var respuesta = await cliente.PostAsJsonAsync(ruta, cuerpo);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<TicketDto>())!;
    }

    private async Task<HttpClient> ClienteConSesion(string usuario)
    {
        var cliente = factory.CreateClient();
        var contrasena = factory.Services.GetRequiredService<IConfiguration>()["UsuariosIniciales:Contrasena"];
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/ingresar", new SolicitudIngreso(usuario, contrasena));
        var ingreso = await respuesta.Content.ReadFromJsonAsync<RespuestaIngreso>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ingreso!.Token);
        return cliente;
    }
}
