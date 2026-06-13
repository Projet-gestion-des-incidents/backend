namespace projet0.Domain.Entities
{
    public class IncidentArchive
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }
        public Guid ArchiveParId { get; set; }
        public DateTime DateArchivage { get; set; }
        public string? Commentaire { get; set; }  

        // Navigation
        public virtual Incident Incident { get; set; }
        public virtual ApplicationUser ArchivePar { get; set; }
    }
}
