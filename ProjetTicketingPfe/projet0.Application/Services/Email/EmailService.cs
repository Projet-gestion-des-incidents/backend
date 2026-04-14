using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using projet0.Application.Commun.DTOs;
using System.Threading.Tasks;

namespace projet0.Application.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Tentative d'envoi d'email à {To} via {Host}:{Port}",
                    to, _settings.Host, _settings.Port);

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_settings.From));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();

                // Connexion au serveur Gmail
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);

                // Authentification avec le mot de passe d'application
                await smtp.AuthenticateAsync(_settings.Username, _settings.Password);

                // Envoi
                await smtp.SendAsync(email);

                // Déconnexion
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("✅ Email envoyé avec succès à {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur envoi email à {To}: {Message}", to, ex.Message);
                throw;
            }
        }
    }
}
