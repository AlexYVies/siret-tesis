using Tesis.Application.Abstracciones;

namespace Tesis.Application.Servicios;

public sealed class NotificadorAsesoriaNulo : INotificadorAsesoria
{
    public Task NotificarSolicitudConfirmacionAsync(
        Guid folio,
        IEnumerable<int> idProfesores,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}