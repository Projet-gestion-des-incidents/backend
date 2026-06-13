using Microsoft.EntityFrameworkCore;  
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;


namespace projet0.Infrastructure.Repositories
{
    public class TicketArchiveRepository : GenericRepository<TicketArchive>, ITicketArchiveRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketArchiveRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<Guid>> GetAllArchivedTicketIdsAsync()
        {
            return await _context.TicketArchives
                .Select(a => a.TicketId)
                .Distinct()
                .ToListAsync();
        }
        public async Task<bool> ExistsAsync(Guid ticketId, Guid userId)
        {
            return await _context.TicketArchives
                .AnyAsync(a => a.TicketId == ticketId && a.ArchiveParId == userId);
        }

        public async Task<List<Guid>> GetArchivedTicketIdsByUserAsync(Guid userId)
        {
            return await _context.TicketArchives
                .Where(a => a.ArchiveParId == userId)
                .Select(a => a.TicketId)
                .Distinct()
                .ToListAsync();
        }
      
        public async Task<List<TicketArchive>> GetArchivesByTicketCreatorAsync(Guid userId)
        {
            //  Pour les tickets, on utilise ArchiveParId
            // (peu importe qui a créé le ticket)
            return await _context.TicketArchives
                .Where(a => a.ArchiveParId == userId)
                .ToListAsync();
        }
        public async Task<TicketArchive?> GetByTicketAndUserAsync(Guid ticketId, Guid userId)
        {
            return await _context.TicketArchives
                .FirstOrDefaultAsync(a => a.TicketId == ticketId && a.ArchiveParId == userId);
        }
    }
}
