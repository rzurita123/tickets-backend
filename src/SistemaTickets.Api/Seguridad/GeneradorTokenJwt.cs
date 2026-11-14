using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Api.Seguridad;

public class GeneradorTokenJwt(IOptions<OpcionesJwt> opciones, TimeProvider reloj) : IGeneradorToken
{
    public TokenAcceso Generar(Usuario usuario)
    {
        var jwt = opciones.Value;
        var expira = reloj.GetUtcNow().UtcDateTime.AddMinutes(jwt.MinutosExpiracion);
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
        };
        if (usuario.LocalidadId is int localidadId)
        {
            claims.Add(new Claim(ClaimsUsuario.Localidad, localidadId.ToString()));
        }

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Emisor,
            Audience = jwt.Audiencia,
            Subject = new ClaimsIdentity(claims),
            Expires = expira,
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256),
        });

        return new TokenAcceso(token, expira);
    }
}
