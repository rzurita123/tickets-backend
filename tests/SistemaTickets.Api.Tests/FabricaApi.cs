using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Api.Tests;

public class FabricaApi : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _conexion.Open();
        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<ContextoTickets>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<ContextoTickets>>();
            servicios.AddDbContext<ContextoTickets>(o => o.UseSqlite(_conexion));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _conexion.Dispose();
        }
    }
}
