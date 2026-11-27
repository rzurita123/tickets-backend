using System.Net;
using System.Net.Http.Json;
using SistemaTickets.Api.Controllers;

namespace SistemaTickets.Api.Tests;

public class FavoritosTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Un_operador_marca_y_desmarca_tickets_y_soluciones()
    {
        var operador = await factory.ClienteConSesion("vpereira");
        var ticket = await (await factory.ClienteConSesion("mtechera")).CrearTicket("Para favoritos");

        Assert.Equal(HttpStatusCode.NoContent, (await operador.PutAsync($"/api/favoritos/tickets/{ticket.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await operador.PutAsync($"/api/favoritos/tickets/{ticket.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await operador.PutAsync("/api/favoritos/soluciones/sol-term-impresora", null)).StatusCode);
        var marcados = await operador.GetAsync("/api/favoritos").Ok<List<FavoritoDto>>();
        Assert.Equal(HttpStatusCode.NoContent, (await operador.DeleteAsync($"/api/favoritos/tickets/{ticket.Id}")).StatusCode);
        var quedan = await operador.GetAsync("/api/favoritos").Ok<List<FavoritoDto>>();

        Assert.Equal(2, marcados.Count);
        Assert.Contains(marcados, f => f.Tipo == "ticket" && f.TicketId == ticket.Id);
        Assert.Contains(marcados, f => f.Tipo == "solucion" && f.Solucion == "sol-term-impresora");
        Assert.Equal("solucion", Assert.Single(quedan).Tipo);
    }

    [Fact]
    public async Task Los_favoritos_son_de_cada_persona()
    {
        var vpereira = await factory.ClienteConSesion("vpereira");
        var nacosta = await factory.ClienteConSesion("nacosta");
        await vpereira.PutAsync("/api/favoritos/soluciones/sol-term-impresora", null);

        var ajenos = await nacosta.GetAsync("/api/favoritos").Ok<List<FavoritoDto>>();

        Assert.DoesNotContain(ajenos, f => f.Solucion == "sol-term-impresora");
    }

    [Fact]
    public async Task Lo_que_no_existe_devuelve_404()
    {
        var operador = await factory.ClienteConSesion("vpereira");

        Assert.Equal(HttpStatusCode.NotFound, (await operador.PutAsync("/api/favoritos/tickets/999999", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await operador.PutAsync("/api/favoritos/soluciones/no-existe", null)).StatusCode);
    }

    [Fact]
    public async Task Un_cliente_no_tiene_favoritos()
    {
        var cliente = await factory.ClienteConSesion("mtechera");

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/favoritos")).StatusCode);
    }
}
