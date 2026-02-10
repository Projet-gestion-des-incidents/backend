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
        public async Task<ApiResponse<AuthResponseDTO>> RegisterAsync(RegisterDTO dto)
        {
            var role = await _roleManager.FindByIdAsync(dto.RoleId);

            if (role == null || role.Name == "Admin")
            {
                return ApiResponse<AuthResponseDTO>.Failure(
                    message: "Rôle invalide",
                    resultCode: 15
                );
            }

            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                Nom = dto.Nom,
                Prenom = dto.Prenom,
                
                PhoneNumber = dto.PhoneNumber, 
                BirthDate = dto.BirthDate,
                EmailConfirmed = false 
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

            // Assignation du rôle par défaut
            await _userManager.AddToRoleAsync(user, role.Name);

            // ENVOYER UN OTP POUR CONFIRMER L'EMAIL
            var otpResult = await _otpService.GenerateAndSendOtpAsync(
                user,
                OtpPurpose.EmailConfirmation
            );

            if (otpResult.ResultCode != 0)
            {
             // Gérer l'erreur d'envoi OTP
             return ApiResponse<AuthResponseDTO>.Failure(
             message: "Compte créé, mais erreur lors de l'envoi du code de confirmation",
             resultCode: 2
                 );
            }

        // NE PAS DONNER DE TOKEN D'ACCÈS IMMÉDIATEMENT
        // L'utilisateur doit d'abord confirmer son email
        return ApiResponse<AuthResponseDTO>.Success(
        data: null,
        message: "Compte créé avec succès. Veuillez confirmer votre email.",
        resultCode: 0
            );
        }

        // ================= LOGIN =================
        /*public async Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
            return ApiResponse<AuthResponseDTO>.Failure(
            message: "Email ou mot de passe incorrect",
            resultCode: 10
                );
            }

            // VÉRIFIER SI L'UTILISATEUR EST LOCKOUT
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            if (isLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var remainingTime = lockoutEnd.HasValue ?
                    (lockoutEnd.Value - DateTimeOffset.UtcNow) : TimeSpan.Zero;

                return ApiResponse<AuthResponseDTO>.Failure(
                    message: $"Compte temporairement bloqué. Réessayez dans {remainingTime.Minutes} minutes.",
                    resultCode: 13
                );
            }

            // TENTER LA CONNEXION
            // UTILISEZ SignInManager.PasswordSignInAsync (cela gère automatiquement le lockout)
            var signInResult = await _signInManager.PasswordSignInAsync(
                user,
                dto.Password,
                isPersistent: false,
                lockoutOnFailure: true); // IMPORTANT: lockoutOnFailure = true
            if (!signInResult.Succeeded)

            {
                // Incrémenter le compteur d'échecs
                await _userManager.AccessFailedAsync(user);

                // Vérifier si l'utilisateur vient d'être lockout
                var newIsLockedOut = await _userManager.IsLockedOutAsync(user);
                if (newIsLockedOut)
                {
                    return ApiResponse<AuthResponseDTO>.Failure(
                        message: "Trop de tentatives échouées. Votre compte est temporairement bloqué.",
                        resultCode: 14
                    );
                }

                // Calculer les tentatives restantes
                var failedCount = await _userManager.GetAccessFailedCountAsync(user);
                var remainingAttempts = 3 - failedCount;

                return ApiResponse<AuthResponseDTO>.Failure(
                    message: $"Email ou mot de passe incorrect. Il vous reste {remainingAttempts} tentative(s).",
                    resultCode: 10
                );
            }

            // RÉINITIALISER LE COMPTEUR D'ÉCHECS EN CAS DE SUCCÈS
            if (await _userManager.GetAccessFailedCountAsync(user) > 0)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            var roles = await _userManager.GetRolesAsync(user);

            
            // VÉRIFIER SI L'EMAIL EST CONFIRMÉ
            if (!user.EmailConfirmed && !roles.Contains("Admin"))
            {
                // Option 1: Refuser le login
                return ApiResponse<AuthResponseDTO>.Failure(
             message: "Veuillez confirmer votre email avant de vous connecter",
             resultCode: 11
                 );

                // Option 2: Renvoyer un OTP pour confirmer
                // var otpResult = await _otpService.GenerateAndSendOtpAsync(
                //     user, 
                //     OtpPurpose.EmailConfirmation
                // );
                // return new ApiResponse<AuthResponseDTO>(
                //     message: "Veuillez confirmer votre email. Un code a été envoyé.",
                //     resultCode: 12
                // );
            }

            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken(user);
            var userRole = roles.FirstOrDefault(); // on prend le premier rôle (ou tu peux gérer plusieurs rôles)

            var response = new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["Jwt:AccessTokenExpirationMinutes"])
                ),
                UserName = user.UserName,
                Role = userRole //  le frontend récupére le rôle

            };
            return ApiResponse<AuthResponseDTO>.Success(
                   data: response,
                   message: "Connexion réussie",
                   resultCode: 0
                       );
        }*/

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