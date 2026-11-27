using SistemaTickets.Api.Controllers;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Favoritos;
using SistemaTickets.Application.Soluciones;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Application.Usuarios;

namespace SistemaTickets.Api.Configuracion;

public static class RegistroServicios
{
    public static IServiceCollection AddAplicacion(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddSingleton(configuracion.GetSection("ListadoTickets").Get<OpcionesListadoTickets>() ?? new OpcionesListadoTickets());
        servicios.AddScoped<ServicioIngreso>();
        servicios.AddScoped<ServicioCatalogos>();
        servicios.AddScoped<ServicioTickets>();
        servicios.AddScoped<ServicioSoluciones>();
        servicios.AddScoped<ServicioParametros>();
        servicios.AddScoped<ServicioFavoritos>();
        servicios.AddScoped<ServicioConsultaTickets>();
        servicios.AddScoped<TraductorFiltroTickets>();
        servicios.AddScoped<MapeadorTickets>();
        return servicios;
    }

    public static IServiceCollection AddUsuarioActual(this IServiceCollection servicios)
    {
        servicios.AddHttpContextAccessor();
        servicios.AddScoped<IUsuarioActual, UsuarioActualHttp>();
        return servicios;
    }
}