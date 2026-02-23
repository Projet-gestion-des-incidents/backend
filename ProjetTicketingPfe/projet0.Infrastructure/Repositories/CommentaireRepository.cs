using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;

namespace projet0.Infrastructure.Repositories
{
    public class CommentaireRepository : GenericRepository<CommentaireTicket>, ICommentaireRepository
    {
        private readonly ApplicationDbContext _context;

        public CommentaireRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<CommentaireTicket>> GetCommentairesByTicketIdAsync(Guid ticketId)
        {
            return await _context.CommentairesTicket  // ✅ Correction: "CommentaireTickets" (singulier)
                .Include(c => c.Auteur)
                .Include(c => c.PiecesJointes)
                .Where(c => c.TicketId == ticketId)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<CommentaireTicket> GetCommentaireWithPiecesJointesAsync(Guid id)
        {
            return await _context.CommentairesTicket // ✅ Correction: "CommentaireTickets" (singulier)
                .Include(c => c.Auteur)
                .Include(c => c.PiecesJointes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}