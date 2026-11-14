using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace SistemaTickets.Api.Seguridad;

public static class ClaimsUsuario
{
    public const string Localidad = "localidad";

    public static int? IdUsuario(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
}
