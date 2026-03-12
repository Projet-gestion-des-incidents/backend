using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace projet0.Infrastructure.Repositories
{
    public class IncidentTicketRepository : GenericRepository<IncidentTicket>, IIncidentTicketRepository
    {
        private readonly ApplicationDbContext _context;

        public IncidentTicketRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<IncidentTicket>> GetByTicketIdAsync(Guid ticketId)
        {
            return await _context.IncidentTickets
                .Where(it => it.TicketId == ticketId)
                .ToListAsync();
        }

        public async Task<List<IncidentTicket>> GetByIncidentIdAsync(Guid incidentId)
        {
            return await _context.IncidentTickets
                .Where(it => it.IncidentId == incidentId)
                .ToListAsync();
        }

        public async Task<IncidentTicket> GetByTicketAndIncidentAsync(Guid ticketId, Guid incidentId)
        {
            return await _context.IncidentTickets
                .FirstOrDefaultAsync(it => it.TicketId == ticketId && it.IncidentId == incidentId);
        }

        public async Task<List<Incident>> GetIncidentsByTicketIdAsync(Guid ticketId)
        {
            return await _context.Incidents
                .Include(i => i.EntitesImpactees)
                .Include(i => i.IncidentTickets)
                .Where(i => i.IncidentTickets.Any(it => it.TicketId == ticketId))
                .ToListAsync();
        }

        public async Task<List<Ticket>> GetTicketsByIncidentIdAsync(Guid incidentId)
        {
            return await _context.IncidentTickets
                .Where(it => it.IncidentId == incidentId)
                .Select(it => it.Ticket)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid ticketId, Guid incidentId)
        {
            return await _context.IncidentTickets
                .AnyAsync(it => it.TicketId == ticketId && it.IncidentId == incidentId);
        }
    }
}
