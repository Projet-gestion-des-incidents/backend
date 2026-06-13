using projet0.Domain.Entities;

namespace projet0.Application.Services.Token
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken(ApplicationUser user);
        Task<string?> RefreshAccessTokenAsync(string refreshToken);
    }
}
