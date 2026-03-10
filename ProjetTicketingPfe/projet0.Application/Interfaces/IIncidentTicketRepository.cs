using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface IIncidentTicketRepository : IGenericRepository<IncidentTicket>
    {
        Task<List<IncidentTicket>> GetByTicketIdAsync(Guid ticketId);
        Task<List<IncidentTicket>> GetByIncidentIdAsync(Guid incidentId);
        Task<IncidentTicket> GetByTicketAndIncidentAsync(Guid ticketId, Guid incidentId);
        Task<List<Incident>> GetIncidentsByTicketIdAsync(Guid ticketId);
        Task<List<Ticket>> GetTicketsByIncidentIdAsync(Guid incidentId);
        Task<bool> ExistsAsync(Guid ticketId, Guid incidentId);
    }
}
