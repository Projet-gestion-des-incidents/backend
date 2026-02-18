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
    public class IncidentRepository : GenericRepository<Incident>, IIncidentRepository
    {
        private readonly ApplicationDbContext _context;

        public IncidentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Incident> GetByCodeAsync(string code)
        {
            return await _context.Incidents
                .Include(i => i.IncidentTickets)
                    .ThenInclude(it => it.Ticket)
                .Include(i => i.EntitesImpactees)
                .Include(i => i.Notifications)
                .FirstOrDefaultAsync(i => i.CodeIncident == code);
        }
        public override async Task<Incident> GetByIdAsync(Guid id)
        {
            return await _context.Incidents
                .Include(i => i.EntitesImpactees)  // ✅ AJOUTER CET INCLUDE !
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public override async Task<IEnumerable<Incident>> GetAllAsync()
        {
            return await _context.Incidents
                .Include(i => i.EntitesImpactees)  // ✅ AJOUTER CET INCLUDE !
                .OrderByDescending(i => i.DateDetection)
                .ToListAsync();
        }
        public async Task<List<Incident>> GetIncidentsByStatutAsync(StatutIncident statut)
        {
            return await _context.Incidents
                   .Include(i => i.EntitesImpactees)
                .Where(i => i.StatutIncident == statut)
                .OrderByDescending(i => i.DateDetection)
                .ToListAsync();
        }

        public async Task<List<Incident>> GetIncidentsBySeveriteAsync(SeveriteIncident severite)
        {
            return await _context.Incidents
                   .Include(i => i.EntitesImpactees)
                .Where(i => i.SeveriteIncident == severite)
                .OrderByDescending(i => i.DateDetection)
                .ToListAsync();
        }

        public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null)
        {
            var query = _context.Incidents.Where(i => i.CodeIncident == code);

            if (excludeId.HasValue)
            {
                query = query.Where(i => i.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<string> GenerateCodeIncidentAsync()
        {
            var year = DateTime.Now.Year;
            var nextNumber = await GetNextIncidentNumberAsync(year);
            return $"INC-{year}-{nextNumber:D3}";
        }

        public async Task<int> GetNextIncidentNumberAsync(int year)
        {
            var lastIncident = await _context.Incidents
                .Where(i => i.CodeIncident.StartsWith($"INC-{year}-"))
                .OrderByDescending(i => i.CodeIncident)
                .FirstOrDefaultAsync();

            if (lastIncident == null)
                return 1;

            var parts = lastIncident.CodeIncident.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                return lastNumber + 1;

            return 1;
        }

        public async Task<Incident> GetIncidentWithDetailsAsync(Guid id)
        {
            return await _context.Incidents
                .Include(i => i.IncidentTickets)
                    .ThenInclude(it => it.Ticket)
                .Include(i => i.EntitesImpactees)
                .Include(i => i.Notifications)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Incident>> GetAllWithDetailsAsync()
        {
            return await _context.Incidents
                .Include(i => i.IncidentTickets)
                    .ThenInclude(it => it.Ticket)
                .Include(i => i.EntitesImpactees)
                .Include(i => i.Notifications)
                .OrderByDescending(i => i.DateDetection)
                .ToListAsync();
        }
        public IQueryable<Incident> QueryWithDetails(Guid? createdById = null)
        {
            var query = _context.Incidents
                .Include(i => i.EntitesImpactees)
                .Include(i => i.IncidentTickets)
                    .ThenInclude(it => it.Ticket)
                .AsQueryable();

            if (createdById.HasValue)
                query = query.Where(i => i.CreatedById == createdById.Value);

            return query;
        }
        public void RemoveEntiteImpactee(EntiteImpactee entite)
        {
            _context.EntitesImpactees.Remove(entite);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task AddEntiteImpacteeAsync(EntiteImpactee entite)
        {
            await _context.EntitesImpactees.AddAsync(entite);
        }

        public async Task<List<Incident>> GetIncidentsByCreatedByAsync(Guid createdById)
        {
            return await _context.Incidents
                .Include(i => i.EntitesImpactees)
                .Include(i => i.IncidentTickets)
                    .ThenInclude(it => it.Ticket)
                .Where(i => i.CreatedById == createdById)
                .OrderByDescending(i => i.DateDetection)
                .ToListAsync();
        }


    }
}