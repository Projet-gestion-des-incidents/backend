using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class TPEConfiguration : IEntityTypeConfiguration<TPE>
    {
        public void Configure(EntityTypeBuilder<TPE> builder)
        {
            builder.ToTable("TPEs");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.NumSerie).IsRequired().HasMaxLength(50);
            builder.Property(t => t.NumSerieComplet).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Modele).HasConversion<int>().IsRequired();

            // Index composite pour garantir l'unicité (NumSerie + Modele)
            builder.HasIndex(t => new { t.NumSerie, t.Modele }).IsUnique();

            builder.HasOne(t => t.Commercant)
                .WithMany(u => u.TPEs)
                .HasForeignKey(t => t.CommercantId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ CONFIGURATION DES CHAMPS D'AUDIT
            builder.Property(t => t.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(t => t.CreatedById)
                .IsRequired(false);

            builder.Property(t => t.UpdatedAt)
                .IsRequired(false);

            builder.Property(t => t.UpdatedById)
                .IsRequired(false);

            // Relations avec les utilisateurs pour l'audit
            builder.HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.UpdatedBy)
                .WithMany()
                .HasForeignKey(t => t.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        
    }
    }
}
