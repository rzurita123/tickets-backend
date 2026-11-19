using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Catalogos;

public class RepositorioCatalogos(ContextoTickets contexto) : IRepositorioCatalogos
{
    public async Task<IReadOnlyList<ItemCatalogo>> ListarAsync(TipoCatalogo tipo, CancellationToken cancelacion = default) => tipo switch
    {
        TipoCatalogo.Sistemas => await Listar<Sistema>(cancelacion),
        TipoCatalogo.Subsistemas => await Listar<Subsistema>(cancelacion),
        TipoCatalogo.TiposProblema => await Listar<TipoProblema>(cancelacion),
        TipoCatalogo.Criticidades => await Listar<Criticidad>(cancelacion),
        _ => await Listar<GrupoSoporte>(cancelacion),
    };

    public Task AgregarAsync(TipoCatalogo tipo, ItemCatalogo item, CancellationToken cancelacion = default)
    {
        contexto.Add(item);
        return Task.CompletedTask;
    }

    public Task GuardarAsync(CancellationToken cancelacion = default) => contexto.SaveChangesAsync(cancelacion);

    private async Task<IReadOnlyList<ItemCatalogo>> Listar<T>(CancellationToken cancelacion)
        where T : ItemCatalogo =>
        await contexto.Set<T>().OrderBy(x => x.Id).ToListAsync(cancelacion);
}
