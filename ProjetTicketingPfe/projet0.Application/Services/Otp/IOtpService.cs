using projet0.Application.Commun.Ressources;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

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

        }
}
