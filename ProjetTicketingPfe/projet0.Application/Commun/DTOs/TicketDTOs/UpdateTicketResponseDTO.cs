using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class UpdateTicketResponseDTO
    {
        public Guid Id { get; set; }
        public string ReferenceTicket { get; set; }
        public string TitreTicket { get; set; }
        public string DescriptionTicket { get; set; }
        public StatutTicket? StatutTicket { get; set; }
        public string StatutTicketLibelle { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateLimite { get; set; }
        public DateTime? DateCloture { get; set; }
        public Guid CreateurId { get; set; }
        public string CreateurNom { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeNom { get; set; }
        public int NombreCommentaires { get; set; }
        public int NombrePiecesJointes { get; set; }
        public List<CommentaireDTO> Commentaires { get; set; }
    }
}