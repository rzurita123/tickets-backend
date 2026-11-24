using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Localidades;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Infrastructure.Catalogos;
using SistemaTickets.Infrastructure.Localidades;
using SistemaTickets.Infrastructure.Soluciones;
using SistemaTickets.Infrastructure.Tickets;
using SistemaTickets.Infrastructure.Usuarios;

namespace SistemaTickets.Infrastructure.Persistencia;

public static class RegistroPersistencia
{
    public static IServiceCollection AddPersistencia(this IServiceCollection servicios, string cadenaConexion)
    {
        servicios.AddDbContext<ContextoTickets>(o => o.UseSqlServer(cadenaConexion));
        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IRepositorioLocalidades, RepositorioLocalidades>();
        servicios.AddScoped<IRepositorioCatalogos, RepositorioCatalogos>();
        servicios.AddScoped<IRepositorioTickets, RepositorioTickets>();
        servicios.AddScoped<IRepositorioSoluciones, RepositorioSoluciones>();
        return servicios;
    }

    public static async Task PrepararBaseAsync(this IServiceProvider servicios, string? contrasenaInicial, bool cargarRegistrosDiarios = false)
    {
        await using var ambito = servicios.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<ContextoTickets>();

        if (contexto.Database.IsSqlServer())
        {
            await contexto.Database.MigrateAsync();
        }
        else
        {
            await contexto.Database.EnsureCreatedAsync();
        }

        await DatosIniciales.CargarAsync(contexto, ambito.ServiceProvider.GetRequiredService<IHashContrasenas>(), contrasenaInicial);
        if (cargarRegistrosDiarios)
        {
            await RegistrosDiarios.CargarAsync(contexto);
        }
    }
}
