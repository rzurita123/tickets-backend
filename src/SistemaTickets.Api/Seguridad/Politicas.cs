using Microsoft.AspNetCore.Authorization;

using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Seguridad;

public static class Politicas
{
    public const string EsOrmen = nameof(EsOrmen);
    public const string EsAdministrador = nameof(EsAdministrador);

    public static AuthorizationOptions AgregarPoliticas(this AuthorizationOptions opciones)
    {
        opciones.AddPolicy(EsOrmen, p => p.RequireRole(nameof(Rol.Operador), nameof(Rol.Administrador)));
        opciones.AddPolicy(EsAdministrador, p => p.RequireRole(nameof(Rol.Administrador)));
        return opciones;
    }
}