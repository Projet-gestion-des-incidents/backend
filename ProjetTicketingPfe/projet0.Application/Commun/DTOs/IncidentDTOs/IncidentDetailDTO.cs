using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.DTOs.Ticket;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class IncidentDetailDTO: IncidentDTO
    {
        public List<IncidentTicketDTO> Tickets { get; set; }
        public List<EntiteImpacteeDTO> EntitesImpactees { get; set; }
        public List<IncidentTPEDTO> TPEs { get; set; }
        public List<PieceJointeDTO> PiecesJointes { get; set; }

    }

}
