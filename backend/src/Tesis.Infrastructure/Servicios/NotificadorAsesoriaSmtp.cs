using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tesis.Application.Abstracciones;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Infrastructure.Servicios;

public sealed class NotificadorAsesoriaSmtp : INotificadorAsesoria
{
    private readonly TesisDbContext _contexto;
    private readonly ICorreoService _correo;
    private readonly ILogger<NotificadorAsesoriaSmtp> _logger;

    public NotificadorAsesoriaSmtp(
        TesisDbContext contexto,
        ICorreoService correo,
        ILogger<NotificadorAsesoriaSmtp> logger)
    {
        _contexto = contexto;
        _correo = correo;
        _logger = logger;
    }

    public async Task NotificarSolicitudConfirmacionAsync(
        Guid folio,
        IEnumerable<int> idProfesores,
        CancellationToken ct = default)
    {
        var destino = await _contexto.Profesores
            .Where(p => idProfesores.Contains(p.IdProfesor))
            .Select(p => p.Usuario!.Email)
            .Where(correo => correo != null)
            .ToListAsync(ct);

        if (destino.Count == 0)
        {
            return;
        }

        foreach (var correo in destino)
        {
            await _correo.EnviarAsync(
                correo!,
                "SIRET - UPT: Solicitud de confirmación de asesoría",
                $"<p>Se le ha registrado como asesor de la tesis con folio <strong>{folio}</strong> en el sistema <strong>SIRET (Sistema Integral de Repositorio y Evaluación de Tesis)</strong>. " +
                "Ingrese al sistema para confirmar o rechazar la asesoría.</p>",
                ct);
        }

        _logger.LogInformation("Notificada solicitud de confirmación {Folio} a {Cantidad} destinatarios.", folio, destino.Count);
    }
}