using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaTickets.Application.Auditoria;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Favoritos;
using SistemaTickets.Application.Localidades;
using SistemaTickets.Application.Notificaciones;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Infrastructure.Auditoria;
using SistemaTickets.Infrastructure.Catalogos;
using SistemaTickets.Infrastructure.Favoritos;
using SistemaTickets.Infrastructure.Localidades;
using SistemaTickets.Infrastructure.Notificaciones;
using SistemaTickets.Infrastructure.Soluciones;
using SistemaTickets.Infrastructure.Tickets;
using SistemaTickets.Infrastructure.Usuarios;

namespace SistemaTickets.Infrastructure.Persistencia;

public static class RegistroPersistencia
{
    public static IServiceCollection AddPersistencia(this IServiceCollection servicios, string cadenaConexion)
    {
        servicios.AddDbContext<ContextoTickets>(o => o.UseSqlServer(cadenaConexion, sql => sql.EnableRetryOnFailure()));
        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IRepositorioLocalidades, RepositorioLocalidades>();
        servicios.AddScoped<IRepositorioCatalogos, RepositorioCatalogos>();
        servicios.AddScoped<IRepositorioTickets, RepositorioTickets>();
        servicios.AddScoped<IRepositorioSoluciones, RepositorioSoluciones>();
        servicios.AddScoped<IRepositorioParametros, RepositorioParametros>();
        servicios.AddScoped<IRepositorioFavoritos, RepositorioFavoritos>();
        servicios.AddScoped<IAuditoria, AuditoriaEf>();
        servicios.AddScoped<INotificaciones, NotificacionesEf>();
        servicios.AddMemoryCache();
        servicios.AddScoped<IConsultaTickets, ConsultaTicketsEf>();
        servicios.AddSingleton<ICatalogoLectura, CatalogoLecturaEnCache>();
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
