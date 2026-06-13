using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;

namespace projet0.Infrastructure.Data.Configurations
{
    public class TicketArchiveConfiguration : IEntityTypeConfiguration<TicketArchive>
    {
        public void Configure(EntityTypeBuilder<TicketArchive> builder)
        {
            builder.ToTable("TicketArchives");
            builder.HasKey(a => a.Id);

            builder.HasOne(a => a.Ticket)
                .WithMany(t => t.TicketArchives)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.ArchivePar)
                .WithMany()
                .HasForeignKey(a => a.ArchiveParId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.TicketId, a.ArchiveParId }).IsUnique();
        }
    }
}
