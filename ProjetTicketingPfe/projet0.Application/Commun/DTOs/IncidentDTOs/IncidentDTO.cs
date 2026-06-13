using projet0.Domain.Enums;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class IncidentDTO
    {
        public Guid Id { get; set; }
        public string CodeIncident { get; set; }       
        public string DescriptionIncident { get; set; }
        public SeveriteIncident SeveriteIncident { get; set; }
        public string SeveriteIncidentLibelle { get; set; }
        public StatutIncident StatutIncident { get; set; }
        public string StatutIncidentLibelle { get; set; }
        public DateTime DateDetection { get; set; }
        public DateTime? DateResolution { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedById { get; set; }
        public string CreatedByName { get; set; }
        public TypeProbleme TypeProbleme { get; set; }
        public string Emplacement { get; set; }
        public DateTime? DateArchivage { get; set; }
        public int TicketCount { get; set; }
        public bool HasTicket => TicketCount > 0;
        public List<IncidentTicketInfoDTO> Tickets { get; set; } = new();
        public List<EntiteImpacteeDTO> EntitesImpactees { get; set; } = new();
        public class IncidentTicketInfoDTO
        {
            public Guid TicketId { get; set; }
            public string ReferenceTicket { get; set; }
            public string TitreTicket { get; set; }
            public string StatutTicket { get; set; }
            public DateTime DateLiaison { get; set; }
        }

    }
}
