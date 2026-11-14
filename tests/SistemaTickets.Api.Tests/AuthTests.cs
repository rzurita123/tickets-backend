using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class AuthTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private string Contrasena =>
        factory.Services.GetRequiredService<IConfiguration>()["UsuariosIniciales:Contrasena"]!;

    [Fact]
    public async Task Ingresar_devuelve_un_token_que_permite_consultar_el_usuario()
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/ingresar", new SolicitudIngreso("vpereira", Contrasena));
        var ingreso = await respuesta.Content.ReadFromJsonAsync<RespuestaIngreso>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(ingreso);
        Assert.Equal("OPERADOR", ingreso.Usuario.Rol);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ingreso.Token);
        var yo = await cliente.GetFromJsonAsync<UsuarioDto>("/api/auth/yo");

        Assert.Equal("vpereira", yo?.Usuario);
    }

    [Fact]
    public async Task Ingresar_con_contrasena_incorrecta_devuelve_401()
    {
        var respuesta = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/ingresar", new SolicitudIngreso("vpereira", Contrasena + "x"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Ingresar_sin_datos_devuelve_400()
    {
        var respuesta = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/ingresar", new SolicitudIngreso("", null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Consultar_el_usuario_sin_token_devuelve_401()
    {
        var respuesta = await factory.CreateClient().GetAsync("/api/auth/yo");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
