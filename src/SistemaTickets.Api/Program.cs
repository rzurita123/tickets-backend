using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SistemaTickets.Api.Configuracion;
using SistemaTickets.Api.Middleware;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Infrastructure.Persistencia;
using SistemaTickets.Infrastructure.Seguridad;

const string PoliticaCors = "Frontend";

var builder = WebApplication.CreateBuilder(args);
var configuracion = builder.Configuration;
var esDesarrollo = builder.Environment.IsDevelopment();

var jwt = configuracion.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();
if (string.IsNullOrEmpty(jwt.Clave))
{
    if (!esDesarrollo)
    {
        throw new InvalidOperationException("Falta la configuración Jwt:Clave.");
    }

    jwt.Clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

var cadenaConexion = configuracion.GetConnectionString("Tickets")
    ?? throw new InvalidOperationException("Falta la cadena de conexión Tickets.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<OpcionesJwt>(o =>
{
    o.Clave = jwt.Clave;
    o.Emisor = jwt.Emisor;
    o.Audiencia = jwt.Audiencia;
    o.MinutosExpiracion = jwt.MinutosExpiracion;
});

builder.Services.AddSingleton<IHashContrasenas, HashContrasenasPbkdf2>();
builder.Services.AddPersistencia(cadenaConexion);
builder.Services.AddSingleton<IGeneradorToken, GeneradorTokenJwt>();
builder.Services.AddAplicacion(configuracion);
builder.Services.AddUsuarioActual();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Emisor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization(o => o.AgregarPoliticas());

var origenes = configuracion.GetSection("Cors:Origenes").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy(PoliticaCors, p =>
{
    if (esDesarrollo)
    {
        p.SetIsOriginAllowed(_ => true);
    }
    else
    {
        p.WithOrigins(origenes);
    }

    p.AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
await app.Services.PrepararBaseAsync(
    configuracion["UsuariosIniciales:Contrasena"],
    esDesarrollo && configuracion.GetValue<bool>("Ejemplos:CargarRegistrosDiarios"));

app.UseExceptionHandler();
app.UseHttpsRedirection();
var frontend = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "..", "tickets-frontend"));
if (esDesarrollo && File.Exists(Path.Combine(frontend, "index.html")))
{
    var archivosFrontend = new PhysicalFileProvider(frontend);
    app.Lifetime.ApplicationStopped.Register(archivosFrontend.Dispose);
    var opcionesFrontend = new FileServerOptions { FileProvider = archivosFrontend };
    opcionesFrontend.StaticFileOptions.OnPrepareResponse = respuesta =>
        respuesta.Context.Response.Headers.CacheControl = "no-store";
    app.UseFileServer(opcionesFrontend);
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
