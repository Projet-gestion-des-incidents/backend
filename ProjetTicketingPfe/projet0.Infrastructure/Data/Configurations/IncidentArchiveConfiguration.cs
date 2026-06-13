using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using projet0.Domain.Entities;

namespace projet0.Infrastructure.Data.Configurations
{
    public class IncidentArchiveConfiguration : IEntityTypeConfiguration<IncidentArchive>
    {
        public void Configure(EntityTypeBuilder<IncidentArchive> builder)
        {
            builder.ToTable("IncidentArchives");
            builder.HasKey(a => a.Id);

            builder.HasOne(a => a.Incident)
                .WithMany(i => i.IncidentArchives)
                .HasForeignKey(a => a.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.ArchivePar)
                .WithMany()
                .HasForeignKey(a => a.ArchiveParId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.IncidentId, a.ArchiveParId }).IsUnique();
        }
    }
}
