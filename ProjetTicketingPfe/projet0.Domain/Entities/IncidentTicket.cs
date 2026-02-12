using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class IncidentTicket
    {
        // Clé composite (IncidentId + TicketId)
        public Guid IncidentId { get; set; }
        public Guid TicketId { get; set; }

        // Date de liaison
        public DateTime DateLiaison { get; set; }

        // Qui a fait la liaison
        public Guid? LieParId { get; set; }

        // Navigation Properties
        public virtual Incident Incident { get; set; }
        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser LiePar { get; set; }
    }
}
