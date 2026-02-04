using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.Auth;
using projet0.Application.Services.Otp;
using projet0.Application.Services.Token;
using projet0.Domain.Entities;
using projet0.Domain.Enums;

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


        public AuthController(
            IAuthService authService,
            ITokenService tokenService,
            IOtpService otpService,
            UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _tokenService = tokenService;
            _otpService = otpService;
            _userManager = userManager;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDTO dto)
            => Ok(await _authService.RegisterAsync(dto));

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDTO dto)
            => Ok(await _authService.LoginAsync(dto));

        //[HttpPost("send-otp")]
        //[AllowAnonymous]
        //public async Task<IActionResult> SendOtp([FromBody] EmailDTO dto)
        //{
        //    if (string.IsNullOrEmpty(dto.Email))
        //        return BadRequest("Email requis");

        //    var user = await _userManager.FindByEmailAsync(dto.Email);
        //    if (user == null)
        //        return NotFound(new ApiResponse<string>("Utilisateur introuvable", null, 20));

        //    await _otpService.GenerateAndSendOtpAsync(user, OtpPurpose.EmailConfirmation);

        //    // RETOURNER ApiResponse AU LIEU D'UNE CHAÎNE SIMPLE
        //    return Ok(new ApiResponse<string>(
        //        data: "OTP envoyé avec succès",
        //        message: "Un code de vérification a été envoyé à votre adresse email",
        //        resultCode: 0
        //    ));
        //}

        //[HttpPost("validate-otp")]
        //[AllowAnonymous]
        //public async Task<IActionResult> ValidateOtp(ValidateOtpDTO dto)
        //{
        //    var user = await _userManager.FindByEmailAsync(dto.Email);
        //    if (user == null)
        //        return NotFound("Utilisateur introuvable");

        //    var isValid = await _otpService.ValidateOtpAsync(
        //        user.Id,
        //        dto.Code,
        //        OtpPurpose.EmailConfirmation
        //    );

        //    if (!isValid)
        //        return BadRequest(new ApiResponse<string>("OTP invalide ou expiré", null, 30));
        //    user.EmailConfirmed = true;
        //    await _userManager.UpdateAsync(user);

        //    return Ok(new ApiResponse<string>(
        //                  data: "OTP validé",
        //                  message: "OTP validé avec succès. Vous êtes maintenant connecté.",
        //                  resultCode: 0
        //              ));
        //}        

        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] EmailDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Email))
                return BadRequest(ApiResponse<string>.Failure(
                                    message: "Email requis",
                                    resultCode: 10
                                ));
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(ApiResponse<string>.Failure(
                                   message: "Utilisateur introuvable",
                                   resultCode: 20
                               ));
            var result = await _otpService.GenerateAndSendOtpAsync(user, OtpPurpose.EmailConfirmation);

            // Retourner directement l'ApiResponse du service
           
                // Pour les codes d'avertissement (comme email non envoyé), on peut toujours retourner 200
                // mais avec un resultCode différent pour informer le frontend
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
                //// OTP validé avec succès
                //user.EmailConfirmed = true;
                //await _userManager.UpdateAsync(user);

                //// Générer le token JWT
                //var token = _tokenService.GenerateAccessToken(user , "User");
                //var refreshToken = _tokenService.GenerateRefreshToken();

                //var authResponse = new AuthResponseDTO
                //{
                //    AccessToken = token,
                //    RefreshToken = refreshToken,
                //    ExpiresAt = DateTime.UtcNow.AddHours(2),
                //    Email = user.Email,
                //    UserName = user.UserName,
                //    Role = user.Role,
                //    EmailConfirmed = user.EmailConfirmed
                //};

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
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Failure(
                     message: "Utilisateur introuvable",
                     resultCode: 40
                 ));
            }

            var otpValid = await _otpService.ValidateOtpAsync(
                user.Id,
                dto.OtpCode,
                OtpPurpose.ResetPassword
            );

            if (otpValid.ResultCode != 0)
            {
                return BadRequest(ApiResponse<string>.Failure(
                     message: otpValid.Message!,
                     resultCode: otpValid.ResultCode
                 ));
            }

            // Générer token Identity
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                user,
                resetToken,
                dto.NewPassword
            );

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.Failure(
                    message: "Erreur lors de la réinitialisation du mot de passe",
                    errors: result.Errors.Select(e => e.Description).ToList(),
                    resultCode: 41
                ));
            }

            return Ok(ApiResponse<string>.Success(
                data: "Mot de passe réinitialisé avec succès",
                message: "Mot de passe réinitialisé avec succès",
                resultCode: 0
            ));
        }
    }
}