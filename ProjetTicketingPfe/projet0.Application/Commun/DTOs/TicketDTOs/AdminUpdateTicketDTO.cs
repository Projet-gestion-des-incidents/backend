using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class AdminUpdateTicketDTO
    {
        public string? TitreTicket { get; set; }
        public string? DescriptionTicket { get; set; }
        public DateTime? DateLimite { get; set; }
        public Guid? AssigneeId { get; set; }
    }
}
