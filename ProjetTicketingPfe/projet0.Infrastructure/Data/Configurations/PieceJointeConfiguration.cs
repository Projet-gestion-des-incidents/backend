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
            builder.Property(p => p.CheminStockage).IsRequired().HasMaxLength(500);
            builder.Property(p => p.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Taille).IsRequired();
            builder.Property(p => p.DateAjout).HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.TypePieceJointe).HasConversion<int>();

            builder.HasOne(p => p.Commentaire)
                .WithMany(c => c.PiecesJointes)
                .HasForeignKey(p => p.CommentaireId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.UploadedBy)
                .WithMany(u => u.PiecesJointes)
                .HasForeignKey(p => p.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
