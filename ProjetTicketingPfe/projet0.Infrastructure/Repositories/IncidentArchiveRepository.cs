using Microsoft.EntityFrameworkCore;  
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;


namespace projet0.Infrastructure.Repositories
{
    public class IncidentArchiveRepository : GenericRepository<IncidentArchive>, IIncidentArchiveRepository
    {
        private readonly ApplicationDbContext _context;

        public IncidentArchiveRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(Guid incidentId, Guid userId)
        {
            return await _context.IncidentArchives
                .AnyAsync(a => a.IncidentId == incidentId && a.ArchiveParId == userId);
        }

        public async Task<List<Guid>> GetArchivedIncidentIdsByUserAsync(Guid userId)
        {
            return await _context.IncidentArchives
                .Where(a => a.ArchiveParId == userId)
                .Select(a => a.IncidentId)
                .Distinct()
                .ToListAsync();
        }
        public async Task<List<IncidentArchive>> GetArchivesByUserAsync(Guid userId)
        {
            return await _context.IncidentArchives
                .Where(a => a.ArchiveParId == userId)
                .ToListAsync();
        }
        public async Task<IncidentArchive?> GetByIncidentAndUserAsync(Guid incidentId, Guid userId)
        {
            return await _context.IncidentArchives
                .FirstOrDefaultAsync(a => a.IncidentId == incidentId && a.ArchiveParId == userId);
        }
        public async Task<List<Guid>> GetAllArchivedIncidentIdsAsync()
        {
            return await _context.IncidentArchives
                .Select(a => a.IncidentId)
                .Distinct()
                .ToListAsync();
        }
    }
}
