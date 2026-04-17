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

        // Méthode d'envoi d'email de bienvenue avec mot de passe
        public async Task SendWelcomeEmailAsync(string to, string nom, string prenom, string defaultPassword)
        {
            try
            {
                var subject = "🎉 Bienvenue sur notre plateforme - Vos identifiants de connexion";

                var body = $@"
Bonjour {prenom} {nom},

Votre compte a été créé avec succès par l'administrateur.

🔐 **Vos identifiants de connexion :**
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📧 Email : {to}
🔑 Mot de passe temporaire : {defaultPassword}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ **Important :**
• Ce mot de passe est temporaire
• Veuillez le changer dès votre première connexion
• Ne partagez jamais votre mot de passe

🔗 **Lien de connexion :** [URL de votre application]

Pour des raisons de sécurité, ce lien expirera dans 24 heures.

Cordialement,
L'équipe d'administration
";

                await SendAsync(to, subject, body);
                _logger.LogInformation("Email de bienvenue envoyé à {Email} avec mot de passe temporaire", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de bienvenue à {Email}", to);
                throw;
            }
        }

        public async Task SendPasswordChangeConfirmationAsync(string email)
        {
            var subject = "🔐 Votre mot de passe a été modifié";
            var body = $@"
Bonjour,

Votre mot de passe a été changé avec succès.

Si vous n'êtes pas à l'origine de ce changement, contactez immédiatement l'administrateur.

Cordialement,
L'équipe technique";

            await SendAsync(email, subject, body);
        }
    }
}
