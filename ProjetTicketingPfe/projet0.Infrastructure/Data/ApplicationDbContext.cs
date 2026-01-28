using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using projet0.Domain.Entities;
namespace projet0.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid> 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Ajouter le DbSet ici, en dehors de OnModelCreating
        public DbSet<OtpCode> OtpCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurations supplémentaires
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configuration Fluent API pour OtpCode
            builder.Entity<OtpCode>()
                .HasOne(o => o.User)
                .WithMany() // si tu n'as pas de collection d'OtpCode dans ApplicationUser
                .HasForeignKey(o => o.UserId);
        }
    }
}
