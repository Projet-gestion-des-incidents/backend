using Microsoft.AspNetCore.Identity;
using projet0.Domain.Entities;

namespace projet0.Infrastructure.Seed
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            string[] roles = { "Admin", "Technicien", "Commercant" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole<Guid>(role)
                    );
                }
            }
        }
    }
}
