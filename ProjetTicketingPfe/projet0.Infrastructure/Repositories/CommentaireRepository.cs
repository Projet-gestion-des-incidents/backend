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
            return await _context.CommentairesTicket  
                .Include(c => c.Auteur)
                .Include(c => c.PiecesJointes)
                .Where(c => c.TicketId == ticketId)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<CommentaireTicket> GetCommentaireWithPiecesJointesAsync(Guid id)
        {
            return await _context.CommentairesTicket 
                .Include(c => c.PiecesJointes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Récupérer un commentaire pour modification
        public async Task<CommentaireTicket> GetCommentaireForUpdateAsync(Guid id)
        {
            return await _context.CommentairesTicket
                .Include(c => c.PiecesJointes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Vérifier si le commentaire a des pièces jointes
        public async Task<bool> HasPiecesJointesAsync(Guid commentaireId)
        {
            return await _context.PiecesJointes.AnyAsync(p => p.CommentaireId == commentaireId);
        }

        // Supprimer un commentaire et ses pièces jointes
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
        #region Nouvelles méthodes pour les commentaires du technicien

        /// <summary>
        /// Récupère tous les commentaires d'un technicien spécifique
        /// </summary>
        /// <param name="technicienId">ID du technicien</param>
        /// <returns>Liste des commentaires du technicien avec leurs pièces jointes et informations du ticket</returns>
        public async Task<IEnumerable<CommentaireTicket>> GetCommentairesByTechnicienAsync(Guid technicienId)
        {
            return await _context.CommentairesTicket
                .Include(c => c.Auteur)
                .Include(c => c.Ticket)
                .Include(c => c.PiecesJointes)
                .Where(c => c.AuteurId == technicienId)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les commentaires d'un technicien pour un ticket spécifique
        /// </summary>
        /// <param name="ticketId">ID du ticket</param>
        /// <param name="technicienId">ID du technicien</param>
        /// <returns>Liste des commentaires du technicien pour ce ticket</returns>
        public async Task<IEnumerable<CommentaireTicket>> GetCommentairesByTicketAndTechnicienAsync(Guid ticketId, Guid technicienId)
        {
            return await _context.CommentairesTicket
                .Include(c => c.Auteur)
                .Include(c => c.PiecesJointes)
                .Where(c => c.TicketId == ticketId && c.AuteurId == technicienId)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les commentaires d'un technicien avec pagination
        /// </summary>
        /// <param name="technicienId">ID du technicien</param>
        /// <param name="pageIndex">Index de la page (commence à 0)</param>
        /// <param name="pageSize">Taille de la page</param>
        /// <returns>Liste paginée des commentaires</returns>
        public async Task<IEnumerable<CommentaireTicket>> GetCommentairesByTechnicienPagedAsync(
            Guid technicienId,
            int pageIndex = 0,
            int pageSize = 10)
        {
            return await _context.CommentairesTicket
                .Include(c => c.Ticket)
                .Include(c => c.PiecesJointes)
                .Where(c => c.AuteurId == technicienId)
                .OrderByDescending(c => c.DateCreation)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Compte le nombre total de commentaires d'un technicien
        /// </summary>
        /// <param name="technicienId">ID du technicien</param>
        /// <returns>Nombre de commentaires</returns>
        public async Task<int> CountCommentairesByTechnicienAsync(Guid technicienId)
        {
            return await _context.CommentairesTicket
                .Where(c => c.AuteurId == technicienId)
                .CountAsync();
        }

        #endregion
    }
}