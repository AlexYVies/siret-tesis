namespace Tesis.Application.Abstracciones;

public interface ICorreoService
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken ct = default);
}