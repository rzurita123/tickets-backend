using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Auditoria;
using SistemaTickets.Domain.Auditoria;
using SistemaTickets.Infrastructure.Persistencia;

namespace SistemaTickets.Infrastructure.Auditoria;

public class AuditoriaEf(ContextoTickets contexto, TimeProvider reloj) : IAuditoria
{
    private const int LargoMaximoDetalle = 500;
    private const int LimiteMaximo = 4000;

    public void Registrar(int? usuarioId, string operacion, string? entidad = null, string? entidadId = null, string? detalle = null) =>
        contexto.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            Fecha = reloj.GetUtcNow().UtcDateTime,
            UsuarioId = usuarioId,
            Operacion = operacion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle is null ? null : TextoAuditoria.Truncar(detalle, LargoMaximoDetalle),
        });

    public Task GuardarAsync(CancellationToken cancelacion = default) => contexto.SaveChangesAsync(cancelacion);

    public async Task<IReadOnlyList<RegistroAuditoria>> ListarAsync(FiltroAuditoria filtro, CancellationToken cancelacion = default)
    {
        var consulta = contexto.RegistrosAuditoria.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filtro.Operacion))
        {
            consulta = consulta.Where(r => r.Operacion == filtro.Operacion);
        }

        if (filtro.UsuarioId is int usuarioId)
        {
            consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
        {
            consulta = consulta.Where(r => r.Entidad == filtro.Entidad);
        }

        if (!string.IsNullOrWhiteSpace(filtro.EntidadId))
        {
            consulta = consulta.Where(r => r.EntidadId == filtro.EntidadId);
        }

        if (filtro.Desde is DateTime desde)
        {
            consulta = consulta.Where(r => r.Fecha >= desde);
        }

        if (filtro.Hasta is DateTime hasta)
        {
            consulta = consulta.Where(r => r.Fecha < hasta);
        }

        return await consulta
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Id)
            .Take(Math.Clamp(filtro.Limite, 1, LimiteMaximo))
            .ToListAsync(cancelacion);
    }
}
