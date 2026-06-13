using Microsoft.Extensions.Logging;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.Email;
using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Services.Otp
{
    public class OtpService : IOtpService
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OtpService> _logger;

        public OtpService(
            IOtpRepository otpRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            ILogger<OtpService> logger)
        {
            _otpRepository = otpRepository;
            _emailService = emailService;
            _userRepository = userRepository;
            _logger = logger;
        }
        public async Task<ApiResponse<string>> GenerateAndSendOtpAsync(
    ApplicationUser user,
    OtpPurpose purpose)
        {
            try
            {
                var code = new Random().Next(100000, 999999).ToString();

                _logger.LogInformation(" Génération OTP pour {Email} - Code: {Code} - Purpose: {Purpose}",
                    user.Email, code, purpose);

                var otp = new OtpCode
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Code = code,
                    CreatedAt = DateTime.UtcNow,
                    ExpireAt = DateTime.UtcNow.AddMinutes(5),
                    Status = OtpStatus.Generated,
                    Purpose = purpose
                };

                await _otpRepository.AddAsync(otp);

                _logger.LogInformation(" OTP sauvegardé en base pour {Email} (ID: {OtpId})", user.Email, otp.Id);

                // 3. Envoyer l'email RÉELLEMENT
                bool emailSent = false;
                string emailErrorMessage = null;

                try
                {
                    // Préparer le sujet et le corps selon le purpose
                    string subject = purpose == OtpPurpose.EmailConfirmation
                        ? "Confirmation de votre inscription - Code OTP"
                        : "Réinitialisation de votre mot de passe - Code OTP";

                    string body;
                    if (purpose == OtpPurpose.EmailConfirmation)
                    {
                        body = $@"
Bonjour {user.Prenom} {user.Nom},

Merci pour votre inscription sur la plateforme TicketTracker de MS Solutions Group.

** Votre code de confirmation : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 Code OTP : {code}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Informations importantes : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Ce code est valable 5 MINUTES.
• Ce code est à USAGE UNIQUE.
• Ne partagez JAMAIS ce code.
• Si vous n'avez pas créé de compte, veuillez CONTACTER notre support technique IMMÉDIATEMENT.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Procédure à suivre : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  1. Retournez sur la plateforme TicketTracker.
  2. Saisissez ce code de confirmation.
  3. Votre compte sera activé automatiquement.
  4. Vous pourrez ensuite vous connecter avec vos identifiants.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Contact Support : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pour toute assistance, n'hésitez pas à contacter notre support technique :

  Email : support@mssolutionsgroup.com
  Téléphone : +216 71 715 001
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cordialement,
L'équipe MS Solutions Group";
                    }
                    else // ResetPassword
                    {
                        body = $@"
Bonjour {user.Prenom} {user.Nom},

Nous avons reçu une demande de réinitialisation de votre mot de passe sur la plateforme TicketTracker de MS Solutions Group.

** Votre code de vérification : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 Code OTP : {code}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Informations importantes : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Ce code est valable 5 MINUTES.
• Ce code est à USAGE UNIQUE.
• Ne partagez JAMAIS ce code.
• Si vous n'êtes pas à l'origine de cette demande, veuillez CONTACTER notre support technique IMMÉDIATEMENT.
• Votre mot de passe restera INCHANGÉ tant que vous n'aurez pas validé ce code.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Procédure à suivre : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  1. Retournez sur la plateforme TicketTracker.
  2. Saisissez ce code de vérification.
  3. Créez votre nouveau mot de passe sécurisé.
  4. Confirmez votre nouveau mot de passe.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Recommandations de sécurité : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Utilisez un mot de passe UNIQUE et COMPLEXE.
• Minimum 8 caractères avec lettres majuscules, minuscules, chiffres et symboles.
• Ne réutilisez PAS vos anciens mots de passe.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Contact Support : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pour toute assistance ou en cas de doute, n'hésitez pas à contacter notre support technique :

  Email : support@mssolutionsgroup.com
  Téléphone : +216 71 715 001
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cordialement,
L'équipe MS Solutions Group";
                    }

                    // ENVOI RÉEL DE L'EMAIL
                    await _emailService.SendAsync(user.Email, subject, body);
                    emailSent = true;
                    _logger.LogInformation("📧 Email OTP envoyé avec succès à {Email}", user.Email);
                }
                catch (Exception emailEx)
                {
                    emailErrorMessage = emailEx.Message;
                    _logger.LogError(emailEx, "❌ Échec envoi email OTP à {Email}", user.Email);
                }

                // 4. Retourner la réponse 
                if (emailSent)
                {
                    return ApiResponse<string>.Success(
                        data: code,  
                        message: "Code OTP généré et envoyé avec succès par email",
                        resultCode: 0
                    );
                }
                else
                {
                    // Email non envoyé mais code généré - utile pour les tests
                    return ApiResponse<string>.Success(
                        data: code,
                        message: $" Code OTP généré mais email non envoyé. Code: {code} (valide 5 min). Erreur: {emailErrorMessage}",
                        resultCode: 1
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Erreur lors de la génération de l'OTP pour {Email}", user?.Email);
                return ApiResponse<string>.Failure(
                    message: "Erreur lors de la génération du code OTP",
                    errors: new List<string> { ex.Message },
                    resultCode: 99
                );
            }
        }

        public async Task<ApiResponse<bool>> ValidateOtpAsync(
            Guid userId,
            string code,
            OtpPurpose purpose)
        {
            try
            {
                _logger.LogInformation(" Validation OTP pour UserId: {UserId}, Code: {Code}, Purpose: {Purpose}",
                    userId, code, purpose);

                var otp = await _otpRepository
                    .GetValidOtpAsync(userId, code, purpose);

                if (otp == null)
                {
                    _logger.LogWarning(" OTP invalide ou inexistant pour UserId: {UserId}, Code: {Code}", userId, code);
                    return ApiResponse<bool>.Failure(
               message: "OTP invalide, expiré ou déjà utilisé",
               resultCode: 30);
                }

                // Vérifier si le code a expiré
                if (otp.ExpireAt < DateTime.UtcNow)
                {
                    _logger.LogWarning(" OTP expiré pour UserId: {UserId}, ExpireAt: {ExpireAt}", userId, otp.ExpireAt);
                    return ApiResponse<bool>.Failure(
               message: "Le code OTP a expiré",
               resultCode: 31
           );
                }

                // Vérifier si le code a déjà été utilisé
                if (otp.Status == OtpStatus.Consumed)
                {
                    _logger.LogWarning(" OTP déjà utilisé pour UserId: {UserId}", userId);
                    return ApiResponse<bool>.Failure(
                 message: "Ce code OTP a déjà été utilisé",
                 resultCode: 32 // Code d'erreur pour OTP déjà utilisé
                     );
                }

                // Marquer le code comme consommé
                otp.Status = OtpStatus.Consumed;
                await _otpRepository.UpdateAsync(otp);
                _logger.LogInformation(" OTP marqué comme consommé pour UserId: {UserId}", userId);
                // SI C'EST POUR LA CONFIRMATION D'EMAIL, METTRE EmailConfirmed = true
                if (purpose == OtpPurpose.EmailConfirmation)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        if (!user.EmailConfirmed)
                        {
                            user.EmailConfirmed = true;
                            await _userRepository.UpdateAsync(user);
                            _logger.LogInformation(" Email confirmé pour l'utilisateur {UserId} ({Email})", userId, user.Email);
                        }
                        else
                        {
                            _logger.LogInformation(" Email déjà confirmé pour l'utilisateur {UserId}", userId);
                        }
                    }
                }

                return ApiResponse<bool>.Success(
         data: true,
         message: "OTP validé avec succès",
         resultCode: 0
     );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Erreur lors de la validation OTP pour UserId: {UserId}", userId);
                return ApiResponse<bool>.Failure(
                    message: "Erreur lors de la validation de l'OTP",
                    errors: new List<string> { ex.Message },
                    resultCode: 99
                );
            }
        }

        public async Task<ApiResponse<string>> GenerateAndSendOtpToEmailAsync(
            ApplicationUser user,
            string targetEmail,  // Email cible 
            OtpPurpose purpose)
        {
            try
            {
                var code = new Random().Next(100000, 999999).ToString();

                _logger.LogInformation(" Génération OTP pour {TargetEmail} (User: {UserEmail}) - Code: {Code} - Purpose: {Purpose}",
                    targetEmail, user.Email, code, purpose);

                // Sauvegarder en base avec l'ID utilisateur
                var otp = new OtpCode
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Code = code,
                    CreatedAt = DateTime.UtcNow,
                    ExpireAt = DateTime.UtcNow.AddMinutes(5),
                    Status = OtpStatus.Generated,
                    Purpose = purpose
                };

                await _otpRepository.AddAsync(otp);
                _logger.LogInformation(" OTP sauvegardé en base pour UserId: {UserId}", user.Id);

                // Envoyer l'email à l'adresse cible
                string subject = purpose == OtpPurpose.EmailChange
                    ? "Confirmation de changement d'email"
                    : "Votre code OTP";

                // Code corrigé
                string body;
                if (purpose == OtpPurpose.EmailChange)
                {
                    body = $@"
Bonjour {user.Prenom} {user.Nom},

Vous avez demandé à changer votre adresse email sur la plateforme TicketTracker de MS Solutions Group.

** Votre code de vérification : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 Code OTP : {code}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Informations importantes : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Ce code est valable 5 MINUTES.
• Ce code est à USAGE UNIQUE.
• Si vous n'êtes pas à l'origine de cette demande, veuillez CONTACTER notre support technique IMMÉDIATEMENT .
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Procédure à suivre : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  1. Retournez sur la plateforme TicketTracker.
  2. Saisissez ce code de vérification.
  3. Votre adresse email sera automatiquement mise à jour.
  4. Vous devrez vous reconnecter avec votre nouvelle adresse.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Contact Support : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pour toute assistance, n'hésitez pas à contacter notre support technique :

  Email : support@mssolutionsgroup.com
  Téléphone : +216 71 715 001
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cordialement,
L'équipe MS Solutions Group";
                }
                else
                {
                    body = $@"
Bonjour {user.Prenom} {user.Nom},

Vous avez demandé à modifier votre mot de passe sur la plateforme TicketTracker de MS Solutions Group.

** Votre code de vérification : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 Code OTP : {code}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Informations importantes : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Ce code est valable 5 MINUTES.
• Ce code est à USAGE UNIQUE.
• Si vous n'êtes pas à l'origine de cette demande, veuillez CONTACTER notre support technique IMMÉDIATEMENT.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Procédure à suivre : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  1. Retournez sur la plateforme TicketTracker.
  2. Saisissez ce code de vérification.
  3. Créez votre nouveau mot de passe sécurisé.
  4. Confirmez votre nouveau mot de passe.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

** Contact Support : **
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pour toute assistance, n'hésitez pas à contacter notre support technique :

  Email : support@mssolutionsgroup.com
  Téléphone : +216 71 715 001
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cordialement,
L'équipe MS Solutions Group";
                }

                await _emailService.SendAsync(targetEmail, subject, body);
                return ApiResponse<string>.Success(
                    data: code,
                    message: $"Code OTP envoyé à {targetEmail}",
                    resultCode: 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Erreur lors de la génération de l'OTP pour {TargetEmail}", targetEmail);
                return ApiResponse<string>.Failure(
                    message: "Erreur lors de la génération du code OTP",
                    resultCode: 99);
            }
        }
    }
}
