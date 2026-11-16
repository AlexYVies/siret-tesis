using System.Text.Json;
using Tesis.Application.Excepciones;

namespace Tesis.Api.Middlewares;

public class ErrorApiMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ErrorApiMiddleware> _logger;

    public ErrorApiMiddleware(RequestDelegate siguiente, ILogger<ErrorApiMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _siguiente(context);
        }
        catch (EntidadNoEncontradaException ex)
        {
            await ResponderAsync(context, StatusCodes.Status404NotFound, "NO_ENCONTRADO", ex.Message, null);
        }
        catch (SolicitudInvalidaException ex)
        {
            await ResponderAsync(context, StatusCodes.Status400BadRequest, "SOLICITUD_INVALIDA", ex.Message, null);
        }
        catch (OperacionNoPermitidaException ex)
        {
            await ResponderAsync(context, StatusCodes.Status403Forbidden, "OPERACION_NO_PERMITIDA", ex.Message, null);
        }
        catch (ConflictoDeNegocioException ex)
        {
            await ResponderAsync(context, StatusCodes.Status409Conflict, "CONFLICTO_DE_NEGOCIO", ex.Message, null);
        }
        catch (ServicioNoDisponibleException ex)
        {
            await ResponderAsync(context, StatusCodes.Status503ServiceUnavailable, "SERVICIO_NO_DISPONIBLE", ex.Message, null);
        }
        catch (UnauthorizedAccessException)
        {
            await ResponderAsync(context, StatusCodes.Status401Unauthorized, "NO_AUTENTICADO", "No está autenticado.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado.");
            await ResponderAsync(context, StatusCodes.Status500InternalServerError, "ERROR_INTERNO", "Ocurrió un error inesperado.", null);
        }
    }

    private static Task ResponderAsync(
        HttpContext context,
        int codigoEstado,
        string codigo,
        string mensaje,
        string[]? detalles)
    {
        context.Response.StatusCode = codigoEstado;
        context.Response.ContentType = "application/json";
        var cuerpo = JsonSerializer.Serialize(new { code = codigo, message = mensaje, details = detalles });
        return context.Response.WriteAsync(cuerpo);
    }
}