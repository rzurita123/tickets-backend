using Microsoft.AspNetCore.Mvc;

namespace SistemaTickets.Api.Controllers;

[ApiController]
[Route("api/estado")]
public class EstadoController(IWebHostEnvironment entorno) : ControllerBase
{
    [HttpGet]
    public IActionResult Obtener() => Ok(new
    {
        estado = "ok",
        entorno = entorno.EnvironmentName,
        fecha = DateTime.UtcNow,
    });
}
