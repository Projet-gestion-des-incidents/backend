
namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class IncidentArchiveDTO
    {
        public Guid IncidentId { get; set; }
        public string CodeIncident { get; set; }
        public bool EstArchive { get; set; }
        public DateTime? DateArchivage { get; set; }
        public string ArchivePar { get; set; }
    }
}
