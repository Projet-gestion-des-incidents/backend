using Microsoft.AspNetCore.Identity;
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
        Task<IEnumerable<UserWithRoleDto>> GetAllUsersWithRolesAsync();
        Task<IdentityResult> UpdateAsync(ApplicationUser user);
        Task<IdentityResult> SoftDeleteAsync(ApplicationUser user);
        Task<ApplicationUser> GetByEmailAsync(string email);
        Task<ApplicationUser> GetByUserNameAsync(string userName);
        Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName);
        Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync();
        Task<IEnumerable<ApplicationUser>> SearchUsersAsync(string searchTerm);
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null);
        Task SaveChangesAsync();
        Task<bool> IsUserNameUniqueAsync(string userName, Guid? excludeUserId = null);
        Task<IdentityResult> CreateAsync(ApplicationUser user, string password = null);
        Task<IList<string>> GetUserRolesAsync(Guid userId);
        Task<bool> AddUserToRoleAsync(Guid userId, string roleName);
        Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName);

    }
}
