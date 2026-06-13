using projet0.Domain.Entities;

namespace projet0.Application.Interfaces
{
    public interface IPieceJointeRepository : IGenericRepository<PieceJointe>
    {
        /// Récupère une pièce jointe par son ID
        Task<PieceJointe> GetByIdAsync(Guid id);

        /// Récupère toutes les pièces jointes d'un commentaire
        Task<List<PieceJointe>> GetByCommentaireIdAsync(Guid commentaireId);

        /// Vérifie si une pièce jointe existe
        Task<bool> ExistsAsync(Guid id);

        /// Récupère les métadonnées d'une pièce jointe (sans le fichier)
        Task<PieceJointe> GetMetadataAsync(Guid id);
        Task<List<PieceJointe>> GetByIncidentIdAsync(Guid incidentId);
    }
}