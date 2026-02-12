using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Repositories
{
    public class EntiteImpacteeRepository : GenericRepository<EntiteImpactee>, IEntiteImpacteeRepository
    {
        private readonly ApplicationDbContext _context;

        public EntiteImpacteeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<EntiteImpactee>> GetByIdsAsync(List<Guid> ids)
        {
            return await _context.EntitesImpactees
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();
        }

        public async Task<List<EntiteImpactee>> GetByIncidentIdAsync(Guid incidentId)
        {
            return await _context.EntitesImpactees
                .Where(e => e.IncidentId == incidentId)
                .ToListAsync();
        }

        public async Task<List<EntiteImpactee>> GetByTypeAsync(TypeEntiteImpactee type)
        {
            return await _context.EntitesImpactees
                .Where(e => e.TypeEntiteImpactee == type)
                .ToListAsync();
        }
    }
}
