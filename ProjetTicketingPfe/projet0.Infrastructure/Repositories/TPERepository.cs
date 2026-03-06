using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projet0.Infrastructure.Repositories
{
    public class TPERepository : GenericRepository<TPE>, ITPERepository
    {
        public TPERepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TPE>> GetByCommercantIdAsync(Guid commercantId)
        {
            return await _dbSet
                .Where(t => t.CommercantId == commercantId)
                .OrderBy(t => t.NumSerie)
                .ToListAsync();
        }

        public async Task<bool> IsNumSerieUniqueForModeleAsync(string numSerie, ModeleTPE modele, Guid? excludeId = null)
        {
            var query = _dbSet.Where(t => t.NumSerie == numSerie && t.Modele == modele);
            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            return !await query.AnyAsync();
        }

    }
}