using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class UsuariosTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Listar_con_token_devuelve_los_usuarios()
    {
        var cliente = await ClienteConSesion();

        var usuarios = await cliente.GetFromJsonAsync<List<UsuarioDto>>("/api/usuarios");
        var localidades = await cliente.GetFromJsonAsync<List<LocalidadDto>>("/api/localidades");

        Assert.Equal(6, usuarios?.Count);
        Assert.Equal(11, localidades?.Count);
        var localidadId = usuarios?.Single(u => u.Usuario == "mtechera").LocalidadId;
        Assert.Equal("Pando", localidades?.Single(l => l.Id == localidadId).Nombre);
    }

    [Theory]
    [InlineData("/api/usuarios")]
    [InlineData("/api/localidades")]
    public async Task Listar_sin_token_devuelve_401(string ruta)
    {
        var respuesta = await factory.CreateClient().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private async Task<HttpClient> ClienteConSesion()
    {
        var cliente = factory.CreateClient();
        var contrasena = factory.Services.GetRequiredService<IConfiguration>()["UsuariosIniciales:Contrasena"];
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/ingresar", new SolicitudIngreso("mtechera", contrasena));
        var ingreso = await respuesta.Content.ReadFromJsonAsync<RespuestaIngreso>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ingreso!.Token);
        return cliente;
    }
}
