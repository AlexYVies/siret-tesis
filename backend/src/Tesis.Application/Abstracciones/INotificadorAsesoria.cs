namespace Tesis.Application.Abstracciones;

public interface INotificadorAsesoria
{
    Task NotificarSolicitudConfirmacionAsync(
        Guid folio,
        IEnumerable<int> idProfesores,
        CancellationToken ct = default);
}