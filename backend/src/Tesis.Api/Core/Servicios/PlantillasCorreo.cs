namespace Tesis.Api.Core.Servicios;

public static class PlantillasCorreo
{
    public static string GenerarHtmlValidacionCuenta(string destinatario, int idUsuario, string enlaceValidacion, string token)
    {
        return $@"<!DOCTYPE html>
<html lang=""es"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <meta name=""color-scheme"" content=""light"">
  <meta name=""supported-color-schemes"" content=""light"">
  <title>Valida tu cuenta - SIRET UPT</title>
  <style>
    :root {{
      color-scheme: light;
      supported-color-schemes: light;
    }}
    body, table, td, p, a, span {{
      -webkit-text-size-adjust: none;
      -ms-text-size-adjust: none;
    }}
    @media only screen and (max-width: 680px) {{
      .contenedor-tarjeta {{
        width: 100% !important;
        border-radius: 14px !important;
      }}
      .cuerpo-tarjeta {{
        padding: 30px 22px 24px 22px !important;
      }}
      .texto-correo {{
        font-size: 15px !important;
        line-height: 1.6 !important;
      }}
      .btn-enlace {{
        padding: 15px 32px !important;
        font-size: 15px !important;
      }}
    }}
  </style>
</head>
<body style=""margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#1e293b;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color:#f1f5f9;padding:40px 16px;"">
    <tr>
      <td align=""center"">
        <!-- Contenedor Tarjeta Principal Ampliado para Computadora -->
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" class=""contenedor-tarjeta"" style=""max-width:660px;background-color:#ffffff;border-radius:18px;box-shadow:0 10px 28px rgba(0,0,0,0.08);overflow:hidden;border:1px solid #e2e8f0;"">
          
          <!-- Banner Superior UPT Institucional Ampliado -->
          <tr>
            <td style=""padding:0;margin:0;line-height:0;background-color:#001e61;text-align:center;"">
              <img src=""cid:banner_validacion"" width=""660"" alt=""SIRET · Sistema Integral de Repositorio y Evaluación de Tesis - Activación de Cuenta"" style=""display:block;width:100%;max-width:660px;height:auto;border:0;outline:none;margin:0 auto;"" />
            </td>
          </tr>

          <!-- Cuerpo de la Tarjeta con espacio cómodo -->
          <tr>
            <td class=""cuerpo-tarjeta"" style=""background-color:#ffffff;padding:44px 48px 34px 48px;"">
              <p class=""texto-correo"" style=""margin:0 0 18px 0;font-size:16.5px;line-height:1.65;color:#334155;"">
                Estimado(a) <strong style=""color:#001e61;"">{destinatario}</strong>:
              </p>
              <p class=""texto-correo"" style=""margin:0 0 26px 0;font-size:16px;line-height:1.65;color:#475569;"">
                Te damos la bienvenida a <strong>SIRET (Sistema Integral de Repositorio y Evaluación de Tesis)</strong> de la Universidad Politécnica de Tulancingo. Para confirmar tu dirección de correo electrónico y activar tu acceso al sistema, presiona el botón a continuación:
              </p>

              <!-- Botón Principal con protección contra inversión en modo oscuro -->
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin:34px 0;"">
                <tr>
                  <td align=""center"">
                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                      <tr>
                        <td align=""center"" style=""border-radius:12px;background-color:#001e61;border:2px solid #ffc629;box-shadow:0 4px 14px rgba(0,30,97,0.3);"">
                          <a href=""{enlaceValidacion}"" target=""_blank"" class=""btn-enlace"" style=""display:inline-block;padding:16px 48px;font-family:Arial,sans-serif;font-size:16px;font-weight:bold;text-decoration:none;color:#ffffff !important;border-radius:10px;"">
                            <span style=""color:#ffffff !important;-webkit-text-fill-color:#ffffff !important;text-shadow:0 0 1px #ffffff;font-size:16px;font-weight:bold;text-decoration:none;"">Validar mi cuenta</span>
                          </a>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>

              <!-- Nota de seguridad -->
              <p style=""margin:30px 0 0 0;font-size:13.5px;line-height:1.5;color:#64748b;border-top:1px solid #e2e8f0;padding-top:22px;"">
                Si no solicitaste este registro en el sistema SIRET, puedes ignorar este mensaje con total seguridad. Tu cuenta no será activada sin tu confirmación.
              </p>
            </td>
          </tr>

          <!-- Pie de Página Institucional -->
          <tr>
            <td style=""background-color:#f8fafc;border-top:1px solid #e2e8f0;padding:24px 36px;text-align:center;"">
              <p style=""margin:0 0 4px 0;color:#001e61;font-size:13px;font-weight:bold;letter-spacing:0.5px;"">
                Universidad Politécnica de Tulancingo
              </p>
              <p style=""margin:0;font-size:11.5px;color:#64748b;line-height:1.5;"">
                Dirección de Posgrado e Investigación · SIRET (Sistema Integral de Repositorio y Evaluación de Tesis)<br>
                Este es un mensaje institucional automático, favor de no responder.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    public static string GenerarHtmlRecuperarContrasena(string destinatario, int idUsuario, string enlaceRestablecer, string token)
    {
        return $@"<!DOCTYPE html>
<html lang=""es"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <meta name=""color-scheme"" content=""light"">
  <meta name=""supported-color-schemes"" content=""light"">
  <title>Restablecer contraseña - SIRET UPT</title>
  <style>
    :root {{
      color-scheme: light;
      supported-color-schemes: light;
    }}
    body, table, td, p, a, span {{
      -webkit-text-size-adjust: none;
      -ms-text-size-adjust: none;
    }}
    @media only screen and (max-width: 680px) {{
      .contenedor-tarjeta {{
        width: 100% !important;
        border-radius: 14px !important;
      }}
      .cuerpo-tarjeta {{
        padding: 30px 22px 24px 22px !important;
      }}
      .texto-correo {{
        font-size: 15px !important;
        line-height: 1.6 !important;
      }}
      .btn-enlace {{
        padding: 15px 32px !important;
        font-size: 15px !important;
      }}
    }}
  </style>
</head>
<body style=""margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#1e293b;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color:#f1f5f9;padding:40px 16px;"">
    <tr>
      <td align=""center"">
        <!-- Contenedor Tarjeta Principal Ampliado para Computadora -->
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" class=""contenedor-tarjeta"" style=""max-width:660px;background-color:#ffffff;border-radius:18px;box-shadow:0 10px 28px rgba(0,0,0,0.08);overflow:hidden;border:1px solid #e2e8f0;"">
          
          <!-- Banner Superior UPT Institucional Guinda Ampliado -->
          <tr>
            <td style=""padding:0;margin:0;line-height:0;background-color:#870f54;text-align:center;"">
              <img src=""cid:banner_recuperacion"" width=""660"" alt=""SIRET · Sistema Integral de Repositorio y Evaluación de Tesis - Recuperación de Contraseña"" style=""display:block;width:100%;max-width:660px;height:auto;border:0;outline:none;margin:0 auto;"" />
            </td>
          </tr>

          <!-- Cuerpo de la Tarjeta con espacio cómodo -->
          <tr>
            <td class=""cuerpo-tarjeta"" style=""background-color:#ffffff;padding:44px 48px 34px 48px;"">
              <p class=""texto-correo"" style=""margin:0 0 18px 0;font-size:16.5px;line-height:1.65;color:#334155;"">
                Hola, <strong style=""color:#870f54;"">{destinatario}</strong>:
              </p>
              <p class=""texto-correo"" style=""margin:0 0 26px 0;font-size:16px;line-height:1.65;color:#475569;"">
                Recibimos una solicitud para restablecer la contraseña de acceso a tu cuenta en <strong>SIRET (Sistema Integral de Repositorio y Evaluación de Tesis)</strong> de la UPT. Haz clic en el siguiente botón para asignar una nueva contraseña:
              </p>

              <!-- Botón Principal Guinda con protección contra inversión en modo oscuro -->
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin:34px 0;"">
                <tr>
                  <td align=""center"">
                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                      <tr>
                        <td align=""center"" style=""border-radius:12px;background-color:#870f54;border:2px solid #ffc629;box-shadow:0 4px 14px rgba(135,15,84,0.3);"">
                          <a href=""{enlaceRestablecer}"" target=""_blank"" class=""btn-enlace"" style=""display:inline-block;padding:16px 48px;font-family:Arial,sans-serif;font-size:16px;font-weight:bold;text-decoration:none;color:#ffffff !important;border-radius:10px;"">
                            <span style=""color:#ffffff !important;-webkit-text-fill-color:#ffffff !important;text-shadow:0 0 1px #ffffff;font-size:16px;font-weight:bold;text-decoration:none;"">Restablecer contraseña</span>
                          </a>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>

              <!-- Nota de seguridad -->
              <p style=""margin:30px 0 0 0;font-size:13.5px;line-height:1.5;color:#64748b;border-top:1px solid #e2e8f0;padding-top:22px;"">
                Si no solicitaste este cambio, puedes ignorar este mensaje. Tu contraseña actual permanecerá sin modificaciones.
              </p>
            </td>
          </tr>

          <!-- Pie de Página Institucional -->
          <tr>
            <td style=""background-color:#f8fafc;border-top:1px solid #e2e8f0;padding:24px 36px;text-align:center;"">
              <p style=""margin:0 0 4px 0;color:#001e61;font-size:13px;font-weight:bold;letter-spacing:0.5px;"">
                Universidad Politécnica de Tulancingo
              </p>
              <p style=""margin:0;font-size:11.5px;color:#64748b;line-height:1.5;"">
                Dirección de Posgrado e Investigación · SIRET (Sistema Integral de Repositorio y Evaluación de Tesis)<br>
                Este es un mensaje institucional automático, favor de no responder.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    public static string GenerarHtmlInvitacionDocente(string destinatario, string correo, string enlaceInvitacion)
    {
        return $@"<!DOCTYPE html>
<html lang=""es"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <meta name=""color-scheme"" content=""light"">
  <meta name=""supported-color-schemes"" content=""light"">
  <title>Invitación de Docente - SIRET UPT</title>
  <style>
    :root {{
      color-scheme: light;
      supported-color-schemes: light;
    }}
    body, table, td, p, a, span {{
      -webkit-text-size-adjust: none;
      -ms-text-size-adjust: none;
    }}
    @media only screen and (max-width: 680px) {{
      .contenedor-tarjeta {{
        width: 100% !important;
        border-radius: 14px !important;
      }}
      .cuerpo-tarjeta {{
        padding: 30px 22px 24px 22px !important;
      }}
      .texto-correo {{
        font-size: 15px !important;
        line-height: 1.6 !important;
      }}
      .btn-enlace {{
        padding: 15px 32px !important;
        font-size: 15px !important;
      }}
    }}
  </style>
</head>
<body style=""margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#1e293b;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color:#f1f5f9;padding:40px 16px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" class=""contenedor-tarjeta"" style=""max-width:660px;background-color:#ffffff;border-radius:18px;box-shadow:0 10px 28px rgba(0,0,0,0.08);overflow:hidden;border:1px solid #e2e8f0;"">
          <tr>
            <td style=""padding:0;margin:0;line-height:0;background-color:#001e61;text-align:center;"">
              <img src=""cid:banner_validacion"" width=""660"" alt=""SIRET · Sistema Integral de Repositorio y Evaluación de Tesis - Invitación de Docente"" style=""display:block;width:100%;max-width:660px;height:auto;border:0;outline:none;margin:0 auto;"" />
            </td>
          </tr>
          <tr>
            <td class=""cuerpo-tarjeta"" style=""background-color:#ffffff;padding:44px 48px 34px 48px;"">
              <p class=""texto-correo"" style=""margin:0 0 18px 0;font-size:16.5px;line-height:1.65;color:#334155;"">
                Estimado(a) <strong style=""color:#001e61;"">{destinatario}</strong>:
              </p>
              <p class=""texto-correo"" style=""margin:0 0 18px 0;font-size:16px;line-height:1.65;color:#475569;"">
                La Administración de <strong>SIRET (Sistema Integral de Repositorio y Evaluación de Tesis)</strong> de la Universidad Politécnica de Tulancingo te ha enviado una invitación para activar tu cuenta con el perfil de <strong>Docente</strong> asociada al correo institucional <strong>{correo}</strong>.
              </p>
              <p class=""texto-correo"" style=""margin:0 0 26px 0;font-size:16px;line-height:1.65;color:#475569;"">
                Haz clic en el siguiente botón para completar tu registro y crear tu contraseña de acceso:
              </p>
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin:34px 0;"">
                <tr>
                  <td align=""center"">
                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                      <tr>
                        <td align=""center"" style=""border-radius:12px;background-color:#001e61;border:2px solid #ffc629;box-shadow:0 4px 14px rgba(0,30,97,0.3);"">
                          <a href=""{enlaceInvitacion}"" target=""_blank"" class=""btn-enlace"" style=""display:inline-block;padding:16px 48px;font-family:Arial,sans-serif;font-size:16px;font-weight:bold;text-decoration:none;color:#ffffff !important;border-radius:10px;"">
                            <span style=""color:#ffffff !important;-webkit-text-fill-color:#ffffff !important;text-shadow:0 0 1px #ffffff;font-size:16px;font-weight:bold;text-decoration:none;"">Completar mi registro de Docente</span>
                          </a>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
              <p style=""margin:30px 0 0 0;font-size:13.5px;line-height:1.5;color:#64748b;border-top:1px solid #e2e8f0;padding-top:22px;"">
                Este enlace es exclusivo para docentes invitados por la administración de posgrado de la UPT.
              </p>
            </td>
          </tr>
          <tr>
            <td style=""background-color:#f8fafc;border-top:1px solid #e2e8f0;padding:24px 36px;text-align:center;"">
              <p style=""margin:0 0 4px 0;color:#001e61;font-size:13px;font-weight:bold;letter-spacing:0.5px;"">
                Universidad Politécnica de Tulancingo
              </p>
              <p style=""margin:0;font-size:11.5px;color:#64748b;line-height:1.5;"">
                Dirección de Posgrado e Investigación · SIRET (Sistema Integral de Repositorio y Evaluación de Tesis)<br>
                Este es un mensaje institucional automático, favor de no responder.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    public static string GenerarHtmlPrueba(string destinatario)
    {
        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"">
  <title>Correo de Prueba - SIRET UPT</title>
</head>
<body style=""margin:0;padding:24px;background-color:#f8fafc;font-family:sans-serif;color:#1e293b;"">
  <div style=""max-width:560px;margin:auto;background:#fff;padding:36px;border-radius:14px;border:1px solid #e2e8f0;box-shadow:0 2px 8px rgba(0,0,0,0.05);"">
    <div style=""height:4px;background:linear-gradient(90deg,#001e61 0%,#870f54 50%,#ffc629 100%);margin-bottom:20px;border-radius:2px;""></div>
    <h2 style=""color:#001e61;margin-top:0;"">¡Configuración de correo SIRET - UPT exitosa!</h2>
    <p>Este es un correo de prueba enviado desde el sistema <strong>SIRET · Sistema Integral de Repositorio y Evaluación de Tesis (UPT)</strong>.</p>
    <p>Destinatario: <code>{destinatario}</code></p>
    <p>Fecha y hora de envío: <strong>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</strong></p>
    <hr style=""border:none;border-top:1px solid #e2e8f0;margin:20px 0;"">
    <p style=""font-size:12px;color:#870f54;font-weight:bold;"">El servicio SMTP está funcionando de manera correcta y con diseño amplio para computadoras.</p>
  </div>
</body>
</html>";
    }
}
