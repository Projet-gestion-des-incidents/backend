using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using projet0.Application.Commun.DTOs;

namespace projet0.Application.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private const string COMPANY_NAME = "MS Solutions Group";

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
                email.From.Add(new MailboxAddress(COMPANY_NAME, _settings.From));
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

        //envoi d'email de bienvenue avec mot de passe
        public async Task SendWelcomeEmailAsync(string to, string nom, string prenom, string defaultPassword)
        {
            try
            {
                var subject = "MS Solutions Group - Vos identifiants de connexion";

                var body = $@"
Bonjour {prenom} {nom},

Votre compte a été créé avec succès sur la plateforme TicketTracker de MS Solutions Group.

** Vos identifiants de connexion : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 Email : {to}
 Mot de passe temporaire : {defaultPassword}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Instructions importantes : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Ce mot de passe est TEMPORAIRE.
• Veuillez le CHANGER dès votre première connexion.
• Ne communiquez JAMAIS vos identifiants.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Procédure de connexion : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  1. Accédez a la plateforme.
  2. Saisissez votre email et votre mot de passe temporaire.
  3. Créez un nouveau mot de passe sécurisé.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Contact Support : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pour toute assistance, n'hésitez pas à contacter notre support technique : 

  Email : support@mssolutionsgroup.com
  Téléphone : +216 71 715 001     
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━


Cordialement,
L'équipe MS Solutions Group
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
            var subject = "MS Solutions Group - Confirmation de modification du mot de passe";
            var body = $@"
Bonjour,

Votre mot de passe a été modifié avec succès sur la plateforme TicketTracker de MS Solutions Group.

** Informations importantes : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Ce changement a été effectué sur votre compte.
• Si vous ne reconnaissez PAS cette modification, veuillez CONTACTER notre support technique IMMÉDIATEMENT.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Recommandations de sécurité : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Utilisez un mot de passe UNIQUE et COMPLEXE.
• Ne réutilisez PAS vos anciens mots de passe.
• Ne communiquez JAMAIS vos identifiants.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Contact Support : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pour toute assistance ou en cas de doute, n'hésitez pas à contacter notre support technique :

  Email : support@mssolutionsgroup.com
  Téléphone : +216 71 715 001
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cordialement,
L'équipe MS Solutions Group";

            await SendAsync(email, subject, body);
        }
    }
}
