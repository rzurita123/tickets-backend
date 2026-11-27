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

    [Fact]
    public async Task Un_cliente_ve_su_localidad_y_al_personal_de_ormen_sin_datos_de_contacto()
    {
        var cliente = await ClienteConSesion("mtechera");

        var lista = (await cliente.GetFromJsonAsync<List<UsuarioDto>>("/api/usuarios"))!;

        var yo = lista.Single(u => u.Usuario == "mtechera");
        Assert.All(lista.Where(u => u.Rol == "CLIENTE"), u => Assert.Equal(yo.LocalidadId, u.LocalidadId));
        Assert.Contains(lista, u => u.Rol != "CLIENTE");
        Assert.NotEqual(string.Empty, yo.Email);
        Assert.All(lista.Where(u => u.Usuario != "mtechera"), u => Assert.Equal(string.Empty, u.Email));
    }

    [Fact]
    public async Task Un_operador_filtra_usuarios_por_rol_y_localidad()
    {
        var operador = await ClienteConSesion();

        var clientes = await operador.GetFromJsonAsync<List<UsuarioDto>>("/api/usuarios?rol=cliente");
        Assert.All(clientes!, u => Assert.Equal("CLIENTE", u.Rol));

        var localidadId = clientes!.Single(u => u.Usuario == "mtechera").LocalidadId;
        var deLaLocalidad = await operador.GetFromJsonAsync<List<UsuarioDto>>($"/api/usuarios?localidadId={localidadId}");
        Assert.All(deLaLocalidad!, u => Assert.Equal(localidadId, u.LocalidadId));
    }

    private async Task<HttpClient> ClienteConSesion(string usuario = "vpereira")
    {
        var cliente = factory.CreateClient();
        var contrasena = factory.Services.GetRequiredService<IConfiguration>()["UsuariosIniciales:Contrasena"];
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/ingresar", new SolicitudIngreso(usuario, contrasena));
        var ingreso = await respuesta.Content.ReadFromJsonAsync<RespuestaIngreso>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ingreso!.Token);
        return cliente;
    }
}
