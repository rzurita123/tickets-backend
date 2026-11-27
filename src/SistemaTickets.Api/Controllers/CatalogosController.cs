using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Controllers;

public record ItemCatalogoDto(string Codigo, string Nombre, bool Activo, string? Sistema = null);

public record CatalogosDto(
    IEnumerable<ItemCatalogoDto> Sistemas,
    IEnumerable<ItemCatalogoDto> Subsistemas,
    IEnumerable<ItemCatalogoDto> TiposProblema,
    IEnumerable<ItemCatalogoDto> Criticidades,
    IEnumerable<ItemCatalogoDto> GruposSoporte);

public record SolicitudItemCatalogo(string? Nombre, string? Sistema, bool? Activo);

[ApiController]
[Route("api/catalogos")]
[Authorize]
public class CatalogosController(ServicioCatalogos catalogos) : ControllerBase
{
    private const string SoloAdministrador = nameof(Rol.Administrador);

    [HttpGet]
    public async Task<CatalogosDto> Listar(CancellationToken cancelacion)
    {
        var sistemas = await catalogos.ListarAsync(TipoCatalogo.Sistemas, cancelacion);

        async Task<IEnumerable<ItemCatalogoDto>> Items(TipoCatalogo tipo) =>
            (await catalogos.ListarAsync(tipo, cancelacion)).Select(x => Dto(x, sistemas));

        return new CatalogosDto(
            sistemas.Select(x => Dto(x, sistemas)),
            await Items(TipoCatalogo.Subsistemas),
            await Items(TipoCatalogo.TiposProblema),
            await Items(TipoCatalogo.Criticidades),
            await Items(TipoCatalogo.GruposSoporte));
    }

    [HttpPost("{tipo}")]
    [Authorize(Roles = SoloAdministrador)]
    public async Task<ActionResult<ItemCatalogoDto>> Crear(TipoCatalogo tipo, SolicitudItemCatalogo solicitud, CancellationToken cancelacion)
    {
        try
        {
            var item = await catalogos.CrearAsync(tipo, solicitud.Nombre, solicitud.Sistema, User.IdUsuario(), cancelacion);
            return await Dto(item, cancelacion);
        }
        catch (ErrorCatalogo e)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: e.Message);
        }
    }

    [HttpPut("{tipo}/{codigo}")]
    [Authorize(Roles = SoloAdministrador)]
    public async Task<ActionResult<ItemCatalogoDto>> Modificar(TipoCatalogo tipo, string codigo, SolicitudItemCatalogo solicitud, CancellationToken cancelacion)
    {
        try
        {
            var item = await catalogos.ModificarAsync(tipo, codigo, solicitud.Nombre, solicitud.Activo, User.IdUsuario(), cancelacion);
            return item is null ? NotFound() : await Dto(item, cancelacion);
        }
        catch (ErrorCatalogo e)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: e.Message);
        }
    }

    private async Task<ItemCatalogoDto> Dto(ItemCatalogo item, CancellationToken cancelacion) =>
        Dto(item, await catalogos.ListarAsync(TipoCatalogo.Sistemas, cancelacion));

    private static ItemCatalogoDto Dto(ItemCatalogo item, IReadOnlyList<ItemCatalogo> sistemas) =>
        new(item.Codigo, item.Nombre, item.Activo,
            item is Subsistema sub ? sistemas.FirstOrDefault(s => s.Id == sub.SistemaId)?.Codigo : null);
}
