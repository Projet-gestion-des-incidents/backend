using System;
using System.Collections.Generic;
using System.Text;

// Fichier: projet0.Infrastructure/Repositories/TicketRepository.cs
using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;

namespace projet0.Infrastructure.Repositories
{
    public class TicketRepository : GenericRepository<Ticket>, ITicketRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Ticket> GetByReferenceAsync(string reference)
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .Include(t => t.Commentaires)
                    .ThenInclude(c => c.PiecesJointes)
                .Include(t => t.Historiques)
                .FirstOrDefaultAsync(t => t.ReferenceTicket == reference);
        }

        public override async Task<Ticket> GetByIdAsync(Guid id)
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public override async Task<IEnumerable<Ticket>> GetAllAsync()
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .OrderByDescending(t => t.DateCreation)
                .ToListAsync();
        }

        public async Task<Ticket> GetTicketWithDetailsAsync(Guid id)
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .Include(t => t.Commentaires)
                    .ThenInclude(c => c.Auteur)
                .Include(t => t.Commentaires)
                    .ThenInclude(c => c.PiecesJointes)
                .Include(t => t.Historiques)
                    .ThenInclude(h => h.ModifiePar)
                .Include(t => t.IncidentTickets)
                    .ThenInclude(it => it.Incident)
                .Include(t => t.Notifications)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Ticket>> GetTicketsByStatutAsync(StatutTicket statut)
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .Where(t => t.StatutTicket == statut)
                .OrderByDescending(t => t.DateCreation)
                .ToListAsync();
        }

        public async Task<List<Ticket>> GetTicketsByPrioriteAsync(PrioriteTicket priorite)
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .Where(t => t.PrioriteTicket == priorite)
                .OrderByDescending(t => t.DateCreation)
                .ToListAsync();
        }

        public async Task<List<Ticket>> GetTicketsByCreateurAsync(Guid createurId)
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .Where(t => t.CreateurId == createurId)
                .OrderByDescending(t => t.DateCreation)
                .ToListAsync();
        }

        public async Task<List<Ticket>> GetTicketsByAssigneeAsync(Guid assigneeId)
        {
            return await _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .Where(t => t.AssigneeId == assigneeId)
                .OrderByDescending(t => t.DateCreation)
                .ToListAsync();
        }

        public async Task<bool> IsReferenceUniqueAsync(string reference, Guid? excludeId = null)
        {
            var query = _context.Tickets.Where(t => t.ReferenceTicket == reference);

            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<string> GenerateReferenceTicketAsync()
        {
            var year = DateTime.Now.Year;
            var nextNumber = await GetNextTicketNumberAsync(year);
            return $"TCK-{year}-{nextNumber:D3}";
        }

        public async Task<int> GetNextTicketNumberAsync(int year)
        {
            var lastTicket = await _context.Tickets
                .Where(t => t.ReferenceTicket.StartsWith($"TCK-{year}-"))
                .OrderByDescending(t => t.ReferenceTicket)
                .FirstOrDefaultAsync();

            if (lastTicket == null)
                return 1;

            var parts = lastTicket.ReferenceTicket.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                return lastNumber + 1;

            return 1;
        }

        public IQueryable<Ticket> QueryWithDetails(Guid? createurId = null, Guid? assigneeId = null)
        {
            var query = _context.Tickets
                .Include(t => t.Createur)
                .Include(t => t.Assignee)
                .Include(t => t.Commentaires)
                .Include(t => t.Historiques)
                .AsQueryable();

            if (createurId.HasValue)
                query = query.Where(t => t.CreateurId == createurId.Value);

            if (assigneeId.HasValue)
                query = query.Where(t => t.AssigneeId == assigneeId.Value);

            return query;
        }
    }
}