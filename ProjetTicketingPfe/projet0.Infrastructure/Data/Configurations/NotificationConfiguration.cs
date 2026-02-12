using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Titre).IsRequired().HasMaxLength(200);
            builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            builder.Property(n => n.EstLu).HasDefaultValue(false);
            builder.Property(n => n.DateEnvoi).HasDefaultValueSql("GETUTCDATE()");

            builder.Property(n => n.TypeNotification).HasConversion<int>();

            builder.HasOne(n => n.Destinataire)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.DestinataireId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.Ticket)
                .WithMany(t => t.Notifications)
                .HasForeignKey(n => n.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.Incident)
                .WithMany(i => i.Notifications)
                .HasForeignKey(n => n.IncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.Commentaire)
                .WithMany()
                .HasForeignKey(n => n.CommentaireId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
