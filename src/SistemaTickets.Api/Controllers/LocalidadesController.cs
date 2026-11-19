using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Application.Localidades;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Controllers;

public record LocalidadDto(int Id, string Nombre, bool Activa)
{
    public static LocalidadDto Desde(Localidad l) => new(l.Id, l.Nombre, l.Activa);
}

[ApiController]
[Route("api/localidades")]
[Authorize]
public class LocalidadesController(IRepositorioLocalidades localidades) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<LocalidadDto>> Listar(CancellationToken cancelacion) =>
        (await localidades.ListarAsync(cancelacion)).Select(LocalidadDto.Desde);
}
