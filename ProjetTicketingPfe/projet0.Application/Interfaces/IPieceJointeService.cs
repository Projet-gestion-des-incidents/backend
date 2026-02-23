// Fichier: projet0.Application/Interfaces/IPieceJointeService.cs
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Domain.Entities;

namespace projet0.Application.Interfaces
{
    public interface IPieceJointeService
    {
        /// <summary>
        /// Sauvegarde un fichier et ses métadonnées
        /// </summary>
        Task<PieceJointe> SauvegarderFichierAsync(
            CreatePieceJointeDTO dto,
            Guid commentaireId,
            Guid uploadedById);

        /// <summary>
        /// Récupère l'URL d'un fichier
        /// </summary>
        Task<string> GetUrlFichierAsync(Guid pieceJointeId);

        /// <summary>
        /// Supprime un fichier (physique et base de données)
        /// </summary>
        Task<bool> SupprimerFichierAsync(Guid pieceJointeId);

        /// <summary>
        /// Récupère toutes les pièces jointes d'un commentaire
        /// </summary>
        Task<List<PieceJointeDTO>> GetPiecesJointesByCommentaireAsync(Guid commentaireId);
        Task<bool> SupprimerPiecesJointesAsync(List<Guid> pieceJointeIds);
    }
}