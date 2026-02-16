using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
    {
        public void Configure(EntityTypeBuilder<Incident> builder)
        {
            builder.ToTable("Incidents");
            builder.HasKey(i => i.Id);

            builder.HasIndex(i => i.CodeIncident).IsUnique();

            builder.Property(i => i.CodeIncident).IsRequired().HasMaxLength(20);
            builder.Property(i => i.TitreIncident).IsRequired().HasMaxLength(200);
            builder.Property(i => i.DescriptionIncident).HasMaxLength(2000);
            builder.Property(i => i.DateDetection).HasDefaultValueSql("GETUTCDATE()").IsRequired();
            builder.Property(i => i.CreatedById);
            builder.Property(i => i.UpdatedById);

            // Enums
            builder.Property(i => i.SeveriteIncident).HasConversion<int>();
            builder.Property(i => i.StatutIncident).HasConversion<int>();

            // Relations
            builder.HasMany(i => i.IncidentTickets)
                .WithOne(it => it.Incident)
                .HasForeignKey(it => it.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(i => i.EntitesImpactees)
                .WithOne(e => e.Incident)
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
