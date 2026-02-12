using Microsoft.AspNetCore.Identity;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface IUserRepository : IGenericRepository<ApplicationUser>
    {
        Task<IdentityResult> RestoreAsync(ApplicationUser user);
        Task<PagedResult<UserWithRoleDto>> GetAllUsersWithRolesAsync(PagedRequest request);
        Task<IdentityResult> UpdateAsync(ApplicationUser user);
        Task<IdentityResult> SoftDeleteAsync(ApplicationUser user);
        Task<ApplicationUser> GetByEmailAsync(string email);
        Task<ApplicationUser> GetByUserNameAsync(string userName);
        Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName);
        Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync();
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null);
        //Task SaveChangesAsync();
        Task<int> SaveChangesAsync();
        Task<bool> IsUserNameUniqueAsync(string userName, Guid? excludeUserId = null);
        Task<IdentityResult> CreateAsync(ApplicationUser user, string password = null);
        Task<IList<string>> GetUserRolesAsync(Guid userId);
        Task<bool> AddUserToRoleAsync(Guid userId, string roleName);
        Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName);
        Task<(IEnumerable<UserWithRoleDto> Users, int TotalCount)> SearchUsersAsync(
            UserSearchRequest request);
    }
}
