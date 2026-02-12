using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");
            builder.HasKey(t => t.Id);

            builder.HasIndex(t => t.ReferenceTicket).IsUnique();

            builder.Property(t => t.ReferenceTicket).IsRequired().HasMaxLength(20);
            builder.Property(t => t.TitreTicket).IsRequired().HasMaxLength(200);
            builder.Property(t => t.DescriptionTicket).HasMaxLength(2000);
            builder.Property(t => t.DateCreation).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Enums
            builder.Property(t => t.StatutTicket).HasConversion<int>();
            builder.Property(t => t.PrioriteTicket).HasConversion<int>();

            // Relations
            builder.HasOne(t => t.Createur)
                .WithMany(u => u.TicketsCrees)
                .HasForeignKey(t => t.CreateurId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Assignee)
                .WithMany(u => u.TicketsAssignes)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.IncidentTickets)
                .WithOne(it => it.Ticket)
                .HasForeignKey(it => it.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Historiques)
                .WithOne(h => h.Ticket)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Commentaires)
                .WithOne(c => c.Ticket)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
