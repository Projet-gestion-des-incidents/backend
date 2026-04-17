using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.Auth;
using projet0.Application.Services.Email;
using projet0.Application.Services.Otp;
using projet0.Application.Services.Token;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System.Security.Claims;

namespace projet0.API.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        private readonly IOtpService _otpService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        public AuthController(
            IAuthService authService,
            ITokenService tokenService,
            IOtpService otpService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _authService = authService;
            _tokenService = tokenService;
            _otpService = otpService;
            _userManager = userManager;
            _emailService = emailService;
        }
       
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            // Vérifier la validité du modèle
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<AuthResponseDTO>.Failure(
                    message: "Données d'inscription invalides",
                    errors: errors,
                    resultCode: 99
                ));
            }

            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDTO dto)
            => Ok(await _authService.LoginAsync(dto));

        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] EmailDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Email))
                return BadRequest(ApiResponse<string>.Failure(
    message: "Email requis",
    errors: null,
    resultCode: 10
));
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(ApiResponse<string>.Failure(
                                   message: "Utilisateur introuvable",
                                   resultCode: 20
                               ));
            var result = await _otpService.GenerateAndSendOtpAsync(user, OtpPurpose.EmailConfirmation);

                return Ok(result);
            }
        
        [HttpPost("validate-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Failure(
                    message: "Utilisateur introuvable",
                    resultCode: 20
                ));
            }

                var result = await _otpService.ValidateOtpAsync(
                user.Id,
                dto.Code,
                OtpPurpose.EmailConfirmation
            );

            if (result.ResultCode == 0 && result.Data)
            {
                return Ok(ApiResponse<AuthResponseDTO>.Success(
                         data: null,
                         message: result.Message,
                         resultCode: 0
                     ));
            }

            return BadRequest(result);
        }

        [HttpPost("sign-out")]
        [Authorize]
        public IActionResult SignOut()
        {
            return Ok(ApiResponse<string>.Success(
                message: "Déconnexion réussie !"
            ));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPassword dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Failure(
                     message: "Utilisateur introuvable",
                     resultCode: 40
                 ));
            }

            var result = await _otpService.GenerateAndSendOtpAsync(
                user,
                OtpPurpose.ResetPassword
            );

            return result.ResultCode == 0 || result.ResultCode == 1
                   ? Ok(result)
                   : BadRequest(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            // Valider le modèle
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Failure(
                    message: "Données invalides",
                    errors: ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(),
                    resultCode: 99));
            }

            // Vérifier que les mots de passe correspondent
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest(ApiResponse<string>.Failure(
                    message: "Les mots de passe ne correspondent pas",
                    resultCode: 42));
            }

            // Vérifier la force du mot de passe
            if (dto.NewPassword.Length < 6)
            {
                return BadRequest(ApiResponse<string>.Failure(
                    message: "Le mot de passe doit contenir au moins 6 caractères",
                    resultCode: 43));
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Failure(
                    message: "Utilisateur introuvable",
                    resultCode: 40));
            }

            // Valider l'OTP
            var otpValid = await _otpService.ValidateOtpAsync(
                user.Id,
                dto.OtpCode,
                OtpPurpose.ResetPassword
            );

            if (otpValid.ResultCode != 0)
            {
                return BadRequest(ApiResponse<string>.Failure(
                    message: otpValid.Message!,
                    resultCode: otpValid.ResultCode));
            }

            // Réinitialiser le mot de passe
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.Failure(
                    message: "Erreur lors de la réinitialisation",
                    errors: result.Errors.Select(e => e.Description).ToList(),
                    resultCode: 41));
            }

            // Optionnel : Déconnecter toutes les sessions actives
            await _userManager.UpdateSecurityStampAsync(user);

            return Ok(ApiResponse<string>.Success(
                data: null,
                message: "Mot de passe réinitialisé avec succès. Vous pouvez maintenant vous connecter.",
                resultCode: 0));
        }

        [HttpPost("confirm-email-change")]
        [Authorize]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(ApiResponse<string>.Failure(
    message: "Utilisateur introuvable",
    errors: null,
    resultCode: 40
));

            // Valider l'OTP
            var otpValid = await _otpService.ValidateOtpAsync(
                user.Id,
                dto.OtpCode,
                OtpPurpose.EmailChange
            );

            if (otpValid.ResultCode != 0)
            {
                return BadRequest(ApiResponse<string>.Failure(
    message: otpValid.Message,
    errors: null,
    resultCode: otpValid.ResultCode));
            }

            // Vérifier que le nouvel email n'est pas déjà utilisé
            var existingUser = await _userManager.FindByEmailAsync(dto.NewEmail);
            if (existingUser != null && existingUser.Id != userId)
            {
                return BadRequest(ApiResponse<string>.Failure(
    message: "Email requis",
    resultCode: 10
));
            }

            // Changer l'email
            user.Email = dto.NewEmail;
            user.NormalizedEmail = dto.NewEmail.ToUpper();
            user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.Failure(
    message: "Erreur lors du changement d'email",
    errors: null,
    resultCode: 99));
            }

            return Ok(ApiResponse<string>.Success(
                message: $"Email changé avec succès vers {dto.NewEmail}. Veuillez vous reconnecter.",
                resultCode: 0));
        }

        [HttpPost("confirm-password-change")]
        [Authorize]
        public async Task<IActionResult> ConfirmPasswordChange([FromBody] ConfirmPasswordChangeDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(ApiResponse<string>.Failure(
    message: "Utilisateur introuvable",
    errors: null,
    resultCode: 20
));

            // Valider l'OTP
            var otpValid = await _otpService.ValidateOtpAsync(
                user.Id,
                dto.OtpCode,
                OtpPurpose.ResetPassword
            );

            if (otpValid.ResultCode != 0)
            {
                return BadRequest(ApiResponse<string>.Failure(
                    message: otpValid.Message,
                    errors: null,
                    resultCode: otpValid.ResultCode));
            }

            // Changer le mot de passe
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.Failure(
    message: "Erreur lors du changement de mot de passe",
    errors: null,
    resultCode: 21));
            }

            // Envoyer confirmation par email
            await _emailService.SendPasswordChangeConfirmationAsync(user.Email);

            return Ok(ApiResponse<string>.Success(
                message: "Mot de passe changé avec succès. Veuillez vous reconnecter.",
                resultCode: 0));
        }
    }
}