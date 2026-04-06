using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService, RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config, IOtpService otpService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _config = config;
            _otpService = otpService;
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

            // NETTOYER LE LOCKOUT EXPIRÉ
            await CleanExpiredLockoutForUserAsync(user);

            // VÉRIFIER SI L'UTILISATEUR EST LOCKOUT
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            if (isLockedOut)
            {
                // Distinguer lockout permanent (désactivation admin) vs temporaire (tentatives échouées)
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
                    // Calculer le temps restant
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
                    // L'utilisateur vient d'être lockout par cette tentative
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
                    // Mauvais mot de passe
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

            // SUCCÈS - RÉINITIALISER LE COMPTEUR D'ÉCHECS
            if (await _userManager.GetAccessFailedCountAsync(user) > 0)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!user.EmailConfirmed && !roles.Contains("Admin"))
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Veuillez confirmer votre email avant de vous connecter",
                    resultCode: 11
                );
            }

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

            return ApiResponse<AuthResponseDTO>.Success(
                data: response,
                message: "Connexion réussie",
                resultCode: 0
            );
        }

        // Méthode pour nettoyer le lockout expiré
        private async Task CleanExpiredLockoutForUserAsync(ApplicationUser user)
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

            // Nettoyer seulement si lockout temporaire (pas MaxValue) et expiré
            if (lockoutEnd.HasValue &&
                lockoutEnd.Value != DateTimeOffset.MaxValue &&
                lockoutEnd.Value <= DateTimeOffset.UtcNow)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
                //_logger.LogDebug("Lockout temporaire expiré nettoyé pour {Email}", user.Email);
            }
        }    

        // Méthode utilitaire pour détecter un lockout permanent
        private bool IsPermanentLockout(DateTimeOffset lockoutEnd)
        {
            // Si lockoutEnd est très loin dans le futur (>= 1 an), considérer comme permanent
            return lockoutEnd > DateTimeOffset.UtcNow.AddYears(1);
        }
    }
}