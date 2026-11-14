using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SistemaTickets.Api.Tests;

public class InicioTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Api_Responde()
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/html", respuesta.Content.Headers.ContentType?.MediaType);
    }
}
