using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class EntiteImpacteeConfiguration : IEntityTypeConfiguration<EntiteImpactee>
    {
        public void Configure(EntityTypeBuilder<EntiteImpactee> builder)
        {
            builder.ToTable("EntitesImpactees");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Nom).IsRequired().HasMaxLength(200);
            builder.Property(e => e.TypeEntiteImpactee).HasConversion<int>();

            builder.Property(e => e.IncidentId)
               .IsRequired();

builder.HasOne(e => e.Incident)
       .WithMany(i => i.EntitesImpactees)
       .HasForeignKey(e => e.IncidentId)
      .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
