using projet0.Application.Commun.Ressources;
using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Services.Otp
{
        public interface IOtpService
        {
            Task<ApiResponse<string>> GenerateAndSendOtpAsync(
                ApplicationUser user,
                OtpPurpose purpose);

            Task<ApiResponse<bool>> ValidateOtpAsync(
                Guid userId,
                string code,
                OtpPurpose purpose);

        Task<ApiResponse<string>> GenerateAndSendOtpToEmailAsync(
            ApplicationUser user,
            string targetEmail,
            OtpPurpose purpose);
    }
}
