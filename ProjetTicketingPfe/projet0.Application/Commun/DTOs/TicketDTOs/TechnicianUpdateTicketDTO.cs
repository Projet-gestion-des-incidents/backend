using projet0.Domain.Enums;

namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class TechnicianUpdateTicketDTO
    {
        public Guid? AssigneeId { get; set; }
        public StatutTicket? StatutTicket { get; set; }
    }
}
