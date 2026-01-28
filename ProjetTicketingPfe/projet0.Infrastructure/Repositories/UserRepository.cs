using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projet0.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
            : base(context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public Task<ApplicationUser> GetByEmailAsync(string email) => _userManager.FindByEmailAsync(email);

        public Task<ApplicationUser> GetByUserNameAsync(string userName) => _userManager.FindByNameAsync(userName);
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password) => await _userManager.CreateAsync(user, password);

        public async Task<IdentityResult> DeleteAsync(ApplicationUser user)
        {
            return await _userManager.DeleteAsync(user);
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users;
        }

        public async Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync()
        {
            return await _dbSet.OrderBy(u => u.Nom).ThenBy(u => u.Prenom).ToListAsync();
        }     

        public async Task<IEnumerable<ApplicationUser>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            searchTerm = searchTerm.ToLower();
            return await _dbSet
                .Where(u => u.Nom.ToLower().Contains(searchTerm) ||
                            u.Prenom.ToLower().Contains(searchTerm) ||
                            u.Email.ToLower().Contains(searchTerm) ||
                            u.UserName.ToLower().Contains(searchTerm))
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.Email == email);
            if (excludeUserId.HasValue) query = query.Where(u => u.Id != excludeUserId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsUserNameUniqueAsync(string userName, Guid? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.UserName == userName);
            if (excludeUserId.HasValue) query = query.Where(u => u.Id != excludeUserId.Value);
            return !await query.AnyAsync();
        }

        public async Task<IList<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await GetByIdAsync(userId);
            return user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        }

        public async Task<bool> AddUserToRoleAsync(Guid userId, string roleName)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }
        public async Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public async Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }
    }
}
