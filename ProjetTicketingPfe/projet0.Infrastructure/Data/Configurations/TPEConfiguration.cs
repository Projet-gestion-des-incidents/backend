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

            builder.HasIndex(t => t.NumSerie).IsUnique();
            builder.Property(t => t.NumSerie).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Modele).IsRequired().HasMaxLength(100);

            

            builder.HasOne(t => t.Commercant)
                .WithMany(u => u.TPEs)
                .HasForeignKey(t => t.CommercantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
