using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class LinkIncidentsToTicketDTO
    {
        public Guid TicketId { get; set; }
        public List<Guid> IncidentIds { get; set; }
    }
}
public class LinkTicketsToIncidentDTO
{
    public Guid IncidentId { get; set; }
    public List<Guid> TicketIds { get; set; }
}
