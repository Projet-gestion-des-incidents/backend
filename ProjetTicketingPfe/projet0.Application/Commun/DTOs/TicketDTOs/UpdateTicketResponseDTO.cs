using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class UpdateTicketResponseDTO : TicketDetailDTO
    {
        public List<Guid> CommentairesModifies { get; set; } = new();
        public List<Guid> CommentairesAjoutes { get; set; } = new();
        public List<Guid> CommentairesSupprimes { get; set; } = new();
        public Dictionary<Guid, List<Guid>> PiecesJointesSupprimees { get; set; } = new();
        public Dictionary<Guid, List<Guid>> PiecesJointesAjoutees { get; set; } = new();
    }
}