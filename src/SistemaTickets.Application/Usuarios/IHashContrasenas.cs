namespace SistemaTickets.Application.Usuarios;

public interface IHashContrasenas
{
    string Generar(string contrasena);

    bool Verificar(string hash, string contrasena);
}
