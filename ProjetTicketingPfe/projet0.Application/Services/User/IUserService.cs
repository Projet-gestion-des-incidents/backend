using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Domain.Entities;

namespace projet0.Application.Services.User
{
    public interface IUserService
    {
        Task<ApiResponse<ApplicationUser>> EditProfileAsync(Guid userId, EditProfileDto dto);
        Task<ApiResponse<string>> ActivateAsync(Guid id);
        Task<UserProfileDto> GetMyProfileAsync(Guid userId);
        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        Task<ApiResponse<PagedResult<UserWithRoleDto>>> GetAllUsersWithRolesAsync(PagedRequest request);
        Task<ApplicationUser> GetByIdAsync(Guid id);      
        Task<ApiResponse<ApplicationUser>> UpdateAsync(Guid id, UserDto dto);
        Task<ApiResponse<string>> DesactivateAsync(Guid id);
        Task<ApiResponse<string>> DeleteAsync(Guid id);
        Task<ApiResponse<PagedResult<UserWithRoleDto>>> SearchUsersAsync(UserSearchRequest request);
        Task<IList<string>> GetUserRolesAsync(Guid userId);
        Task<ApiResponse<IEnumerable<TechnicienDto>>> GetTechniciensAsync();
        Task<ApiResponse<ApplicationUser>> CreateTechnicienAsync(CreateTechnicienDto dto);
        Task<ApiResponse<ApplicationUser>> CreateCommercantAsync(CreateCommercantDto dto);
        Task<ApiResponse<PagedResult<TechnicienDto>>> GetTechniciensPagedAsync(TechnicienSearchRequest request);
        Task<ApiResponse<PagedResult<CommercantDto>>> GetCommercantsPagedAsync(CommercantSearchRequest request);
        Task<ApiResponse<ApplicationUser>> EditTechnicienProfileAsync(Guid userId, EditTechnicienProfileDto dto);
        Task<ApiResponse<ApplicationUser>> EditCommercantProfileAsync(Guid userId, EditCommercantProfileDto dto);
        Task<ApiResponse<ApplicationUser>> AdminUpdateTechnicienAsync(Guid userId, AdminUpdateTechnicienDto dto);
        Task<ApiResponse<ApplicationUser>> AdminUpdateCommercantAsync(Guid userId, AdminUpdateCommercantDto dto);
        Task<ApiResponse<TechnicienDto>> GetTechnicienByIdAsync(Guid id);
        Task<ApiResponse<CommercantDto>> GetCommercantByIdAsync(Guid id);
        Task<ApiResponse<ApplicationUser>> EditAdminProfileAsync(Guid userId, EditAdminProfileDto dto);
    }

}
