using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Infrastructure.Seguridad;
using SistemaTickets.Infrastructure.Usuarios;

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

var contrasenaInicial = configuracion["UsuariosIniciales:Contrasena"]
    ?? throw new InvalidOperationException("Falta la configuración UsuariosIniciales:Contrasena.");

builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<OpcionesJwt>(o =>
{
    o.Clave = jwt.Clave;
    o.Emisor = jwt.Emisor;
    o.Audiencia = jwt.Audiencia;
    o.MinutosExpiracion = jwt.MinutosExpiracion;
});

builder.Services.AddSingleton<IHashContrasenas, HashContrasenasPbkdf2>();
builder.Services.AddSingleton<IRepositorioUsuarios>(sp =>
    new RepositorioUsuariosEnMemoria(sp.GetRequiredService<IHashContrasenas>(), contrasenaInicial));
builder.Services.AddSingleton<IGeneradorToken, GeneradorTokenJwt>();
builder.Services.AddScoped<ServicioIngreso>();

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
builder.Services.AddAuthorization();

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

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
