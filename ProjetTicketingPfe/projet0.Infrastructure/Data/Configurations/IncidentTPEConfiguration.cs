using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class IncidentTPEConfiguration : IEntityTypeConfiguration<IncidentTPE>
    {
        public void Configure(EntityTypeBuilder<IncidentTPE> builder)
        {
            builder.ToTable("IncidentTPEs");
            builder.HasKey(it => new { it.IncidentId, it.TPEId });

            builder.Property(it => it.DateAssociation)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(it => it.Incident)
                .WithMany(i => i.IncidentTPEs)
                .HasForeignKey(it => it.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(it => it.TPE)
                .WithMany(t => t.IncidentTPEs)
                .HasForeignKey(it => it.TPEId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
