using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.Email;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Services.Otp
{
    public class OtpService : IOtpService
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        public OtpService(
            IOtpRepository otpRepository,
            IEmailService emailService,
            IUserRepository userRepository)
        {
            _otpRepository = otpRepository;
            _emailService = emailService;
            _userRepository = userRepository;
        }
        public async Task<ApiResponse<string>> GenerateAndSendOtpAsync(
    ApplicationUser user,
    OtpPurpose purpose)
        {
            try
            {
                var code = new Random().Next(100000, 999999).ToString();

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

                // Logique d'envoi d'email
              
                    // COMMENTÉ POUR TEST - À décommenter en production
                    // emailSent = await _emailService.SendAsync(
                    //     user.Email,
                    //     "Votre code OTP",
                    //     $"Votre code est : {code} (valide 5 minutes)"
                    // );


                    try
                    {
                        // Simulation en dev
                        bool emailSent = true;
                        // PROD :
                        // bool emailSent = await _emailService.SendAsync(...);

                        return emailSent
                            ? ApiResponse<string>.Success(
                                data: code, //  uniquement en DEV
                                message: "Code OTP généré et envoyé avec succès",
                                resultCode: 0
                            )
                            : ApiResponse<string>.Success(
                                data: code,
                                message: "Code OTP généré mais email non envoyé",
                                resultCode: 1
                            );
                    }
                    catch (Exception emailEx)
                    {
                        return ApiResponse<string>.Failure(
                            message: "Code OTP généré mais erreur lors de l'envoi de l'email",
                            errors: new List<string> { emailEx.Message },
                            resultCode: 2
                        );
                    }
                }
                catch (Exception ex)
                {
                    return ApiResponse<string>.Failure(
                        message: "Erreur lors de la génération de l'OTP",
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
                var otp = await _otpRepository
                    .GetValidOtpAsync(userId, code, purpose);

                if (otp == null)
                {
                    return ApiResponse<bool>.Failure(
               message: "OTP invalide, expiré ou déjà utilisé",
               resultCode: 30);
                }

                // Vérifier si le code a expiré
                if (otp.ExpireAt < DateTime.UtcNow)
                {
                    return ApiResponse<bool>.Failure(
               message: "Le code OTP a expiré",
               resultCode: 31
           );
                }

                // Vérifier si le code a déjà été utilisé
                if (otp.Status == OtpStatus.Consumed)
                {
                    return ApiResponse<bool>.Failure(
                 message: "Ce code OTP a déjà été utilisé",
                 resultCode: 32 // Code d'erreur pour OTP déjà utilisé
                     );
                }

                // Marquer le code comme consommé
                otp.Status = OtpStatus.Consumed;
                await _otpRepository.UpdateAsync(otp);

                // SI C'EST POUR LA CONFIRMATION D'EMAIL, METTRE EmailConfirmed = true
                if (purpose == OtpPurpose.EmailConfirmation)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        user.EmailConfirmed = true;
                        await _userRepository.UpdateAsync(user);
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
                return ApiResponse<bool>.Failure(
                    message: "Erreur lors de la validation de l'OTP",
                    errors: new List<string> { ex.Message },
                    resultCode: 99
                );
            }
        }

    }
}
