using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;


namespace projet0.Infrastructure.Repositories
{
    public class IncidentTPERepository : GenericRepository<IncidentTPE>, IIncidentTPERepository
    {
        private readonly ApplicationDbContext _context;

        public IncidentTPERepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<IncidentTPE>> GetByIncidentIdAsync(Guid incidentId)
        {
            return await _context.IncidentTPEs
                .Where(it => it.IncidentId == incidentId)
                .ToListAsync();
        }

        public async Task<List<IncidentTPE>> GetByTPEIdAsync(Guid tpeId)
        {
            return await _context.IncidentTPEs
                .Where(it => it.TPEId == tpeId)
                .ToListAsync();
        }

        public async Task<IncidentTPE> GetByIncidentAndTPEAsync(Guid incidentId, Guid tpeId)
        {
            return await _context.IncidentTPEs
                .FirstOrDefaultAsync(it => it.IncidentId == incidentId && it.TPEId == tpeId);
        }

        public async Task<bool> ExistsAsync(Guid incidentId, Guid tpeId)
        {
            return await _context.IncidentTPEs
                .AnyAsync(it => it.IncidentId == incidentId && it.TPEId == tpeId);
        }

        public async Task<bool> DeleteLiaisonAsync(Guid incidentId, Guid tpeId)
        {
            var liaison = await _context.IncidentTPEs
                .FirstOrDefaultAsync(it => it.IncidentId == incidentId && it.TPEId == tpeId);

            if (liaison == null)
                return false;

            _context.IncidentTPEs.Remove(liaison);
            await _context.SaveChangesAsync();

            return true;
        }

        
    }
}
