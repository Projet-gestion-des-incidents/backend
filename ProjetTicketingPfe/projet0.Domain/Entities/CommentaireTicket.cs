using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class CommentaireTicket
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public DateTime DateCreation { get; set; }
        public bool EstInterne { get; set; }  // Commentaire interne (visible seulement par les admins)

        // Foreign Keys
        public Guid TicketId { get; set; }
        public Guid AuteurId { get; set; }

        // Navigation Properties
        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser Auteur { get; set; }
        public virtual ICollection<PieceJointe> PiecesJointes { get; set; }
    }
}
