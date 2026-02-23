using System;
using System.Collections.Generic;
using System.Text;


namespace projet0.Application.Commun.DTOs.Ticket
{
    public class TicketDetailDTO : TicketDTO
    {
        public List<CommentaireDTO> Commentaires { get; set; } = new();
    }
}
