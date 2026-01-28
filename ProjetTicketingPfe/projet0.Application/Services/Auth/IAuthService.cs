using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Services.Auth
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDTO>> RegisterAsync(RegisterDTO dto);
        Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO dto);

    }

}
