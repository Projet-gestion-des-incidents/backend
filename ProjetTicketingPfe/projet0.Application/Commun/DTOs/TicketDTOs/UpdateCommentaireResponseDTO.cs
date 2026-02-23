using projet0.Application.Commun.DTOs.Ticket;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class UpdateCommentaireResponseDTO : CommentaireDTO
    {
        public List<Guid> PiecesJointesSupprimees { get; set; } = new();
        public List<Guid> PiecesJointesAjoutees { get; set; } = new();
    }

}
