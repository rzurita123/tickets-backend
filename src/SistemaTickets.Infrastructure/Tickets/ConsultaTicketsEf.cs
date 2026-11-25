using Microsoft.EntityFrameworkCore;

using SistemaTickets.Application.Tickets;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Tickets;

/// Lado de lectura de tickets: listados paginados y conteos sin cargar el agregado completo.
public class ConsultaTicketsEf(ContextoTickets contexto) : IConsultaTickets
{
    public async Task<PaginaTickets> ListarPaginaAsync(Usuario usuario, CriteriosTickets c, CancellationToken cancelacion = default)
    {
        var q = Filtrar(Visibles(usuario), usuario, c);
        if (c.Estados is { Count: > 0 } lista)
        {
            var estados = lista.ToArray();
            q = q.Where(t => estados.Contains(t.Estado));
        }

        var total = await q.CountAsync(cancelacion);
        var items = await Ordenar(q, c)
            .Skip((c.Pagina - 1) * c.PorPagina)
            .Take(c.PorPagina)
            .ToListAsync(cancelacion);
        return new PaginaTickets(items, total, c.Pagina, c.PorPagina);
    }

    public async Task<IReadOnlyDictionary<EstadoTicket, int>> ContarPorEstadoAsync(Usuario usuario, CriteriosTickets c, CancellationToken cancelacion = default)
    {
        var filas = await Filtrar(Visibles(usuario), usuario, c)
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync(cancelacion);
        return filas.ToDictionary(f => f.Estado, f => f.Cantidad);
    }

    /// Seguridad (RNF2): se aplica siempre primero, sin importar los filtros que mande el front.
    private IQueryable<Ticket> Visibles(Usuario usuario) =>
        contexto.Tickets.AsNoTracking()
            .Where(t => t.Estado != EstadoTicket.Borrador)
            .Where(VisibilidadTickets.Para(usuario));

    private static IQueryable<Ticket> Filtrar(IQueryable<Ticket> q, Usuario usuario, CriteriosTickets c)
    {
        if (usuario.EsOrmen && c.LocalidadId is int localidadId)
        {
            q = q.Where(t => t.LocalidadId == localidadId);
        }

        if (c.SistemaId is int sistemaId)
        {
            q = q.Where(t => t.SistemaId == sistemaId);
        }

        if (c.CriticidadId is int criticidadId)
        {
            q = q.Where(t => t.CriticidadId == criticidadId);
        }

        if (usuario.EsOrmen)
        {
            if (c.SinOperador)
            {
                q = q.Where(t => t.OperadorId == null);
            }
            else if (c.OperadorId is int operadorId)
            {
                q = q.Where(t => t.OperadorId == operadorId);
            }
        }

        if (!string.IsNullOrWhiteSpace(c.Texto))
        {
            var texto = c.Texto.Trim().TrimStart('#');
            q = texto.Length > 0 && texto.All(char.IsDigit)
                ? q.Where(t => t.Numero != null && EF.Functions.Like(t.Numero.Value.ToString(), texto + "%"))
                : q.Where(t => t.Titulo.Contains(texto) || t.Descripcion.Contains(texto));
        }

        return q;
    }

    private static IQueryable<Ticket> Ordenar(IQueryable<Ticket> q, CriteriosTickets c) => c.Orden switch
    {
        "antiguos" => q.OrderBy(t => t.CreadoEn).ThenBy(t => t.Id),
        "actualizados" => q.OrderByDescending(t => t.ActualizadoEn).ThenByDescending(t => t.Id),
        "criticidad" => q
            .OrderBy(t => t.CriticidadId == c.CriticidadAltaId ? 1 : t.CriticidadId == c.CriticidadMediaId ? 2 : 3)
            .ThenBy(t => t.CreadoEn)
            .ThenBy(t => t.Id),
        _ => q.OrderByDescending(t => t.CreadoEn).ThenByDescending(t => t.Id),
    };
}