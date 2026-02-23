using System;
using System.Collections.Generic;
using System.Text;
using projet0.Domain.Enums;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class CommentaireDTO
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public DateTime DateCreation { get; set; }
        public bool EstInterne { get; set; }
        public Guid AuteurId { get; set; }
        public string AuteurNom { get; set; }
        public List<PieceJointeDTO> PiecesJointes { get; set; } = new();
    }
}
