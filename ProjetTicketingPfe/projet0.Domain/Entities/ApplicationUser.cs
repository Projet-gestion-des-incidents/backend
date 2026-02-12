using Microsoft.AspNetCore.Identity;
using System;

namespace projet0.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    { 
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string? Image { get; set; }
        public DateTime? BirthDate { get; set; }
        public UserStatut Statut { get; set; } = UserStatut.Actif;

        // NOUVELLES PROPRIÉTÉS POUR TICKETS & INCIDENTS
        public virtual ICollection<Ticket> TicketsCrees { get; set; }
        public virtual ICollection<Ticket> TicketsAssignes { get; set; }        
        public virtual ICollection<CommentaireTicket> Commentaires { get; set; }
        public virtual ICollection<PieceJointe> PiecesJointes { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<HistoriqueTicket> HistoriquesModifies { get; set; }
        public virtual ICollection<IncidentTicket> IncidentLiaisons { get; set; }

    }
}