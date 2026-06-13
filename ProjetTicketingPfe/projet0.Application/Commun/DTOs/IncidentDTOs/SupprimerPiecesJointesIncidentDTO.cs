
namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class SupprimerPiecesJointesIncidentDTO
    {
        public Guid IncidentId { get; set; }
        public List<Guid> PieceJointeIds { get; set; }
    }
}
