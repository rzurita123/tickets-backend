using System.Net;
using System.Net.Http.Json;

namespace SistemaTickets.Api.Tests;

public class ParametrosTests(FabricaApi factory) : IClassFixture<FabricaApi>
{
    private static Dictionary<string, int?> Valores(int maxResultados) => new()
    {
        ["pesoSistema"] = 3, ["pesoSubsistema"] = 2, ["pesoPalabraClave"] = 2,
        ["pesoLocalidad"] = 1, ["pesoPalabraComun"] = 1, ["maxResultados"] = maxResultados,
    };

    [Fact]
    public async Task Cualquier_usuario_lee_los_parametros()
    {
        var cliente = await factory.ClienteConSesion("mtechera");

        var parametros = await cliente.GetAsync("/api/parametros").Ok<Dictionary<string, int>>();

        Assert.Equal(6, parametros.Count);
        Assert.Equal(3, parametros["pesoSistema"]);
    }

    [Fact]
    public async Task Solo_un_administrador_los_cambia()
    {
        var operador = await factory.ClienteConSesion("vpereira");

        Assert.Equal(HttpStatusCode.Forbidden, (await operador.PutAsJsonAsync("/api/parametros", Valores(6))).StatusCode);
    }

    [Fact]
    public async Task Fuera_de_rango_devuelve_400_con_el_campo()
    {
        var admin = await factory.ClienteConSesion("scabrera");

        var respuesta = await admin.PutAsJsonAsync("/api/parametros", Valores(40));

        Assert.Contains("maxResultados", (await respuesta.ErroresAsync()).Keys);
    }

    [Fact]
    public async Task Un_administrador_los_cambia()
    {
        var admin = await factory.ClienteConSesion("ralvarez");

        var guardados = await admin.PutAsJsonAsync("/api/parametros", Valores(9)).Ok<Dictionary<string, int>>();
        var leidos = await admin.GetAsync("/api/parametros").Ok<Dictionary<string, int>>();

        Assert.Equal(9, guardados["maxResultados"]);
        Assert.Equal(9, leidos["maxResultados"]);
    }
}
