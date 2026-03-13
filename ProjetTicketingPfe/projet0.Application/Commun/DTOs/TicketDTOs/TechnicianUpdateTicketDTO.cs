using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class TechnicianUpdateTicketDTO
    {
        public Guid? AssigneeId { get; set; }
        public StatutTicket? StatutTicket { get; set; }
    }
}
