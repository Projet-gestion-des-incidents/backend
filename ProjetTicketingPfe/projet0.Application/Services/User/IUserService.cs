using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace projet0.Application.Services.User
{
    public interface IUserService
    {
        Task<ApiResponse<string>> ActivateAsync(Guid id);

        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        Task<IEnumerable<UserWithRoleDto>> GetAllUsersWithRolesAsync();
        Task<ApplicationUser> GetByIdAsync(Guid id);
        Task<ApiResponse<ApplicationUser>> CreateAsync(UserDto dto);
        Task<ApiResponse<ApplicationUser>> UpdateAsync(Guid id, UserDto dto);
        Task<ApiResponse<string>> DeleteAsync(Guid id);
    }

}
