using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class HistoriqueTicketConfiguration : IEntityTypeConfiguration<HistoriqueTicket>
    {
        public void Configure(EntityTypeBuilder<HistoriqueTicket> builder)
        {
            builder.ToTable("HistoriquesTicket");
            builder.HasKey(h => h.Id);

            builder.Property(h => h.DateChangement).HasDefaultValueSql("GETUTCDATE()");

            builder.Property(h => h.AncienStatut).HasConversion<int>();
            builder.Property(h => h.NouveauStatut).HasConversion<int>();

            builder.HasOne(h => h.Ticket)
                .WithMany(t => t.Historiques)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.ModifiePar)
                .WithMany(u => u.HistoriquesModifies)
                .HasForeignKey(h => h.ModifieParId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
