using System.Net;
using System.Net.Mail;

namespace sgspe.Services.Notifications;

/// <summary>
/// Envio opcional por SMTP. Só é ativado quando Smtp:Host está configurado; caso contrário
/// as notificações ficam apenas no sistema.
/// </summary>
public class EmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config["Smtp:Host"]);

    public async Task<bool> SendAsync(string to, string subject, string body)
    {
        if (!IsConfigured) return false;
        try
        {
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            using var client = new SmtpClient(_config["Smtp:Host"], port) { EnableSsl = _config.GetValue("Smtp:EnableSsl", true) };
            var user = _config["Smtp:User"];
            if (!string.IsNullOrEmpty(user)) client.Credentials = new NetworkCredential(user, _config["Smtp:Password"]);
            var from = _config["Smtp:From"] ?? user ?? "no-reply@sgspe.local";
            using var msg = new MailMessage(from, to, subject, body);
            await client.SendMailAsync(msg);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de notificação para {To}", to);
            return false;
        }
    }
}
