using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class EntiteImpactee
    {
        public Guid Id { get; set; }
        public TypeEntiteImpactee TypeEntiteImpactee { get; set; }
        public string Nom { get; set; }

        // Foreign Keys
        public Guid IncidentId { get; set; }

        // Navigation Properties
        public virtual Incident Incident { get; set; }
    }
}
