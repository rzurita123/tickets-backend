using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Usuarios;

public record TokenAcceso(string Valor, DateTime Expira);

public interface IGeneradorToken
{
    TokenAcceso Generar(Usuario usuario);
}
