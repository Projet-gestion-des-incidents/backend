using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Services.Token
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken(ApplicationUser user);
        Task<string?> RefreshAccessTokenAsync(string refreshToken);
    }
}
