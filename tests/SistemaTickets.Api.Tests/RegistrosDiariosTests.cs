using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Infrastructure.Persistencia;
using SistemaTickets.Infrastructure.Seguridad;

namespace SistemaTickets.Api.Tests;

public class RegistrosDiariosTests
{
    private const string PrefijoReferencia = "Registro diario de ejemplo #";

    [Fact]
    public async Task Carga_los_registros_desde_cero_y_conserva_los_mismos_tickets_al_repetirla()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        await using var servicios = new ServiceCollection()
            .AddDbContext<ContextoTickets>(opciones => opciones.UseSqlite(conexion))
            .AddSingleton<IHashContrasenas, HashContrasenasPbkdf2>()
            .BuildServiceProvider();

        await servicios.PrepararBaseAsync("contrasena-de-pruebas", cargarRegistrosDiarios: true);

        var (primeraCarga, totalInicial) = await LeerCargaAsync(servicios);
        Assert.Equal(253, primeraCarga.Length);
        Assert.Equal(244, primeraCarga.Count(r => r.Estado == EstadoTicket.Abierto));
        Assert.Equal(9, primeraCarga.Count(r => r.Estado == EstadoTicket.Cerrado));
        Assert.Equal(253, primeraCarga.Select(r => r.Id).Distinct().Count());
        Assert.Equal(253, primeraCarga.Select(r => r.Numero).Distinct().Count());
        Assert.All(primeraCarga, r => Assert.True(r.Numero > 0));
        Assert.Equal(
            Enumerable.Range(1, 253).Select(i => $"{PrefijoReferencia}{i:D3}"),
            primeraCarga.Select(r => r.Referencia));

        await using (var ambito = servicios.CreateAsyncScope())
        {
            var contexto = ambito.ServiceProvider.GetRequiredService<ContextoTickets>();
            var id = primeraCarga[2].Id;
            var ticket = await contexto.Tickets.AsNoTracking().SingleAsync(t => t.Id == id);
            var fecha = new DateTime(2026, 11, 9, 12, 24, 0, DateTimeKind.Utc);

            Assert.True(ticket.Privado);
            Assert.Equal(fecha, ticket.CreadoEn);
            Assert.Equal(fecha, ticket.CerradoEn);
            Assert.Equal(
                "Entro corro el updater y reviso si el worker se inicio, se inicio sin problemas. Solucionado.",
                ticket.Resolucion);
            Assert.Equal("Lagomar", await contexto.Localidades.Where(l => l.Id == ticket.LocalidadId).Select(l => l.Nombre).SingleAsync());
            Assert.Equal("Updater", await contexto.Sistemas.Where(s => s.Id == ticket.SistemaId).Select(s => s.Nombre).SingleAsync());
            Assert.Equal("Otros", await contexto.Subsistemas
                .Where(s => s.Id == ticket.SubsistemaId && s.SistemaId == ticket.SistemaId)
                .Select(s => s.Nombre).SingleAsync());
        }

        await servicios.PrepararBaseAsync("contrasena-de-pruebas", cargarRegistrosDiarios: true);

        var (segundaCarga, totalFinal) = await LeerCargaAsync(servicios);
        Assert.Equal(totalInicial, totalFinal);
        Assert.Equal(primeraCarga, segundaCarga);
    }

    private static async Task<(RegistroCargado[] Registros, int Total)> LeerCargaAsync(IServiceProvider servicios)
    {
        await using var ambito = servicios.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<ContextoTickets>();
        var tickets = await contexto.Tickets.AsNoTracking().Include(t => t.Actividades).ToListAsync();
        var registros = tickets.SelectMany(t => t.Actividades
            .Where(a => a.Tipo == TipoActividad.Comentario && a.Privada &&
                a.Texto?.StartsWith(PrefijoReferencia, StringComparison.Ordinal) == true)
            .Select(a => new RegistroCargado(a.Texto!.Split('\n', 2)[0], t.Id, t.Numero, t.Estado)))
            .OrderBy(r => r.Referencia, StringComparer.Ordinal)
            .ToArray();

        return (registros, tickets.Count);
    }

    private sealed record RegistroCargado(string Referencia, int Id, int? Numero, EstadoTicket Estado);
}
