using SistemaTickets.Domain.Catalogos;

namespace SistemaTickets.Application.Catalogos;

public enum TipoCatalogo
{
    Sistemas,
    Subsistemas,
    TiposProblema,
    Criticidades,
    GruposSoporte,
}

public interface IRepositorioCatalogos
{
    Task<IReadOnlyList<ItemCatalogo>> ListarAsync(TipoCatalogo tipo, CancellationToken cancelacion = default);

    Task AgregarAsync(TipoCatalogo tipo, ItemCatalogo item, CancellationToken cancelacion = default);

    Task GuardarAsync(CancellationToken cancelacion = default);
}
