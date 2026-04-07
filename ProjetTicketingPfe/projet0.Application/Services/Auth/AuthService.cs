using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.Otp;
using projet0.Application.Services.Token;
using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IOtpService _otpService;
        private readonly ILogger _logger;
        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService, RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config, IOtpService otpService, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _config = config;
            _otpService = otpService;
            _logger = logger;
        }

        // ================= REGISTER =================
        // Dans Application/Services/Auth/AuthService.cs
        // Dans AuthService.RegisterAsync - Ajouter ces validations
        // Dans AuthService.RegisterAsync - Ajouter ces validations
        public async Task<ApiResponse<AuthResponseDTO>> RegisterAsync(RegisterDTO dto)
        {
            // ✅ 1. Valider le modèle (les annotations sont déjà vérifiées par le contrôleur)
            // Mais on peut ajouter des validations supplémentaires

            // ✅ 2. Vérifier l'unicité de l'email
            var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingEmail != null)
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Cet email est déjà utilisé",
                    resultCode: 10
                );
            }

            // ✅ 3. Vérifier l'unicité du nom d'utilisateur
            var existingUserName = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUserName != null)
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Ce nom d'utilisateur est déjà pris",
                    resultCode: 11
                );
            }

            // ✅ 4. Vérifier l'unicité du numéro de téléphone (si fourni)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var users = _userManager.Users.ToList();
                var existingPhone = users.FirstOrDefault(u => u.PhoneNumber == dto.PhoneNumber);
                if (existingPhone != null)
                {
                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: "Ce numéro de téléphone est déjà utilisé",
                        resultCode: 12
                    );
                }
            }

            // ✅ 5. Vérifier la force du mot de passe (validation supplémentaire)
            var passwordValidator = new PasswordValidator<ApplicationUser>();
            var passwordResult = await passwordValidator.ValidateAsync(_userManager, null, dto.Password);
            if (!passwordResult.Succeeded)
            {
                var errors = passwordResult.Errors.Select(e => e.Description).ToList();
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Le mot de passe ne respecte pas les règles de sécurité",
                    errors: errors,
                    resultCode: 13
                );
            }

            // ✅ 6. Récupérer le rôle "Technicien"
            var role = await _roleManager.FindByNameAsync("Technicien");
            if (role == null)
            {
                role = new IdentityRole<Guid> { Name = "Technicien", NormalizedName = "TECHNICIEN" };
                await _roleManager.CreateAsync(role);
            }

            // ✅ 7. Créer l'utilisateur (EmailConfirmed forcé à false)
            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                Nom = dto.Nom,
                Prenom = dto.Prenom,
                PhoneNumber = dto.PhoneNumber,
                BirthDate = dto.BirthDate,
               
                EmailConfirmed = false  // ✅ Toujours false - l'utilisateur doit confirmer son email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Erreur lors de la création de l'utilisateur",
                    errors: result.Errors.Select(e => e.Description).ToList(),
                    resultCode: 1
                );
            }

            // ✅ 8. Assignation du rôle Technicien
            await _userManager.AddToRoleAsync(user, role.Name);

            // ✅ 9. Envoyer OTP pour confirmer l'email
            var otpResult = await _otpService.GenerateAndSendOtpAsync(
                user,
                OtpPurpose.EmailConfirmation
            );

            if (otpResult.ResultCode != 0)
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Compte créé, mais erreur lors de l'envoi du code de confirmation",
                    resultCode: 2
                );
            }

            return ApiResponse<AuthResponseDTO>.Success(
                data: null,
                message: "Compte technicien créé avec succès. Veuillez confirmer votre email avec le code reçu.",
                resultCode: 0
            );
        }

        // ================= LOGIN =================
        public async Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Email ou mot de passe incorrect",
                    resultCode: 10
                );
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");

            // ============================================
            // ✅ ADMIN : Pas de lockout
            // ============================================
            if (isAdmin)
            {
                // Vérifier le mot de passe
                if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                {
                    _logger.LogWarning("Tentative de connexion admin échouée pour {Email}", dto.Email);
                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: "Email ou mot de passe incorrect",
                        resultCode: 10
                    );
                }

                // Réinitialiser le compteur de tentatives si nécessaire
                if (await _userManager.GetAccessFailedCountAsync(user) > 0)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                }

                // Vérifier si le compte est désactivé (lockout permanent par admin)
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                if (lockoutEnd.HasValue && lockoutEnd.Value == DateTimeOffset.MaxValue)
                {
                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: "Votre compte a été désactivé. Contactez l'administrateur.",
                        resultCode: 15
                    );
                }

                // S'assurer que le statut est Actif
                if (user.Statut != UserStatut.Actif)
                {
                    user.Statut = UserStatut.Actif;
                    await _userManager.UpdateAsync(user);
                }

                // Générer les tokens
                var accessToken = _tokenService.GenerateAccessToken(user, roles);
                var refreshToken = _tokenService.GenerateRefreshToken(user);
                var userRole = roles.FirstOrDefault();

                var response = new AuthResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(
                        int.Parse(_config["Jwt:AccessTokenExpirationMinutes"])
                    ),
                    UserName = user.UserName,
                    Role = userRole,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed
                };

                _logger.LogInformation("Connexion admin réussie pour {Email}", dto.Email);

                return ApiResponse<AuthResponseDTO>.Success(
                    data: response,
                    message: "Connexion réussie",
                    resultCode: 0
                );
            }

            // ============================================
            // ✅ NON-ADMIN : Logique avec lockout
            // ============================================

            // 1. NETTOYER LE LOCKOUT EXPIRÉ (restaure le statut si nécessaire)
            await CleanExpiredLockoutForUserAsync(user);

            // 2. SYNCHRONISER LE STATUT (vérification supplémentaire)
            await SyncUserStatusAsync(user);

            // 3. VÉRIFIER SI L'UTILISATEUR EST ENCORE LOCKOUT
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            if (isLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

                if (lockoutEnd.HasValue && lockoutEnd.Value == DateTimeOffset.MaxValue)
                {
                    // LOCKOUT PERMANENT (désactivation par admin)
                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: "Votre compte a été désactivé. Contactez l'administrateur.",
                        resultCode: 15
                    );
                }
                else
                {
                    // LOCKOUT TEMPORAIRE (3 mauvais mots de passe)
                    var remainingTime = lockoutEnd.HasValue
                        ? (lockoutEnd.Value - DateTimeOffset.UtcNow)
                        : TimeSpan.FromMinutes(15);

                    var minutes = remainingTime.TotalMinutes > 0
                        ? Math.Ceiling(remainingTime.TotalMinutes)
                        : 1;

                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: $"Votre compte est temporairement bloqué. Réessayez dans {minutes} minute(s).",
                        resultCode: 13
                    );
                }
            }

            // TENTER LA CONNEXION
            var signInResult = await _signInManager.PasswordSignInAsync(
                user,
                dto.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                {
                    // L'utilisateur vient d'être lockout - Mettre à jour le statut
                    await UpdateUserStatusAfterLockoutAsync(user);

                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: "Trop de tentatives échouées. Votre compte est temporairement bloqué.",
                        resultCode: 14
                    );
                }
                else if (signInResult.IsNotAllowed)
                {
                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: "Veuillez confirmer votre email avant de vous connecter",
                        resultCode: 11
                    );
                }
                else
                {
                    var failedCount = await _userManager.GetAccessFailedCountAsync(user);
                    var remainingAttempts = Math.Max(0, 3 - failedCount);

                    if (remainingAttempts > 0)
                    {
                        return ApiResponse<AuthResponseDTO>.Failure(
                            message: $"Email ou mot de passe incorrect. Il vous reste {remainingAttempts} tentative(s).",
                            resultCode: 10
                        );
                    }
                    else
                    {
                        return ApiResponse<AuthResponseDTO>.Failure(
                            message: "Email ou mot de passe incorrect.",
                            resultCode: 10
                        );
                    }
                }
            }

            // SUCCÈS - RÉINITIALISER LE COMPTEUR D'ÉCHECS ET LE STATUT
            if (await _userManager.GetAccessFailedCountAsync(user) > 0)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            // S'assurer que le statut est Actif après une connexion réussie
            if (user.Statut != UserStatut.Actif)
            {
                user.Statut = UserStatut.Actif;
                await _userManager.UpdateAsync(user);
            }

            // Vérifier la confirmation d'email
            if (!user.EmailConfirmed)
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Veuillez confirmer votre email avant de vous connecter",
                    resultCode: 11
                );
            }

            // Générer les tokens
            var accessTokenNonAdmin = _tokenService.GenerateAccessToken(user, roles);
            var refreshTokenNonAdmin = _tokenService.GenerateRefreshToken(user);
            var userRoleNonAdmin = roles.FirstOrDefault();

            var responseNonAdmin = new AuthResponseDTO
            {
                AccessToken = accessTokenNonAdmin,
                RefreshToken = refreshTokenNonAdmin,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["Jwt:AccessTokenExpirationMinutes"])
                ),
                UserName = user.UserName,
                Role = userRoleNonAdmin,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed
            };

            _logger.LogInformation("Connexion réussie pour {Email}", dto.Email);

            return ApiResponse<AuthResponseDTO>.Success(
                data: responseNonAdmin,
                message: "Connexion réussie",
                resultCode: 0
            );
        }



        // Méthode utilitaire pour détecter un lockout permanent
        private bool IsPermanentLockout(DateTimeOffset lockoutEnd)
        {
            // Si lockoutEnd est très loin dans le futur (>= 1 an), considérer comme permanent
            return lockoutEnd > DateTimeOffset.UtcNow.AddYears(1);
        }

        // Dans AuthService.cs, ajoutez ces méthodes

        /// <summary>
        /// Synchronise le champ Statut avec LockoutEnd
        /// </summary>
        private async Task SyncUserStatusAsync(ApplicationUser user)
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            // Vérifier si l'utilisateur est actuellement bloqué
            var isCurrentlyLocked = isLockedOut || (lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow);

            if (isCurrentlyLocked)
            {
                if (user.Statut != UserStatut.Inactif)
                {
                    user.Statut = UserStatut.Inactif;
                    await _userManager.UpdateAsync(user);
                }
            }
            else
            {
                // ✅ Si non bloqué, le statut doit être Actif
                if (user.Statut != UserStatut.Actif)
                {
                    user.Statut = UserStatut.Actif;
                    await _userManager.UpdateAsync(user);
                }
            }
        }

        /// <summary>
        /// Met à jour le statut après un lockout (3 tentatives échouées)
        /// </summary>
        private async Task UpdateUserStatusAfterLockoutAsync(ApplicationUser user)
        {
            user.Statut = UserStatut.Inactif;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Utilisateur {UserId} bloqué après 3 tentatives - Statut mis à Inactif", user.Id);
        }

        /// <summary>
        /// Nettoie le lockout expiré et restaure le statut
        /// </summary>
        private async Task CleanExpiredLockoutForUserAsync(ApplicationUser user)
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

            // Nettoyer seulement si lockout temporaire (pas MaxValue) et expiré
            if (lockoutEnd.HasValue &&
                lockoutEnd.Value != DateTimeOffset.MaxValue &&
                lockoutEnd.Value <= DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("Lockout expiré pour l'utilisateur {UserId} - Fin: {LockoutEnd}",
                    user.Id, lockoutEnd.Value);

                // 1. Supprimer le lockout
                await _userManager.SetLockoutEndDateAsync(user, null);

                // 2. Réinitialiser le compteur de tentatives
                await _userManager.ResetAccessFailedCountAsync(user);

                // 3. ✅ RESTAURER LE STATUT À ACTIF
                if (user.Statut != UserStatut.Actif)
                {
                    user.Statut = UserStatut.Actif;
                    var updateResult = await _userManager.UpdateAsync(user);

                    if (updateResult.Succeeded)
                    {
                        _logger.LogInformation("Statut de l'utilisateur {UserId} restauré à Actif", user.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Échec de la mise à jour du statut pour {UserId}", user.Id);
                    }
                }
            }
        }

    }
}