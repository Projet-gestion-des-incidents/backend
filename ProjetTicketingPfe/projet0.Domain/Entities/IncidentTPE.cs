using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class IncidentTPE
    {
        public Guid IncidentId { get; set; }
        public Guid TPEId { get; set; }
        public DateTime DateAssociation { get; set; }

        // Navigation Properties
        public virtual Incident Incident { get; set; }
        public virtual TPE TPE { get; set; }
    }
}
