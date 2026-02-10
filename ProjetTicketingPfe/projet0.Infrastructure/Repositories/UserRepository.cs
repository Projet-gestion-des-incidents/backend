using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
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
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Créer sans mot de passe (si jamais nécessaire)
                return await _userManager.CreateAsync(user);
            }
            else
            {
                // Créer avec mot de passe
                return await _userManager.CreateAsync(user, password);
            }
        }

        /*public async Task<IdentityResult> SoftDeleteAsync(ApplicationUser user)
        {
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            return await _userManager.UpdateAsync(user);
        }
        public async Task<IdentityResult> RestoreAsync(ApplicationUser user)
        {
            user.IsDeleted = false;
            user.DeletedAt = null;
            return await _userManager.UpdateAsync(user);
        }*/

        public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users;
        }

        public async Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync()
        {
            return await _dbSet.OrderBy(u => u.Nom).ThenBy(u => u.Prenom).ToListAsync();
        }
        public async Task<IEnumerable<UserWithRoleDto>> GetAllUsersWithRolesAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var usersWithRoles = new List<UserWithRoleDto>();

            foreach (var user in users)
            {
                // Récupère le rôle (on suppose un seul rôle)
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "USER"; // fallback

                usersWithRoles.Add(new UserWithRoleDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Image = user.Image,
                    Role = roleName,
                    Statut = user.Statut,
                    BirthDate =user.BirthDate
                });
            }

            return usersWithRoles;
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

        public Task<IdentityResult> RestoreAsync(ApplicationUser user)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> SoftDeleteAsync(ApplicationUser user)
        {
            throw new NotImplementedException();
        }
        
        public async Task<(IEnumerable<UserWithRoleDto> Users, int TotalCount)> SearchUsersAsync(
            UserSearchRequest request)
        {
            // 1. Créer la query de base
            var query = _userManager.Users.AsQueryable();

            // 2. Recherche globale
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Nom.ToLower().Contains(term) ||
                    u.Prenom.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.UserName.ToLower().Contains(term));
            }

            // 3. Filtres additionnels
            if (request.Statut.HasValue)
                query = query.Where(u => u.Statut == request.Statut.Value);

            if (!string.IsNullOrWhiteSpace(request.UserName))
                query = query.Where(u => u.UserName.Contains(request.UserName));

            if (!string.IsNullOrWhiteSpace(request.Email))
                query = query.Where(u => u.Email.Contains(request.Email));

            if (!string.IsNullOrWhiteSpace(request.Nom))
                query = query.Where(u => u.Nom.Contains(request.Nom));

            if (!string.IsNullOrWhiteSpace(request.Prenom))
                query = query.Where(u => u.Prenom.Contains(request.Prenom));

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                query = query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(request.PhoneNumber));

            // 4. Filtre par DATE de naissance (modifier pour accepter l'année)
            if (request.BirthDate.HasValue)
            {
                // Utilisez l'année seulement
                var year = request.BirthDate.Value.Year;
                query = query.Where(u => u.BirthDate.HasValue && u.BirthDate.Value.Year == year);
            }

            // 5. Appliquer le tri
            query = ApplySorting(query, request.SortBy, request.SortDescending);

            // 6. Compter le total (avant pagination)
            var totalCount = await query.CountAsync();

            // 7. Appliquer la pagination
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var skip = (page - 1) * pageSize;
            if (skip >= totalCount && totalCount > 0)
            {
                page = (int)Math.Ceiling(totalCount / (double)pageSize);
                skip = (page - 1) * pageSize;
            }

            var paginatedUsers = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // 8. Convertir en DTO avec rôles
            var usersWithRoles = new List<UserWithRoleDto>();
            foreach (var user in paginatedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "USER";

                // Filtrer par rôle si spécifié
                if (string.IsNullOrWhiteSpace(request.Role) ||
                    string.Equals(roleName, request.Role.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    usersWithRoles.Add(new UserWithRoleDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Nom = user.Nom,
                        Prenom = user.Prenom,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Image = user.Image,
                        Role = roleName,
                        Statut = user.Statut,
                        BirthDate = user.BirthDate
                    });
                }
            }

            // 9. Ajuster le totalCount après filtrage par rôle
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                totalCount = usersWithRoles.Count;
            }

            return (usersWithRoles, totalCount);
        }

        // Méthode helper pour le tri (identique à celle de UserService)
        private IQueryable<ApplicationUser> ApplySorting(
            IQueryable<ApplicationUser> query,
            string sortBy,
            bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderBy(u => u.Nom);

            switch (sortBy.ToLower())
            {
                case "username":
                    return sortDescending
                        ? query.OrderByDescending(u => u.UserName)
                        : query.OrderBy(u => u.UserName);

                case "email":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email);

                case "nom":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Nom)
                        : query.OrderBy(u => u.Nom);

                case "prenom":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Prenom)
                        : query.OrderBy(u => u.Prenom);

                case "birthdate":
                    if (sortDescending)
                        return query.OrderByDescending(u => u.BirthDate.HasValue)
                                   .ThenByDescending(u => u.BirthDate);
                    else
                        return query.OrderBy(u => u.BirthDate.HasValue)
                                   .ThenBy(u => u.BirthDate);

                case "statut":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Statut)
                        : query.OrderBy(u => u.Statut);

                default:
                    return query.OrderBy(u => u.Nom);
            }
        }

    }
}
