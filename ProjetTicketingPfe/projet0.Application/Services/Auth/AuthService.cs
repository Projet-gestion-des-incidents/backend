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
        private readonly ITokenService _tokenService;
        private readonly IOtpService _otpService;
        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService, RoleManager<IdentityRole<Guid>> roleManager
,
            IConfiguration config, IOtpService otpService)
        {
            _userManager = userManager;
            _roleManager = roleManager;

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
                Age = dto.Age,
                EmailConfirmed = false // Explicitement false à l'inscription
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
        public async Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
            return ApiResponse<AuthResponseDTO>.Failure(
            message: "Email ou mot de passe incorrect",
            resultCode: 10
                );
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
                Role = userRole // <-- maintenant le frontend pourra récupérer le rôle

            };
            return ApiResponse<AuthResponseDTO>.Success(
                   data: response,
                   message: "Connexion réussie",
                   resultCode: 0
                       );
        }

    }
}