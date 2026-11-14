using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SistemaTickets.Api.Tests;

public class EstadoTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task El_endpoint_de_estado_responde_ok()
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.GetAsync("/api/estado");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("ok", json.RootElement.GetProperty("estado").GetString());
    }
}
