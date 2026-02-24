using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Infrastructure.Data;

namespace projet0.Infrastructure.Repositories
{
    public class PieceJointeRepository : GenericRepository<PieceJointe>, IPieceJointeRepository
    {
        private readonly ApplicationDbContext _context;

        public PieceJointeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère une pièce jointe par son ID avec toutes ses propriétés
        /// </summary>
        public async Task<PieceJointe> GetByIdAsync(Guid id)
        {
            return await _context.PiecesJointes
                .Include(p => p.Commentaire)
                .Include(p => p.UploadedBy)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Récupère toutes les pièces jointes d'un commentaire
        /// </summary>
        public async Task<List<PieceJointe>> GetByCommentaireIdAsync(Guid commentaireId)
        {
            return await _context.PiecesJointes
                .Where(p => p.CommentaireId == commentaireId)
                .OrderByDescending(p => p.DateAjout)
                .ToListAsync();
        }

        /// <summary>
        /// Vérifie si une pièce jointe existe
        /// </summary>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.PiecesJointes.AnyAsync(p => p.Id == id);
        }

        /// <summary>
        /// Récupère uniquement les métadonnées d'une pièce jointe (sans les relations)
        /// </summary>
        public async Task<PieceJointe> GetMetadataAsync(Guid id)
        {
            return await _context.PiecesJointes
                .AsNoTracking()  // Performance: pas de tracking
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
