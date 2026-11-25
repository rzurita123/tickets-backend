namespace SistemaTickets.Application.Catalogos;

/// Traduce código <-> id de catálogo sin cargar entidades. Sólo lectura: la gestión sigue en IRepositorioCatalogos.
public interface ICatalogoLectura
{
    Task<int?> IdAsync(TipoCatalogo tipo, string? codigo, CancellationToken cancelacion = default);

    Task<string?> CodigoAsync(TipoCatalogo tipo, int? id, CancellationToken cancelacion = default);

    void Invalidar(TipoCatalogo tipo);
}