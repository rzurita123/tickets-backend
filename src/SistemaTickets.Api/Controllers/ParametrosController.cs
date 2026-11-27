using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Soluciones;

namespace SistemaTickets.Api.Controllers;

/// Pesos de la sugerencia de soluciones.
[ApiController]
[Route("api/parametros")]
[Authorize]
public class ParametrosController(ServicioParametros parametros, IUsuarioActual actual) : ControllerBase
{
    [HttpGet]
    public Task<Dictionary<string, int>> Obtener(CancellationToken cancelacion) => parametros.ObtenerAsync(cancelacion);

    [HttpPut]
    [Authorize(Policy = Politicas.EsAdministrador)]
    public async Task<Dictionary<string, int>> Guardar(Dictionary<string, int?> valores, CancellationToken cancelacion) =>
        await parametros.GuardarAsync(await actual.ObtenerAsync(cancelacion), valores, cancelacion);
}
