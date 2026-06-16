using SendGrid;
using SendGrid.Helpers.Mail;

namespace AURORA.Servicios
{
    public class EmailService
    {
        private readonly string _apiKey;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["EmailSettings:SendGridApiKey"] ?? "";
        }
        // Agregar este método dentro de la clase EmailService

        public async Task SendAdminAccessRequestAsync(string fromEmail, string mensaje)
        {
            var client = new SendGridClient(_apiKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress("auroraappoficial@gmail.com", "AURORA"),
                Subject = "⚠️ Solicitud de acceso · AURORA",
                HtmlContent = $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""/></head>
<body style=""margin:0;padding:0;background:#f7f3ec;font-family:Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""padding:40px 16px;"">
    <tr><td align=""center"">
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
             style=""max-width:480px;background:#ffffff;border-radius:14px;border:1px solid rgba(28,24,20,0.10);overflow:hidden;"">

        <tr>
          <td style=""height:4px;background:linear-gradient(90deg,#b85c38,#d4795a);""></td>
        </tr>

        <tr>
          <td style=""padding:32px 36px 0;text-align:center;"">
            <p style=""margin:0;font-size:11px;letter-spacing:0.2em;text-transform:uppercase;color:#b0a498;"">AURORA</p>
            <h1 style=""margin:12px 0 0;font-size:20px;font-weight:700;color:#1c1814;"">Solicitud de acceso denegado</h1>
          </td>
        </tr>

        <tr>
          <td style=""padding:20px 36px 0;"">
            <p style=""margin:0;font-size:13px;color:#7a6e63;line-height:1.7;"">
              Un usuario ha solicitado aclarar su situación de acceso.
            </p>
          </td>
        </tr>

        <tr>
          <td style=""padding:16px 36px 0;"">
            <div style=""background:#efe9de;border-radius:8px;padding:14px 16px;border:1px solid rgba(28,24,20,0.08);"">
              <p style=""margin:0 0 6px;font-size:10px;text-transform:uppercase;letter-spacing:0.08em;color:#b0a498;"">Correo del usuario</p>
              <p style=""margin:0;font-size:14px;font-weight:600;color:#1c1814;"">{fromEmail}</p>
            </div>
          </td>
        </tr>

        <tr>
          <td style=""padding:12px 36px 0;"">
            <div style=""background:#efe9de;border-radius:8px;padding:14px 16px;border:1px solid rgba(28,24,20,0.08);"">
              <p style=""margin:0 0 6px;font-size:10px;text-transform:uppercase;letter-spacing:0.08em;color:#b0a498;"">Mensaje</p>
              <p style=""margin:0;font-size:14px;color:#3d352c;line-height:1.6;"">{mensaje}</p>
            </div>
          </td>
        </tr>

        <tr>
          <td style=""padding:28px 36px 32px;"">
            <div style=""border-top:1px solid rgba(28,24,20,0.08);padding-top:20px;text-align:center;"">
              <p style=""margin:0;font-size:11px;color:#b0a498;"">© 2026 AURORA App · Mensaje enviado desde la página de acceso denegado.</p>
            </div>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>"
            };

            msg.AddTo(new EmailAddress("auroraappoficial@gmail.com", "Admin AURORA"));
            await client.SendEmailAsync(msg);
        }
        public async Task SendContactoConfirmacionAsync(string toEmail)
        {
            var client = new SendGridClient(_apiKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress("auroraappoficial@gmail.com", "AURORA"),
                Subject = "Recibimos tu mensaje · AURORA",
                HtmlContent = @"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""/></head>
<body style=""margin:0;padding:0;background:#f7f3ec;font-family:Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""padding:40px 16px;"">
    <tr><td align=""center"">
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
             style=""max-width:480px;background:#ffffff;border-radius:14px;border:1px solid rgba(28,24,20,0.10);overflow:hidden;"">

        <tr>
          <td style=""height:4px;background:linear-gradient(90deg,#b85c38,#d4795a);""></td>
        </tr>

        <tr>
          <td style=""padding:32px 36px 0;text-align:center;"">
            <p style=""margin:0;font-size:11px;letter-spacing:0.2em;text-transform:uppercase;color:#b0a498;"">AURORA</p>
            <h1 style=""margin:12px 0 0;font-size:20px;font-weight:700;color:#1c1814;"">Recibimos tu mensaje</h1>
          </td>
        </tr>

        <tr>
          <td style=""padding:20px 36px 0;"">
            <p style=""margin:0;font-size:13px;color:#7a6e63;line-height:1.7;text-align:center;"">
              Gracias por contactarnos. Hemos recibido tu mensaje y lo revisaremos a la brevedad.
            </p>
          </td>
        </tr>

        <tr>
          <td style=""padding:20px 36px 0;"">
            <div style=""background:#efe9de;border-radius:8px;padding:14px 16px;border:1px solid rgba(28,24,20,0.08);"">
              <p style=""margin:0 0 10px;font-size:10px;text-transform:uppercase;letter-spacing:0.08em;color:#b0a498;"">¿Qué sigue?</p>
              <table cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                <tr>
                  <td style=""padding:5px 0;"">
                    <table cellpadding=""0"" cellspacing=""0"" border=""0"">
                      <tr>
                        <td style=""font-size:16px;padding-right:10px;"">⏱</td>
                        <td style=""font-size:13px;color:#3d352c;line-height:1.5;"">Nuestro equipo te responderá en <strong>24–48 horas</strong>.</td>
                      </tr>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:5px 0;"">
                    <table cellpadding=""0"" cellspacing=""0"" border=""0"">
                      <tr>
                        <td style=""font-size:16px;padding-right:10px;"">📬</td>
                        <td style=""font-size:13px;color:#3d352c;line-height:1.5;"">Revisa tu bandeja de entrada y la carpeta de <strong>spam</strong>.</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </div>
          </td>
        </tr>

        <tr>
          <td style=""padding:28px 36px 32px;"">
            <div style=""border-top:1px solid rgba(28,24,20,0.08);padding-top:20px;text-align:center;"">
              <p style=""margin:0;font-size:11px;color:#b0a498;"">— Equipo AURORA · auroraappoficial@gmail.com</p>
            </div>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>"
            };

            msg.AddTo(new EmailAddress(toEmail));
            await client.SendEmailAsync(msg);
        }
        public async Task SendPasswordRecoveryCodeAsync(string toEmail, string code)
        {
            var codeBoxes = string.Join("", code.Select(c => $@"
                <td style=""padding:0 5px;"">
                  <div style=""
                    width:52px;height:64px;background:#1e1e1e;
                    border:1px solid rgba(200,169,110,0.35);border-radius:12px;
                    font-size:28px;font-weight:700;color:#c8a96e;
                    text-align:center;line-height:64px;
                    font-family:Arial,sans-serif;
                  "">{c}</div>
                </td>"));

            var client = new SendGridClient(_apiKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress("auroraappoficial@gmail.com", "AURORA"),
                Subject = "Código de recuperación · AURORA",
                HtmlContent = $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""/></head>
<body style=""margin:0;padding:0;background:#0d0d0d;font-family:Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#0d0d0d;padding:48px 16px;"">
    <tr><td align=""center"">
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
             style=""max-width:520px;background:#161616;border-radius:20px;border:1px solid rgba(255,255,255,0.08);"">
        <tr>
          <td style=""background:#111;border-bottom:1px solid rgba(255,255,255,0.06);padding:28px 40px;text-align:center;"">
            <div style=""display:inline-block;background:rgba(200,169,110,0.12);border:1px solid rgba(200,169,110,0.25);border-radius:12px;padding:8px 22px;font-size:15px;font-weight:700;letter-spacing:3px;color:#c8a96e;"">
              AURORA
            </div>
          </td>
        </tr>
        <tr>
          <td align=""center"" style=""padding:32px 40px 0;"">
            <h1 style=""margin:0;font-size:24px;font-weight:400;color:#ececec;"">Recuperación de contraseña</h1>
          </td>
        </tr>
        <tr>
          <td align=""center"" style=""padding:16px 40px 0;"">
            <p style=""margin:0;font-size:15px;color:#888;line-height:1.6;"">
              Usa el siguiente código para continuar. Expira en <strong style=""color:#c8a96e;"">15 minutos</strong>.
            </p>
          </td>
        </tr>
        <tr>
          <td align=""center"" style=""padding:32px 40px;"">
            <table cellpadding=""0"" cellspacing=""0"" border=""0""><tr>{codeBoxes}</tr></table>
          </td>
        </tr>
        <tr>
          <td style=""background:#111;border-top:1px solid rgba(255,255,255,0.06);padding:20px 40px;text-align:center;"">
            <p style=""margin:0;font-size:12px;color:#555;"">
              © 2026 AURORA App · Si no solicitaste esto, ignora este correo.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>"
            };

            msg.AddTo(new EmailAddress(toEmail));
            await client.SendEmailAsync(msg);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Email de racha: se envía automáticamente al cerrar una sesión
        //  de lectura cuando el usuario suma un día nuevo a su racha.
        // ─────────────────────────────────────────────────────────────────
        public async Task SendStreakReminderAsync(string toEmail, string nombre, int diasRacha)
        {
            // Mensaje motivacional según el hito de días
            string titulo, mensaje, emoji;

            if (diasRacha >= 100)
            {
                emoji = "🏆";
                titulo = $"¡{diasRacha} días de racha! Eres una leyenda";
                mensaje = $"Has alcanzado <strong style=\"color:#c8a96e;\">{diasRacha} días consecutivos</strong> de lectura. Pocos lectores llegan aquí. Sigue así.";
            }
            else if (diasRacha >= 30)
            {
                emoji = "⚡";
                titulo = $"¡{diasRacha} días seguidos!";
                mensaje = $"Un mes entero leyendo cada día. Tu racha de <strong style=\"color:#c8a96e;\">{diasRacha} días</strong> es impresionante. No la detengas.";
            }
            else if (diasRacha >= 7)
            {
                emoji = "🔥";
                titulo = $"¡{diasRacha} días de racha!";
                mensaje = $"Llevas <strong style=\"color:#c8a96e;\">{diasRacha} días consecutivos</strong> leyendo. ¡Sigue así, ya estás formando un hábito!";
            }
            else if (diasRacha >= 3)
            {
                emoji = "📖";
                titulo = $"Racha activa: {diasRacha} días";
                mensaje = $"Ya llevas <strong style=\"color:#c8a96e;\">{diasRacha} días seguidos</strong> leyendo. Cada página cuenta. ¡No pares!";
            }
            else
            {
                emoji = "✨";
                titulo = "¡Racha activada!";
                mensaje = "Acabas de iniciar tu racha de lectura. <strong style=\"color:#c8a96e;\">Vuelve mañana</strong> para que siga creciendo.";
            }

            // Barra de progreso hacia la próxima meta
            int proximaMeta = diasRacha < 7 ? 7 : diasRacha < 15 ? 15 : diasRacha < 30 ? 30 : diasRacha < 60 ? 60 : 100;
            int progresoPct = (int)Math.Min(((double)diasRacha / proximaMeta) * 100, 100);
            int diasFaltantes = Math.Max(proximaMeta - diasRacha, 0);

            string barraProgreso = $@"
              <div style=""background:#1a1a1a;border-radius:8px;height:10px;overflow:hidden;margin:16px 0 8px;"">
                <div style=""background:linear-gradient(90deg,#c8a96e,#e8c98e);height:10px;width:{progresoPct}%;border-radius:8px;""></div>
              </div>
              <p style=""margin:0;font-size:12px;color:#666;text-align:right;"">
                {(diasFaltantes > 0 ? $"Faltan <strong style=\"color:#c8a96e;\">{diasFaltantes} días</strong> para el logro de {proximaMeta} días" : "¡Meta alcanzada!")}
              </p>";

            var client = new SendGridClient(_apiKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress("auroraappoficial@gmail.com", "AURORA"),
                Subject = $"{emoji} {titulo} · AURORA",
                HtmlContent = $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""/></head>
<body style=""margin:0;padding:0;background:#0d0d0d;font-family:Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#0d0d0d;padding:48px 16px;"">
    <tr><td align=""center"">
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
             style=""max-width:520px;background:#161616;border-radius:20px;border:1px solid rgba(255,255,255,0.08);"">

        <!-- Header -->
        <tr>
          <td style=""background:#111;border-bottom:1px solid rgba(255,255,255,0.06);padding:28px 40px;text-align:center;"">
            <div style=""display:inline-block;background:rgba(200,169,110,0.12);border:1px solid rgba(200,169,110,0.25);border-radius:12px;padding:8px 22px;font-size:15px;font-weight:700;letter-spacing:3px;color:#c8a96e;"">
              AURORA
            </div>
          </td>
        </tr>

        <!-- Flame icon -->
        <tr>
          <td align=""center"" style=""padding:36px 40px 0;"">
            <div style=""font-size:56px;line-height:1;"">{emoji}</div>
          </td>
        </tr>

        <!-- Días destacados -->
        <tr>
          <td align=""center"" style=""padding:16px 40px 0;"">
            <div style=""font-size:64px;font-weight:700;color:#c8a96e;line-height:1;font-family:Arial,sans-serif;"">
              {diasRacha}
            </div>
            <div style=""font-size:14px;color:#888;letter-spacing:2px;text-transform:uppercase;margin-top:4px;"">
              {(diasRacha == 1 ? "día consecutivo" : "días consecutivos")}
            </div>
          </td>
        </tr>

        <!-- Título -->
        <tr>
          <td align=""center"" style=""padding:20px 40px 0;"">
            <h1 style=""margin:0;font-size:22px;font-weight:600;color:#ececec;"">{titulo}</h1>
          </td>
        </tr>

        <!-- Mensaje -->
        <tr>
          <td align=""center"" style=""padding:12px 40px 0;"">
            <p style=""margin:0;font-size:15px;color:#888;line-height:1.7;text-align:center;"">
              Hola, <strong style=""color:#ececec;"">{nombre}</strong>. {mensaje}
            </p>
          </td>
        </tr>

        <!-- Barra de progreso -->
        <tr>
          <td style=""padding:24px 40px 0;"">
            {barraProgreso}
          </td>
        </tr>

        <!-- Separador -->
        <tr>
          <td style=""padding:28px 40px;"">
            <div style=""border-top:1px solid rgba(255,255,255,0.06);""></div>
          </td>
        </tr>

        <!-- CTA -->
        <tr>
          <td align=""center"" style=""padding:0 40px 32px;"">
            <p style=""margin:0 0 20px;font-size:14px;color:#666;line-height:1.6;"">
              Recuerda leer mañana para mantener tu racha viva. ¡Cada día cuenta!
            </p>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style=""background:#111;border-top:1px solid rgba(255,255,255,0.06);padding:20px 40px;text-align:center;"">
            <p style=""margin:0;font-size:12px;color:#555;"">
              © 2026 AURORA App · Este correo se envió porque leíste hoy en AURORA.
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>"
            };

            msg.AddTo(new EmailAddress(toEmail));
            await client.SendEmailAsync(msg);
        }
    }
}
