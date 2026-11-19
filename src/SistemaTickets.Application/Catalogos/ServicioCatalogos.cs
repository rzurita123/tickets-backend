using System.Globalization;
using System.Text;
using SistemaTickets.Domain.Catalogos;

namespace SistemaTickets.Application.Catalogos;

public class ErrorCatalogo(string mensaje) : Exception(mensaje);

public class ServicioCatalogos(IRepositorioCatalogos repositorio)
{
    private static readonly Dictionary<TipoCatalogo, string> Prefijos = new()
    {
        [TipoCatalogo.Sistemas] = "sis",
        [TipoCatalogo.Subsistemas] = "sub",
        [TipoCatalogo.TiposProblema] = "tp",
        [TipoCatalogo.Criticidades] = "cri",
        [TipoCatalogo.GruposSoporte] = "gs",
    };

    public Task<IReadOnlyList<ItemCatalogo>> ListarAsync(TipoCatalogo tipo, CancellationToken cancelacion = default) =>
        repositorio.ListarAsync(tipo, cancelacion);

    public async Task<ItemCatalogo> CrearAsync(TipoCatalogo tipo, string? nombre, string? codigoSistema, CancellationToken cancelacion = default)
    {
        var items = await repositorio.ListarAsync(tipo, cancelacion);
        var limpio = Limpiar(nombre);

        var sistemaId = 0;
        if (tipo == TipoCatalogo.Subsistemas)
        {
            var sistemas = await repositorio.ListarAsync(TipoCatalogo.Sistemas, cancelacion);
            sistemaId = sistemas.FirstOrDefault(s => s.Codigo == codigoSistema)?.Id
                ?? throw new ErrorCatalogo("Elegí el sistema.");
        }

        ExigirNombreLibre(items, limpio, sistemaId, null);

        var codigo = GenerarCodigo(Prefijos[tipo], limpio, items);
        ItemCatalogo item = tipo switch
        {
            TipoCatalogo.Sistemas => new Sistema { Codigo = codigo, Nombre = limpio },
            TipoCatalogo.Subsistemas => new Subsistema { Codigo = codigo, Nombre = limpio, SistemaId = sistemaId },
            TipoCatalogo.TiposProblema => new TipoProblema { Codigo = codigo, Nombre = limpio },
            TipoCatalogo.Criticidades => new Criticidad { Codigo = codigo, Nombre = limpio },
            _ => new GrupoSoporte { Codigo = codigo, Nombre = limpio },
        };

        await repositorio.AgregarAsync(tipo, item, cancelacion);
        await repositorio.GuardarAsync(cancelacion);
        return item;
    }

    public async Task<ItemCatalogo?> ModificarAsync(TipoCatalogo tipo, string codigo, string? nombre, bool? activo, CancellationToken cancelacion = default)
    {
        var items = await repositorio.ListarAsync(tipo, cancelacion);
        var item = items.FirstOrDefault(x => x.Codigo == codigo);
        if (item is null)
        {
            return null;
        }

        if (nombre is not null)
        {
            var limpio = Limpiar(nombre);
            ExigirNombreLibre(items, limpio, (item as Subsistema)?.SistemaId ?? 0, item);
            item.Nombre = limpio;
        }

        if (activo is bool valor)
        {
            item.Activo = valor;
        }

        await repositorio.GuardarAsync(cancelacion);
        return item;
    }

    private static string Limpiar(string? nombre)
    {
        var limpio = string.Join(' ', (nombre ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length > 0 ? limpio : throw new ErrorCatalogo("Escribí el nombre.");
    }

    private static void ExigirNombreLibre(IReadOnlyList<ItemCatalogo> items, string nombre, int sistemaId, ItemCatalogo? propio)
    {
        var clave = Normalizar(nombre);
        var repetido = items.Any(x =>
            x != propio
            && Normalizar(x.Nombre) == clave
            && (x is not Subsistema sub || sub.SistemaId == sistemaId));
        if (repetido)
        {
            throw new ErrorCatalogo("Ya existe un elemento con ese nombre.");
        }
    }

    private static string GenerarCodigo(string prefijo, string nombre, IReadOnlyList<ItemCatalogo> items)
    {
        var slug = Normalizar(nombre).Replace(' ', '-');
        var codigoBase = slug.Length > 0 ? $"{prefijo}-{slug}" : prefijo;
        var codigo = codigoBase;
        for (var n = 2; items.Any(x => x.Codigo == codigo); n++)
        {
            codigo = $"{codigoBase}-{n}";
        }

        return codigo;
    }

    private static string Normalizar(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(char.IsAsciiLetterOrDigit(c) ? c : ' ');
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
