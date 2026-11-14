using System.Security.Cryptography;
using SistemaTickets.Application.Usuarios;

namespace SistemaTickets.Infrastructure.Seguridad;

public class HashContrasenasPbkdf2 : IHashContrasenas
{
    private const int Iteraciones = 210_000;
    private const int BytesSal = 16;
    private const int BytesClave = 32;

    public string Generar(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(BytesSal);
        var clave = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA512, BytesClave);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(clave)}";
    }

    public bool Verificar(string hash, string contrasena)
    {
        var partes = hash.Split('.');
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
        {
            return false;
        }

        var sal = Convert.FromBase64String(partes[1]);
        var esperada = Convert.FromBase64String(partes[2]);
        var clave = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA512, esperada.Length);
        return CryptographicOperations.FixedTimeEquals(clave, esperada);
    }
}
