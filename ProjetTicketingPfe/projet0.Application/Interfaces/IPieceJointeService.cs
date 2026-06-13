using projet0.Application.Commun.DTOs.Ticket;
using projet0.Domain.Entities;

namespace projet0.Application.Interfaces
{
    public interface IPieceJointeService
    {
        /// Sauvegarde un fichier et ses métadonnées
        Task<PieceJointe> SauvegarderFichierAsync(
            CreatePieceJointeDTO dto,
            Guid incidentId,
            Guid uploadedById);

        // Pour les commentaires 
        Task<PieceJointe> SauvegarderFichierPourCommentaireAsync(
            CreatePieceJointeDTO dto,
            Guid commentaireId,
            Guid uploadedById);

        /// Récupère l'URL d'un fichier
        Task<string> GetUrlFichierAsync(Guid pieceJointeId);

        /// Supprime un fichier (physique et base de données)
        Task<bool> SupprimerFichierAsync(Guid pieceJointeId);

        /// Récupère toutes les pièces jointes d'un commentaire
       
        Task<List<PieceJointeDTO>> GetPiecesJointesByCommentaireAsync(Guid commentaireId);
        Task<bool> SupprimerPiecesJointesAsync(List<Guid> pieceJointeIds);
        Task<PieceJointe> SauvegarderFichierPourIncidentAsync(
      CreatePieceJointeDTO dto,
      Guid incidentId,
      Guid uploadedById);

        Task<List<PieceJointeDTO>> GetPiecesJointesByIncidentIdAsync(Guid incidentId);
        Task<bool> SupprimerPiecesJointesIncidentAsync(List<Guid> pieceJointeIds);
        Task<PieceJointe> GetMetadataAsync(Guid pieceJointeId);
    }
}