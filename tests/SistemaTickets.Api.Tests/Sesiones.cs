using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public static class Sesiones
{
    public const string Imagen = "data:image/png;base64,iVBORw0KGgo=";

    public static async Task<HttpClient> ClienteConSesion(this FabricaApi factory, string usuario)
    {
        var cliente = factory.CreateClient();
        var contrasena = factory.Services.GetRequiredService<IConfiguration>()["UsuariosIniciales:Contrasena"];
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/ingresar", new SolicitudIngreso(usuario, contrasena));
        var ingreso = await respuesta.Content.ReadFromJsonAsync<RespuestaIngreso>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ingreso!.Token);
        return cliente;
    }

    public static SolicitudTicket Ticket(string titulo, int? localidadId = null) =>
        new(titulo, "No imprime los comprobantes.", localidadId, "sis-caja", "sub-caja-cierre", "cri-alta", null,
            [new ImagenSolicitud("img-1", "foto.png", Imagen)], null, false, false, null, null, null);

    public static async Task<T> Ok<T>(this Task<HttpResponseMessage> pedido)
    {
        var respuesta = await pedido;
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<TicketDto> CrearTicket(this HttpClient cliente, string titulo, int? localidadId = null) =>
        await cliente.PostAsJsonAsync("/api/tickets", Ticket(titulo, localidadId)).Ok<TicketDto>();

    /// Errores por campo de una respuesta 400.
    public static async Task<Dictionary<string, string[]>> ErroresAsync(this HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<Problema>();
        return problema?.Errors ?? [];
    }

    public sealed record Problema(string? Title, Dictionary<string, string[]>? Errors);
}
