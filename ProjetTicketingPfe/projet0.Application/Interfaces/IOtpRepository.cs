using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Interfaces
{
    public interface IOtpRepository
    {
        Task AddAsync(OtpCode otp);
        Task<OtpCode> GetValidOtpAsync(Guid userId, string code, OtpPurpose purpose);
        Task UpdateAsync(OtpCode otp);

    }

}
