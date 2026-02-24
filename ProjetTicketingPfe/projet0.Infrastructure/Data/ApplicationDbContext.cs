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

        public DbSet<ApplicationUser> Users { get; set; }

        // NOUVEAUX DbSets
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<IncidentTicket> IncidentTickets { get; set; }
        public DbSet<CommentaireTicket> CommentairesTicket { get; set; }
        public DbSet<PieceJointe> PiecesJointes { get; set; }
        public DbSet<HistoriqueTicket> HistoriquesTicket { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EntiteImpactee> EntitesImpactees { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Configurations supplémentaires
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configuration Fluent API pour OtpCode
            builder.Entity<OtpCode>()
                .HasOne(o => o.User)
                .WithMany() // si tu n'as pas de collection d'OtpCode dans ApplicationUser
                .HasForeignKey(o => o.UserId);

            builder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReferenceTicket).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TitreTicket).IsRequired().HasMaxLength(200);

                // Relation avec le créateur (Createur)
                entity.HasOne(e => e.Createur)
                    .WithMany()  // Un utilisateur peut créer plusieurs tickets
                    .HasForeignKey(e => e.CreateurId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Tickets_Createur");  // Nom explicite

                // Relation avec l'assigné (Assignee)
                entity.HasOne(e => e.Assignee)
                    .WithMany()  // Un utilisateur peut être assigné à plusieurs tickets
                    .HasForeignKey(e => e.AssigneeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Tickets_Assignee");
            });

            // Configuration pour CommentaireTicket
            builder.Entity<CommentaireTicket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);

                entity.HasOne(e => e.Ticket)
                    .WithMany(t => t.Commentaires)
                    .HasForeignKey(e => e.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Auteur)
                    .WithMany()
                    .HasForeignKey(e => e.AuteurId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuration pour PieceJointe
            builder.Entity<PieceJointe>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomFichier).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CheminStockage).IsRequired().HasMaxLength(500);

                entity.HasOne(e => e.Commentaire)
                    .WithMany(c => c.PiecesJointes)
                    .HasForeignKey(e => e.CommentaireId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UploadedBy)
                    .WithMany()
                    .HasForeignKey(e => e.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
