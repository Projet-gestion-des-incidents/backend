using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;
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

        public async Task<bool> DeleteLiaisonAsync(Guid ticketId, Guid incidentId)
        {
            var liaison = await _context.IncidentTickets
                .FirstOrDefaultAsync(it => it.TicketId == ticketId && it.IncidentId == incidentId);

            if (liaison == null)
                return false;

            _context.IncidentTickets.Remove(liaison);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<List<Guid>> GetIncidentIdsByTicketIdAsync(Guid ticketId)
        {
            return await _context.IncidentTickets
                .Where(it => it.TicketId == ticketId)
                .Select(it => it.IncidentId)
                .ToListAsync();
        }

        public async Task<List<Incident>> GetIncidentsSansTicketAsync()
        {
            // Récupère les incidents qui n'apparaissent dans aucune liaison IncidentTicket
            var incidentsAvecTickets = await _context.IncidentTickets
                .Select(it => it.IncidentId)
                .Distinct()
                .ToListAsync();

            var incidentsSansTicket = await _context.Incidents
                .Include(i => i.EntitesImpactees)
                .Where(i => !incidentsAvecTickets.Contains(i.Id))
                .OrderByDescending(i => i.DateDetection)
                .ToListAsync();

            return incidentsSansTicket;
        }
    }
}
