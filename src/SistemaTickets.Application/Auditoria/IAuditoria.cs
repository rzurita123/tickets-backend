using SistemaTickets.Domain.Auditoria;

namespace SistemaTickets.Application.Auditoria;

public record FiltroAuditoria(
    string? Operacion = null,
    int? UsuarioId = null,
    string? Entidad = null,
    string? EntidadId = null,
    DateTime? Desde = null,
    DateTime? Hasta = null,
    int Limite = 500);

/// Registro de operaciones (RF4). Lo registrado se guarda junto con el resto de los cambios del pedido.
public interface IAuditoria
{
    void Registrar(int? usuarioId, string operacion, string? entidad = null, string? entidadId = null, string? detalle = null);

    Task GuardarAsync(CancellationToken cancelacion = default);

    Task<IReadOnlyList<RegistroAuditoria>> ListarAsync(FiltroAuditoria filtro, CancellationToken cancelacion = default);
}

public static class OperacionesAuditoria
{
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string TicketCreado = "TICKET_CREADO";
    public const string TicketCerradoDirecto = "TICKET_CERRADO_DIRECTO";
    public const string BorradorGuardado = "BORRADOR_GUARDADO";
    public const string BorradorDescartado = "BORRADOR_DESCARTADO";
    public const string ComentarioPublico = "COMENTARIO_PUBLICO";
    public const string ComentarioPrivado = "COMENTARIO_PRIVADO";
    public const string Asignacion = "ASIGNACION";
    public const string Liberacion = "LIBERACION";
    public const string Elevacion = "ELEVACION";
    public const string Bloqueo = "BLOQUEO";
    public const string Desbloqueo = "DESBLOQUEO";
    public const string Datos = "DATOS";
    public const string Cierre = "CIERRE";
    public const string Reapertura = "REAPERTURA";
    public const string Privacidad = "PRIVACIDAD";
    public const string SolucionPropuesta = "SOLUCION_PROPUESTA";
    public const string SolucionCreada = "SOLUCION_CREADA";
    public const string SolucionEditada = "SOLUCION_EDITADA";
    public const string SolucionAprobada = "SOLUCION_APROBADA";
    public const string SolucionRechazada = "SOLUCION_RECHAZADA";
    public const string Catalogo = "CATALOGO";
    public const string Parametros = "PARAMETROS";
    public const string AccesoDenegado = "ACCESO_DENEGADO";
}

public static class EntidadesAuditoria
{
    public const string Ticket = "Ticket";
    public const string Borrador = "Borrador";
    public const string Solucion = "Solucion";
    public const string Catalogo = "Catalogo";
    public const string Parametros = "Parametros";
}

public static class TextoAuditoria
{
    public static string Truncar(string? texto, int largo)
    {
        var limpio = (texto ?? string.Empty).Trim();
        return limpio.Length <= largo ? limpio : limpio[..(largo - 1)].TrimEnd() + "…";
    }
}
