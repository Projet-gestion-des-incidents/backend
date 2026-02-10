using projet0.Application.Common.Models.Pagination;
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
        Task<ApiResponse<ApplicationUser>> EditProfileAsync(Guid userId, EditProfileDto dto);
        Task<ApiResponse<string>> ActivateAsync(Guid id);
        Task<UserProfileDto> GetMyProfileAsync(Guid userId);
        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        Task<IEnumerable<UserWithRoleDto>> GetAllUsersWithRolesAsync();
        Task<ApplicationUser> GetByIdAsync(Guid id);
        Task<ApiResponse<ApplicationUser>> CreateAsync(UserDto dto);
        Task<ApiResponse<ApplicationUser>> UpdateAsync(Guid id, UserDto dto);
        Task<ApiResponse<string>> DesactivateAsync(Guid id);
        Task<ApiResponse<string>> DeleteAsync(Guid id);
        Task<ApiResponse<IEnumerable<UserWithRoleDto>>> SearchUsersAsync(string searchTerm);
        Task<ApiResponse<PagedResult<UserWithRoleDto>>> SearchUsersAsync(UserSearchRequest request);

    }

}
