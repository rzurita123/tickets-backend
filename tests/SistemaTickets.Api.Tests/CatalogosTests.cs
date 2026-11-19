using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class CatalogosTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Listar_devuelve_los_catalogos()
    {
        var cliente = await ClienteConSesion("mtechera");

        var catalogos = await cliente.GetFromJsonAsync<CatalogosDto>("/api/catalogos");

        Assert.NotNull(catalogos);
        Assert.Contains(catalogos.Sistemas, s => s.Codigo == "sis-caja" && s.Nombre == "Caja");
        Assert.Equal("sis-caja", catalogos.Subsistemas.Single(s => s.Codigo == "sub-caja-cierre").Sistema);
        Assert.Contains(catalogos.TiposProblema, t => t.Codigo == "tp-nodef");
        Assert.Equal(3, catalogos.Criticidades.Count());
        Assert.Equal(4, catalogos.GruposSoporte.Count());
    }

    [Fact]
    public async Task Listar_sin_token_devuelve_401()
    {
        var respuesta = await factory.CreateClient().GetAsync("/api/catalogos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_sin_ser_administrador_devuelve_403()
    {
        var cliente = await ClienteConSesion("vpereira");

        var respuesta = await cliente.PostAsJsonAsync("/api/catalogos/sistemas", new SolicitudItemCatalogo("Otro", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_administrador_crea_renombra_y_desactiva()
    {
        var cliente = await ClienteConSesion("scabrera");

        var alta = await cliente.PostAsJsonAsync("/api/catalogos/sistemas", new SolicitudItemCatalogo("Prueba Ágil", null, null));
        var creado = await alta.Content.ReadFromJsonAsync<ItemCatalogoDto>();
        Assert.Equal("sis-prueba-agil", creado?.Codigo);

        var repetido = await cliente.PostAsJsonAsync("/api/catalogos/sistemas", new SolicitudItemCatalogo("prueba agil", null, null));
        Assert.Equal(HttpStatusCode.BadRequest, repetido.StatusCode);

        var sub = await cliente.PostAsJsonAsync("/api/catalogos/subsistemas", new SolicitudItemCatalogo("Otros", "sis-prueba-agil", null));
        var subCreado = await sub.Content.ReadFromJsonAsync<ItemCatalogoDto>();
        Assert.Equal("sis-prueba-agil", subCreado?.Sistema);
        Assert.Equal("sub-otros", subCreado?.Codigo);

        var cambio = await cliente.PutAsJsonAsync("/api/catalogos/sistemas/sis-prueba-agil", new SolicitudItemCatalogo("Prueba 2", null, false));
        var cambiado = await cambio.Content.ReadFromJsonAsync<ItemCatalogoDto>();
        Assert.Equal("Prueba 2", cambiado?.Nombre);
        Assert.False(cambiado?.Activo);

        var inexistente = await cliente.PutAsJsonAsync("/api/catalogos/sistemas/no-existe", new SolicitudItemCatalogo("X", null, null));
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
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
