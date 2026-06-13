using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;

namespace projet0.Application.Services.Auth
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDTO>> RegisterAsync(RegisterDTO dto);
        Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO dto);

    }

}
