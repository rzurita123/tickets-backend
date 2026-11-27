using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using SistemaTickets.Api.Seguridad;
using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Tickets;

namespace SistemaTickets.Api.Middleware;

/// Un solo lugar que traduce excepciones de negocio a códigos HTTP. Lo no previsto sigue como 500 y se registra.
public sealed class ManejadorExcepciones(ILogger<ManejadorExcepciones> registro) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        var estado = excepcion switch
        {
            ErrorTicket or ErrorCatalogo => StatusCodes.Status400BadRequest,
            SinPermisoException => StatusCodes.Status403Forbidden,
            NoAutenticadoException => StatusCodes.Status401Unauthorized,
            _ => 0,
        };

        if (estado == 0)
        {
            registro.LogError(excepcion, "Error no controlado en {Ruta}", contexto.Request.Path);
            return false;
        }

        contexto.Response.StatusCode = estado;
        var problema = excepcion is ErrorTicket { Campo: string campo }
            ? new ValidationProblemDetails(new Dictionary<string, string[]> { [campo] = [excepcion.Message] }) { Status = estado, Title = excepcion.Message }
            : new ProblemDetails { Status = estado, Title = excepcion.Message };
        await contexto.Response.WriteAsJsonAsync<object>(
            problema,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancelacion);
        return true;
    }
}