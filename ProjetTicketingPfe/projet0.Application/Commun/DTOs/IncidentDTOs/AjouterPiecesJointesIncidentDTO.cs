using Microsoft.AspNetCore.Http;

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class AjouterPiecesJointesIncidentDTO
    {
        public Guid IncidentId { get; set; }
        public List<IFormFile> Fichiers { get; set; }
    }
}
