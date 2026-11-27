using SistemaTickets.Application.Auditoria;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Soluciones;

public interface IRepositorioParametros
{
    Task<IReadOnlyList<ParametroSugerencia>> ListarAsync(CancellationToken cancelacion = default);

    void Agregar(ParametroSugerencia parametro);

    Task GuardarAsync(CancellationToken cancelacion = default);
}

/// Pesos de la sugerencia de soluciones.
public class ServicioParametros(IRepositorioParametros parametros, IAuditoria auditoria)
{
    public static readonly IReadOnlyDictionary<string, (int Minimo, int Maximo)> Limites = new Dictionary<string, (int, int)>
    {
        ["pesoSistema"] = (0, 10),
        ["pesoSubsistema"] = (0, 10),
        ["pesoPalabraClave"] = (0, 10),
        ["pesoLocalidad"] = (0, 10),
        ["pesoPalabraComun"] = (0, 10),
        ["maxResultados"] = (1, 15),
    };

    public async Task<Dictionary<string, int>> ObtenerAsync(CancellationToken cancelacion = default) =>
        (await parametros.ListarAsync(cancelacion)).ToDictionary(p => p.Clave, p => p.Valor);

    public async Task<Dictionary<string, int>> GuardarAsync(Usuario usuario, IReadOnlyDictionary<string, int?> valores, CancellationToken cancelacion = default)
    {
        if (usuario.Rol != Rol.Administrador)
        {
            throw new SinPermisoException("Sólo un administrador puede hacer este cambio.");
        }

        foreach (var (clave, (minimo, maximo)) in Limites)
        {
            var valor = valores.GetValueOrDefault(clave);
            if (valor is null || valor < minimo || valor > maximo)
            {
                throw new ErrorTicket($"Usá un número entero entre {minimo} y {maximo}.", clave);
            }
        }

        var actuales = (await parametros.ListarAsync(cancelacion)).ToDictionary(p => p.Clave);
        var cambios = new List<string>();
        foreach (var clave in Limites.Keys)
        {
            var nuevo = valores[clave]!.Value;
            if (!actuales.TryGetValue(clave, out var parametro))
            {
                parametros.Agregar(new ParametroSugerencia { Clave = clave, Valor = nuevo });
                cambios.Add($"{clave}: — → {nuevo}");
            }
            else if (parametro.Valor != nuevo)
            {
                cambios.Add($"{clave}: {parametro.Valor} → {nuevo}");
                parametro.Valor = nuevo;
            }
        }

        auditoria.Registrar(usuario.Id, OperacionesAuditoria.Parametros, EntidadesAuditoria.Parametros, null,
            cambios.Count > 0 ? string.Join(" · ", cambios) : "Sin cambios");
        await parametros.GuardarAsync(cancelacion);
        return await ObtenerAsync(cancelacion);
    }
}
