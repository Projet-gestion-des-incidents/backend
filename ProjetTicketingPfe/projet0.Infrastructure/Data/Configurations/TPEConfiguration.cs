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
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
