using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Tickets;

namespace SistemaTickets.Application.Tickets;

/// Valida y traduce el filtro del front (códigos, nombres de usuario) a criterios con ids.
public class TraductorFiltroTickets(ICatalogoLectura catalogos, IRepositorioUsuarios usuarios, OpcionesListadoTickets opciones)
{
    private const string CriticidadAlta = "cri-alta";
    private const string CriticidadMedia = "cri-media";

    public async Task<CriteriosTickets> TraducirAsync(FiltroTickets f, CancellationToken cancelacion = default)
    {
        var sistemaId = await catalogos.IdAsync(TipoCatalogo.Sistemas, f.Sistema, cancelacion);
        if (!string.IsNullOrEmpty(f.Sistema) && sistemaId is null)
        {
            throw new ErrorTicket("El sistema no existe.");
        }

        var criticidadId = await catalogos.IdAsync(TipoCatalogo.Criticidades, f.Criticidad, cancelacion);
        if (!string.IsNullOrEmpty(f.Criticidad) && criticidadId is null)
        {
            throw new ErrorTicket("La criticidad no existe.");
        }

        var sinOperador = f.Atiende == "sin";
        int? operadorId = null;
        if (!sinOperador && !string.IsNullOrEmpty(f.Atiende))
        {
            var operador = await usuarios.BuscarPorNombreUsuarioAsync(f.Atiende, cancelacion)
                ?? throw new ErrorTicket("La persona que atiende no existe.");
            operadorId = operador.Id;
        }

        return new CriteriosTickets(
            EstadosDe(f.Estado),
            f.LocalidadId,
            sistemaId,
            criticidadId,
            operadorId,
            sinOperador,
            f.Texto,
            f.Orden ?? "recientes",
            await catalogos.IdAsync(TipoCatalogo.Criticidades, CriticidadAlta, cancelacion),
            await catalogos.IdAsync(TipoCatalogo.Criticidades, CriticidadMedia, cancelacion),
            Math.Max(1, f.Pagina),
            f.PorPagina <= 0 ? opciones.PorPaginaPorDefecto : Math.Min(f.PorPagina, opciones.PorPaginaMaximo));
    }

    private static IReadOnlyList<EstadoTicket>? EstadosDe(string? estado) => estado switch
    {
        null or "" or "todos" => null,
        "en-curso" => [EstadoTicket.Abierto, EstadoTicket.EnProceso, EstadoTicket.Bloqueado],
        "ABIERTO" => [EstadoTicket.Abierto],
        "EN_PROCESO" => [EstadoTicket.EnProceso],
        "BLOQUEADO" => [EstadoTicket.Bloqueado],
        "CERRADO" => [EstadoTicket.Cerrado],
        _ => throw new ErrorTicket("El estado no es válido."),
    };
}