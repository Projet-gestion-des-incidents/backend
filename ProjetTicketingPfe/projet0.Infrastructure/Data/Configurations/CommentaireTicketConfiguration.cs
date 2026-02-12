using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class CommentaireTicketConfiguration : IEntityTypeConfiguration<CommentaireTicket>
    {
        public void Configure(EntityTypeBuilder<CommentaireTicket> builder)
        {
            builder.ToTable("CommentairesTicket");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Message).IsRequired().HasMaxLength(2000);
            builder.Property(c => c.EstInterne).HasDefaultValue(false);
            builder.Property(c => c.DateCreation).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(c => c.Ticket)
                .WithMany(t => t.Commentaires)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Auteur)
                .WithMany(u => u.Commentaires)
                .HasForeignKey(c => c.AuteurId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.PiecesJointes)
                .WithOne(p => p.Commentaire)
                .HasForeignKey(p => p.CommentaireId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
