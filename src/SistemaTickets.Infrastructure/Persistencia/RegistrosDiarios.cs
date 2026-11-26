using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Infrastructure.Persistencia;

internal static class RegistrosDiarios
{
    private const string PrefijoReferencia = "Registro diario de ejemplo #";

    private sealed record Registro(
        DateTime Fecha, string Localidad, string Sistema, string Subsistema, string Titulo,
        string Descripcion, string Referencia, string? Autor, string Comentario, bool Cerrado);

    public static Task CargarAsync(ContextoTickets contexto) =>
        contexto.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // Un intento anterior puede haber dejado entidades de la transacción revertida en memoria.
            contexto.ChangeTracker.Clear();
            await CargarEnTransaccionAsync(contexto);
        });

    private static async Task CargarEnTransaccionAsync(ContextoTickets contexto)
    {
        var operador = await contexto.Usuarios
            .Where(u => u.Rol == Rol.Operador)
            .OrderByDescending(u => u.NombreUsuario == "vpereira")
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync();
        if (operador is null)
        {
            return;
        }

        using var recurso = typeof(RegistrosDiarios).Assembly.GetManifestResourceStream(
            "SistemaTickets.Infrastructure.Persistencia.RegistrosDiarios.json")
            ?? throw new InvalidOperationException("Faltan los registros diarios de ejemplo.");
        var registros = JsonSerializer.Deserialize<Registro[]>(recurso)!;
        await using var transaccion = contexto.Database.IsRelational()
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var existentes = await contexto.Set<Actividad>()
            .Where(a => a.Tipo == TipoActividad.Comentario && a.Privada &&
                a.Texto != null && a.Texto.StartsWith(PrefijoReferencia))
            .Select(a => a.Texto!)
            .ToListAsync();
        var referencias = existentes.Select(texto => texto.Split('\n', 2)[0]).ToHashSet();
        // La posición es la referencia del registro. Agregar nuevos ejemplos al final del JSON.
        var pendientes = registros.Select((registro, indice) => (Registro: registro, Referencia: $"{PrefijoReferencia}{indice + 1:D3}"))
            .Where(p => !referencias.Contains(p.Referencia)).ToArray();
        if (pendientes.Length == 0)
        {
            return;
        }

        var localidades = await contexto.Localidades.ToDictionaryAsync(l => l.Nombre, StringComparer.OrdinalIgnoreCase);
        var sistemas = (await contexto.Sistemas.OrderBy(s => s.Id).ToListAsync())
            .GroupBy(s => s.Nombre, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var nombre in pendientes.Select(p => p.Registro.Localidad).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!localidades.ContainsKey(nombre))
            {
                localidades.Add(nombre, contexto.Localidades.Add(new Localidad { Nombre = nombre }).Entity);
            }
        }
        foreach (var nombre in pendientes.Select(p => p.Registro.Sistema).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!sistemas.ContainsKey(nombre))
            {
                sistemas.Add(nombre, contexto.Sistemas.Add(new Sistema { Codigo = "sis-reg-" + Codigo(nombre), Nombre = nombre }).Entity);
            }
        }
        await contexto.SaveChangesAsync();

        var subsistemas = (await contexto.Subsistemas.OrderBy(s => s.Id).ToListAsync())
            .ToLookup(s => s.SistemaId);
        var porSistema = pendientes.Select(p => p.Registro).GroupBy(r => sistemas[r.Sistema].Id);
        var subsistemasPorSistema = new Dictionary<int, Dictionary<string, Subsistema>>();
        foreach (var grupo in porSistema)
        {
            var catalogo = subsistemas[grupo.Key]
                .GroupBy(s => s.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            subsistemasPorSistema.Add(grupo.Key, catalogo);
            foreach (var nombre in grupo.Select(r => r.Subsistema).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!catalogo.ContainsKey(nombre))
                {
                    var subsistema = new Subsistema
                    {
                        SistemaId = grupo.Key,
                        Codigo = $"sub-reg-{Codigo(grupo.First().Sistema)}-{Codigo(nombre)}",
                        Nombre = nombre,
                    };
                    catalogo.Add(nombre, contexto.Subsistemas.Add(subsistema).Entity);
                }
            }
        }
        await contexto.SaveChangesAsync();

        var criticidad = await contexto.Criticidades.SingleAsync(c => c.Codigo == "cri-media");
        var tipo = await contexto.TiposProblema.SingleAsync(t => t.Codigo == "tp-nodef");
        var numero = (await contexto.Tickets.MaxAsync(t => t.Numero) ?? 1000) + 1;
        foreach (var (registro, referencia) in pendientes)
        {
            var sistema = sistemas[registro.Sistema];
            var ticket = Ticket.NuevoBorrador(operador.Id, registro.Fecha);
            ticket.Titulo = registro.Titulo;
            ticket.Descripcion = $"Sede original: {registro.Referencia}\n\n{registro.Descripcion}";
            ticket.LocalidadId = localidades[registro.Localidad].Id;
            ticket.SistemaId = sistema.Id;
            ticket.SubsistemaId = subsistemasPorSistema[sistema.Id][registro.Subsistema].Id;
            ticket.CriticidadId = criticidad.Id;
            ticket.TipoProblemaId = tipo.Id;
            var autor = $"{referencia}\nAutor del registro original: {registro.Autor ?? "Sin identificar"}";

            if (registro.Cerrado)
            {
                ticket.RegistrarResuelto(numero++, true, false, operador.Id, registro.Fecha, registro.Comentario, null, []);
                ticket.Comentar(operador.Id, registro.Fecha, autor, true);
            }
            else
            {
                ticket.Registrar(numero++, true, false, operador.Id, registro.Fecha);
                ticket.Comentar(operador.Id, registro.Fecha, $"{autor}\n\n{registro.Comentario}".TrimEnd(), true);
            }

            contexto.Tickets.Add(ticket);
        }
        await contexto.SaveChangesAsync();
        if (transaccion is not null)
        {
            await transaccion.CommitAsync();
        }
    }

    private static string Codigo(string nombre) =>
        string.Join('-', new string(nombre.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsAsciiLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ')
            .ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
