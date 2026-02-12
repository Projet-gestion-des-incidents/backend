using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class IncidentTicketDTO
    {
        public Guid TicketId { get; set; }
        public string ReferenceTicket { get; set; }
        public string TitreTicket { get; set; }
        public StatutTicket StatutTicket { get; set; }
        public PrioriteTicket PrioriteTicket { get; set; }
    }
}
