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

        // ✅ NOUVEAU: Récupérer un commentaire pour modification
        public async Task<CommentaireTicket> GetCommentaireForUpdateAsync(Guid id)
        {
            return await _context.CommentairesTicket
                .Include(c => c.PiecesJointes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // ✅ NOUVEAU: Vérifier si le commentaire a des pièces jointes
        public async Task<bool> HasPiecesJointesAsync(Guid commentaireId)
        {
            return await _context.PiecesJointes.AnyAsync(p => p.CommentaireId == commentaireId);
        }

        // ✅ NOUVEAU: Supprimer un commentaire et ses pièces jointes
        public async Task DeleteCommentaireWithPiecesJointesAsync(Guid id)
        {
            var commentaire = await _context.CommentairesTicket
                .Include(c => c.PiecesJointes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commentaire != null)
            {
                _context.CommentairesTicket.Remove(commentaire);
                // Les pièces jointes seront supprimées en cascade par EF Core
            }
        }
    }
}