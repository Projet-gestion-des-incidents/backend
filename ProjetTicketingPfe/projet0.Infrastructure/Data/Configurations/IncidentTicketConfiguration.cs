using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class IncidentTicketConfiguration : IEntityTypeConfiguration<IncidentTicket>
    {
        public void Configure(EntityTypeBuilder<IncidentTicket> builder)
        {
            builder.ToTable("IncidentTickets");

            builder.HasKey(it => new { it.IncidentId, it.TicketId });

            builder.Property(it => it.DateLiaison)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(it => it.Incident)
                .WithMany(i => i.IncidentTickets)
                .HasForeignKey(it => it.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(it => it.Ticket)
                .WithMany(t => t.IncidentTickets)
                .HasForeignKey(it => it.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(it => it.LiePar)
                .WithMany(u => u.IncidentLiaisons)
                .HasForeignKey(it => it.LieParId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
