using System;
using System.Collections.Generic;
using System.Text;
using projet0.Domain.Entities;

namespace projet0.Application.Interfaces
{
    public interface IPieceJointeRepository : IGenericRepository<PieceJointe>
    {
        /// <summary>
        /// Récupère une pièce jointe par son ID
        /// </summary>
        Task<PieceJointe> GetByIdAsync(Guid id);

        /// <summary>
        /// Récupère toutes les pièces jointes d'un commentaire
        /// </summary>
        Task<List<PieceJointe>> GetByCommentaireIdAsync(Guid commentaireId);

        /// <summary>
        /// Vérifie si une pièce jointe existe
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// Récupère les métadonnées d'une pièce jointe (sans le fichier)
        /// </summary>
        Task<PieceJointe> GetMetadataAsync(Guid id);
    }
}