using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace projet0.Domain.Entities
{
    public class Incident
    {
        public Guid Id { get; set; }
        public string CodeIncident { get; set; }  // Format: INC-2026-001
        public string DescriptionIncident { get; set; }

        // Enums (stockés comme int)
        public SeveriteIncident SeveriteIncident { get; set; }
        public StatutIncident StatutIncident { get; set; }
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

        // Nouveaux champs
        public string Emplacement { get; set; }
        public TypeProbleme TypeProbleme { get; set; }  // Nouvel enum à créer

        // Relation avec les TPEs
        public virtual ICollection<IncidentTPE> IncidentTPEs { get; set; }

        // Relation avec les pièces jointes (manquante)
        public virtual ICollection<PieceJointe> PiecesJointes { get; set; }
    }
}

