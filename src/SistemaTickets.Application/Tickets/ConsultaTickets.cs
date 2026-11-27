using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Tickets;

/// Lo que manda el frontend en la query string.
public record FiltroTickets(
    string? Estado,
    int? LocalidadId,
    string? Sistema,
    string? Criticidad,
    string? Atiende,
    string? Texto,
    string? Orden,
    int Pagina = 1,
    int PorPagina = 15,
    DateTimeOffset? Desde = null,
    DateTimeOffset? Hasta = null);

/// El mismo filtro, ya validado y traducido a ids para la base.
public record CriteriosTickets(
    IReadOnlyList<EstadoTicket>? Estados,
    int? LocalidadId,
    int? SistemaId,
    int? CriticidadId,
    int? OperadorId,
    bool SinOperador,
    string? Texto,
    string Orden,
    int? CriticidadAltaId,
    int? CriticidadMediaId,
    int Pagina,
    int PorPagina,
    DateTime? Desde = null,
    DateTime? Hasta = null);

public record PaginaTickets(IReadOnlyList<Ticket> Items, int Total, int Pagina, int PorPagina);

/// Configuración inmutable; se registra como singleton (sección "ListadoTickets" de appsettings).
public class OpcionesListadoTickets
{
    public int PorPaginaPorDefecto { get; set; } = 15;

    public int PorPaginaMaximo { get; set; } = 100;
}

/// Puerto de lectura: la implementación vive en Infrastructure.
public interface IConsultaTickets
{
    Task<PaginaTickets> ListarPaginaAsync(Usuario usuario, CriteriosTickets criterios, CancellationToken cancelacion = default);

    Task<IReadOnlyDictionary<EstadoTicket, int>> ContarPorEstadoAsync(Usuario usuario, CriteriosTickets criterios, CancellationToken cancelacion = default);
}