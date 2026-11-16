using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tesis.Application.Abstracciones;

namespace Tesis.Infrastructure.Servicios;

public sealed class CorreoServiceSmtp : ICorreoService
{
    private readonly IConfiguration _configuracion;
    private readonly ILogger<CorreoServiceSmtp> _logger;

    public CorreoServiceSmtp(IConfiguration configuracion, ILogger<CorreoServiceSmtp> logger)
    {
        _configuracion = configuracion;
        _logger = logger;
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken ct = default)
    {
        var host = _configuracion["Correo:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning(
                "Correo no configurado (Correo:Host vacío en appsettings). No se envió correo a {Destino} | Asunto: {Asunto}.",
                destinatario, asunto);
            return;
        }

        var usuario = _configuracion["Correo:Usuario"];
        var contrasena = _configuracion["Correo:Contrasena"];
        var remitenteConfigurado = _configuracion["Correo:De"];

        // Si "De" no está especificado o está vacío, usar el usuario configurado (evita que Gmail/Outlook rechacen el envío)
        var remitente = !string.IsNullOrWhiteSpace(remitenteConfigurado)
            ? remitenteConfigurado.Trim()
            : (!string.IsNullOrWhiteSpace(usuario) ? usuario.Trim() : "no-responder@upt.edu.mx");

        var nombreRemitente = _configuracion["Correo:NombreRemitente"] ??
                              _configuracion["Correo:Nombre"] ??
                              "SIRET · Sistema Integral de Repositorio y Evaluación de Tesis";

        var puerto = int.TryParse(_configuracion["Correo:Puerto"], out var puertoConfigurado)
            ? puertoConfigurado
            : 587;

        var usarSsl = !bool.TryParse(_configuracion["Correo:UsarSsl"], out var ssl) || ssl;

        using var smtp = new SmtpClient(host.Trim(), puerto)
        {
            EnableSsl = usarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout = 20000,
        };

        if (!string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(contrasena))
        {
            var passwordLimpia = host.Contains("gmail", StringComparison.OrdinalIgnoreCase)
                ? contrasena.Replace(" ", "").Trim()
                : contrasena.Trim();
            smtp.Credentials = new NetworkCredential(usuario.Trim(), passwordLimpia);
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(remitente, nombreRemitente, System.Text.Encoding.UTF8),
            Subject = asunto,
            SubjectEncoding = System.Text.Encoding.UTF8,
            Body = cuerpoHtml,
            BodyEncoding = System.Text.Encoding.UTF8,
            IsBodyHtml = true,
        };
        mensaje.To.Add(new MailAddress(destinatario.Trim()));

        if (cuerpoHtml.Contains("cid:"))
        {
            var vistaHtml = AlternateView.CreateAlternateViewFromString(cuerpoHtml, System.Text.Encoding.UTF8, "text/html");

            var baseDirs = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets"),
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Tesis.Api", "Assets"),
                Path.Combine(Directory.GetCurrentDirectory(), "frontend", "public", "assets"),
                @"C:\Users\Alex\Desktop\Tesis\backend\src\Tesis.Api\Assets",
                @"C:\Users\Alex\Desktop\Tesis\frontend\public\assets"
            };

            void AdjuntarSiExiste(string cid, string nombreArchivo)
            {
                if (!cuerpoHtml.Contains($"cid:{cid}")) return;
                var ruta = baseDirs.Select(d => Path.Combine(d, nombreArchivo)).FirstOrDefault(File.Exists);
                if (ruta != null)
                {
                    var recurso = new LinkedResource(ruta, "image/png")
                    {
                        ContentId = cid,
                        TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                    };
                    vistaHtml.LinkedResources.Add(recurso);
                }
            }

            AdjuntarSiExiste("banner_validacion", "banner_validacion.png");
            AdjuntarSiExiste("banner_recuperacion", "banner_recuperacion.png");
            AdjuntarSiExiste("logo_upt", "img_LogoToroUPT.png");

            mensaje.AlternateViews.Add(vistaHtml);
        }



        try
        {
            await smtp.SendMailAsync(mensaje, ct);
            _logger.LogInformation("Correo enviado exitosamente a {Destino} con asunto '{Asunto}'.", destinatario, asunto);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(
                ex,
                "Error SMTP al enviar correo a {Destino} (StatusCode={StatusCode}, Host={Host}, Puerto={Puerto}): {Mensaje}",
                destinatario, ex.StatusCode, host, puerto, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error inesperado al enviar correo a {Destino}: {Mensaje}",
                destinatario, ex.Message);
            throw;
        }
    }
}