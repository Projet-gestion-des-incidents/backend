using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class PieceJointeConfiguration : IEntityTypeConfiguration<PieceJointe>
    {
        public void Configure(EntityTypeBuilder<PieceJointe> builder)
        {
            builder.ToTable("PiecesJointes");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.NomFichier).IsRequired().HasMaxLength(255);
            
            builder.Property(p => p.DateAjout).HasDefaultValueSql("GETUTCDATE()");

            

            builder.HasOne(p => p.Commentaire)
                .WithMany(c => c.PiecesJointes)
                .HasForeignKey(p => p.CommentaireId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.UploadedBy)
                .WithMany(u => u.PiecesJointes)
                .HasForeignKey(p => p.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation avec Incident (optionnelle)
            builder.HasOne(p => p.Incident)
                .WithMany(i => i.PiecesJointes)
                .HasForeignKey(p => p.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
