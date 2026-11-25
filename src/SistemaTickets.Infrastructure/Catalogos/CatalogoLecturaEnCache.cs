using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using SistemaTickets.Application.Catalogos;

namespace SistemaTickets.Infrastructure.Catalogos;

/// Singleton: guarda sólo diccionarios de código/id (inmutables), nunca entidades con tracking.
/// Como no puede inyectar el repositorio scoped, abre su propio scope para cargar.
public sealed class CatalogoLecturaEnCache(IServiceScopeFactory ambitos, IMemoryCache cache) : ICatalogoLectura
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);

    public async Task<int?> IdAsync(TipoCatalogo tipo, string? codigo, CancellationToken cancelacion = default)
    {
        if (string.IsNullOrEmpty(codigo))
        {
            return null;
        }

        if ((await CargarAsync(tipo, cancelacion)).PorCodigo.TryGetValue(codigo, out var id))
        {
            return id;
        }

        Invalidar(tipo);   // puede ser un item recién creado por el administrador
        return (await CargarAsync(tipo, cancelacion)).PorCodigo.TryGetValue(codigo, out id) ? id : null;
    }

    public async Task<string?> CodigoAsync(TipoCatalogo tipo, int? id, CancellationToken cancelacion = default)
    {
        if (id is not int valor)
        {
            return null;
        }

        if ((await CargarAsync(tipo, cancelacion)).PorId.TryGetValue(valor, out var codigo))
        {
            return codigo;
        }

        Invalidar(tipo);
        return (await CargarAsync(tipo, cancelacion)).PorId.GetValueOrDefault(valor);
    }

    public void Invalidar(TipoCatalogo tipo) => cache.Remove(Clave(tipo));

    private static string Clave(TipoCatalogo tipo) => $"catalogo-lectura:{tipo}";

    private async Task<Instantanea> CargarAsync(TipoCatalogo tipo, CancellationToken cancelacion) =>
        (await cache.GetOrCreateAsync(Clave(tipo), async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = Vigencia;
            await using var ambito = ambitos.CreateAsyncScope();
            var items = await ambito.ServiceProvider.GetRequiredService<IRepositorioCatalogos>().ListarAsync(tipo, cancelacion);
            return new Instantanea(items.ToDictionary(x => x.Codigo, x => x.Id), items.ToDictionary(x => x.Id, x => x.Codigo));
        }))!;

    private sealed record Instantanea(Dictionary<string, int> PorCodigo, Dictionary<int, string> PorId);
}