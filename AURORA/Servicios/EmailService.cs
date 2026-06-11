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
    }
}