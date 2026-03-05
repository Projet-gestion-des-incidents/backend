using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class TPE
    {
        public Guid Id { get; set; }
        public string NumSerie { get; set; }
        public string Modele { get; set; }

        // Foreign Key - Relation Many-to-One avec User (Commercant)
        public Guid CommercantId { get; set; }

        // Navigation Properties
        public virtual ApplicationUser Commercant { get; set; }  // Un TPE appartient à un commerçant

        // Relation Many-to-Many avec Incident
        public virtual ICollection<IncidentTPE> IncidentTPEs { get; set; }
    }
}
