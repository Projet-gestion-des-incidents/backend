using projet0.Domain.Enums;

namespace projet0.Domain.Entities
{
    public class Incident
    {
        public Guid Id { get; set; }
        public string CodeIncident { get; set; }  
        public string DescriptionIncident { get; set; }

        // Enums (stockés comme int)
        public SeveriteIncident SeveriteIncident { get; set; }
        public StatutIncident? StatutIncident { get; set; }
        public DateTime DateDetection { get; set; }
        public DateTime? DateResolution { get; set; }
        public Guid? CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }

        // Navigation Properties
        public virtual ICollection<IncidentTicket> IncidentTickets { get; set; }
        public virtual ICollection<EntiteImpactee> EntitesImpactees { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }

        // Audit
        public DateTime? UpdatedAt { get; set; }
        public string? Emplacement { get; set; }
        public TypeProbleme TypeProbleme { get; set; }

        // Relation avec les TPEs
        public virtual ICollection<IncidentTPE> IncidentTPEs { get; set; }

        // Relation avec les pièces jointes 
        public virtual ICollection<PieceJointe> PiecesJointes { get; set; }
        public virtual ICollection<IncidentArchive> IncidentArchives { get; set; }
    }
}

