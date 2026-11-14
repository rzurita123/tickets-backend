namespace SistemaTickets.Api.Seguridad;

public class OpcionesJwt
{
    public const string Seccion = "Jwt";

    public string Clave { get; set; } = string.Empty;
    public string Emisor { get; set; } = "SistemaTickets";
    public string Audiencia { get; set; } = "SistemaTickets";
    public int MinutosExpiracion { get; set; } = 480;
}
